import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';
import { SignalRService } from './signalr.service';

export interface RemotePeer {
  connectionId: string;
  userId: string;
  displayName: string;
  stream: MediaStream | null;
  isAudioOn: boolean;
  isVideoOn: boolean;
}

@Injectable({ providedIn: 'root' })
export class WebRTCService {
  private peerConnections = new Map<string, RTCPeerConnection>();
  private remoteStreams = new Map<string, MediaStream>();
  private localStream: MediaStream | null = null;
  private meetingId = '';

  private _remotePeers = new Map<string, RemotePeer>();
  remotePeerUpdated$ = new Subject<Map<string, RemotePeer>>();

  private iceServers: RTCIceServer[] = [
    { urls: 'stun:stun.l.google.com:19302' },
    { urls: 'stun:stun1.l.google.com:19302' },
    { urls: 'stun:stun2.l.google.com:19302' }
  ];

  constructor(private signalR: SignalRService) {
    this.setupSignalRHandlers();
  }

  private setupSignalRHandlers(): void {
    // When we join, we receive the list of existing participants
    // We create offers to ALL of them
    this.signalR.existingParticipants$.subscribe(async (users: any[]) => {
      console.log('Existing participants:', users.length);
      for (const user of users) {
        console.log('Creating offer to existing user:', user.displayName, user.connectionId);
        this._remotePeers.set(user.connectionId, {
          connectionId: user.connectionId,
          userId: user.userId,
          displayName: user.displayName,
          stream: null,
          isAudioOn: true,
          isVideoOn: true
        });
        this.emitUpdate();
        // We are the new joiner, so WE send offers to existing participants
        await this.createPeerConnection(user.connectionId, true);
      }
    });

    // When a NEW user joins AFTER us, they will send us an offer
    // We just track their info — the offer handler will create the peer connection
    this.signalR.userJoined$.subscribe((data: any) => {
      console.log('New user joined:', data.displayName, data.connectionId);
      // Store their info so handleOffer can find their name
      this._remotePeers.set(data.connectionId, {
        connectionId: data.connectionId,
        userId: data.userId,
        displayName: data.displayName,
        stream: null,
        isAudioOn: true,
        isVideoOn: true
      });
      this.emitUpdate();
      // Do NOT create offer here — the new joiner will send us an offer via ExistingParticipants
    });

    // When a user leaves, clean up their peer connection
    this.signalR.userLeft$.subscribe((data: any) => {
      console.log('User left:', data.displayName);
      for (const [connId, peer] of this._remotePeers) {
        if (peer.userId === data.userId) {
          this.closePeerConnection(connId);
          break;
        }
      }
    });

    // Handle incoming WebRTC signals
    this.signalR.receiveSignal$.subscribe(async (data: any) => {
      const { senderConnectionId, signalType, signalData } = data;
      console.log('Received signal:', signalType, 'from:', senderConnectionId);

      try {
        if (signalType === 'offer') {
          await this.handleOffer(senderConnectionId, JSON.parse(signalData));
        } else if (signalType === 'answer') {
          await this.handleAnswer(senderConnectionId, JSON.parse(signalData));
        } else if (signalType === 'ice-candidate') {
          await this.handleIceCandidate(senderConnectionId, JSON.parse(signalData));
        }
      } catch (err) {
        console.error('Error handling signal:', err);
      }
    });

    // Handle audio/video toggles from remote users
    this.signalR.audioToggled$.subscribe((data: any) => {
      for (const [connId, peer] of this._remotePeers) {
        if (peer.userId === data.userId) {
          peer.isAudioOn = data.isOn;
          this.emitUpdate();
          break;
        }
      }
    });

    this.signalR.videoToggled$.subscribe((data: any) => {
      for (const [connId, peer] of this._remotePeers) {
        if (peer.userId === data.userId) {
          peer.isVideoOn = data.isOn;
          this.emitUpdate();
          break;
        }
      }
    });
  }

  async setLocalStream(stream: MediaStream): Promise<void> {
    this.localStream = stream;
    // Update tracks in existing peer connections using replaceTrack for seamless switching
    const tracks = stream.getTracks();
    for (const [connId, pc] of this.peerConnections) {
      const senders = pc.getSenders();
      for (const track of tracks) {
        const sender = senders.find(s => s.track?.kind === track.kind);
        if (sender) {
          await sender.replaceTrack(track);
        } else {
          pc.addTrack(track, stream);
        }
      }
    }
  }

  setMeetingId(id: string): void {
    this.meetingId = id;
  }

  getRemotePeers(): Map<string, RemotePeer> {
    return this._remotePeers;
  }

  private async createPeerConnection(remoteConnectionId: string, createOffer: boolean): Promise<RTCPeerConnection> {
    // Close existing if any
    if (this.peerConnections.has(remoteConnectionId)) {
      this.peerConnections.get(remoteConnectionId)?.close();
    }

    const pc = new RTCPeerConnection({ iceServers: this.iceServers });
    this.peerConnections.set(remoteConnectionId, pc);

    // Add local tracks
    if (this.localStream) {
      this.localStream.getTracks().forEach(track => {
        pc.addTrack(track, this.localStream!);
      });
    }

    // Handle ICE candidates
    pc.onicecandidate = (event) => {
      if (event.candidate) {
        this.signalR.sendSignal(
          this.meetingId,
          remoteConnectionId,
          'ice-candidate',
          JSON.stringify(event.candidate)
        );
      }
    };

    // Handle remote stream
    pc.ontrack = (event) => {
      console.log('Received remote track from:', remoteConnectionId);
      let remoteStream = this.remoteStreams.get(remoteConnectionId);
      if (!remoteStream) {
        remoteStream = new MediaStream();
        this.remoteStreams.set(remoteConnectionId, remoteStream);
      }
      remoteStream.addTrack(event.track);

      const peer = this._remotePeers.get(remoteConnectionId);
      if (peer) {
        peer.stream = remoteStream;
        this.emitUpdate();
      }
    };

    pc.onconnectionstatechange = () => {
      console.log(`Connection state (${remoteConnectionId}):`, pc.connectionState);
      if (pc.connectionState === 'failed' || pc.connectionState === 'disconnected') {
        this.closePeerConnection(remoteConnectionId);
      }
    };

    // Create offer if we are the initiator
    if (createOffer) {
      try {
        const offer = await pc.createOffer();
        await pc.setLocalDescription(offer);
        await this.signalR.sendSignal(
          this.meetingId,
          remoteConnectionId,
          'offer',
          JSON.stringify(offer)
        );
      } catch (err) {
        console.error('Error creating offer:', err);
      }
    }

    return pc;
  }

  private async handleOffer(senderConnectionId: string, offer: RTCSessionDescriptionInit): Promise<void> {
    // The peer should already be tracked from the UserJoined event
    // If not, add with whatever info we have
    if (!this._remotePeers.has(senderConnectionId)) {
      this._remotePeers.set(senderConnectionId, {
        connectionId: senderConnectionId,
        userId: '',
        displayName: 'Participant',
        stream: null,
        isAudioOn: true,
        isVideoOn: true
      });
      this.emitUpdate();
    }

    const pc = await this.createPeerConnection(senderConnectionId, false);

    await pc.setRemoteDescription(new RTCSessionDescription(offer));
    const answer = await pc.createAnswer();
    await pc.setLocalDescription(answer);

    await this.signalR.sendSignal(
      this.meetingId,
      senderConnectionId,
      'answer',
      JSON.stringify(answer)
    );
  }

  private async handleAnswer(senderConnectionId: string, answer: RTCSessionDescriptionInit): Promise<void> {
    const pc = this.peerConnections.get(senderConnectionId);
    if (pc) {
      await pc.setRemoteDescription(new RTCSessionDescription(answer));
    }
  }

  private async handleIceCandidate(senderConnectionId: string, candidate: RTCIceCandidateInit): Promise<void> {
    const pc = this.peerConnections.get(senderConnectionId);
    if (pc) {
      try {
        await pc.addIceCandidate(new RTCIceCandidate(candidate));
      } catch (err) {
        console.error('Error adding ICE candidate:', err);
      }
    }
  }

  private closePeerConnection(connectionId: string): void {
    const pc = this.peerConnections.get(connectionId);
    if (pc) {
      pc.close();
      this.peerConnections.delete(connectionId);
    }
    this.remoteStreams.delete(connectionId);
    this._remotePeers.delete(connectionId);
    this.emitUpdate();
  }

  private emitUpdate(): void {
    this.remotePeerUpdated$.next(new Map(this._remotePeers));
  }

  cleanup(): void {
    for (const [connId, pc] of this.peerConnections) {
      pc.close();
    }
    this.peerConnections.clear();
    this.remoteStreams.clear();
    this._remotePeers.clear();
    this.localStream = null;
    this.emitUpdate();
  }
}
