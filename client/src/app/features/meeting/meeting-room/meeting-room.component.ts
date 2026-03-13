import { Component, OnInit, OnDestroy, ViewChild, ElementRef, ChangeDetectorRef } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import { MeetingService } from '../../../core/services/meeting.service';
import { SignalRService } from '../../../core/services/signalr.service';
import { AuthService } from '../../../core/services/auth.service';
import { WebRTCService, RemotePeer } from '../../../core/services/webrtc.service';
import { Meeting, Participant, ChatMessage } from '../../../shared/models/models';

@Component({
  selector: 'app-meeting-room',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './meeting-room.component.html',
  styleUrl: './meeting-room.component.css'
})
export class MeetingRoomComponent implements OnInit, OnDestroy {
  @ViewChild('localVideo') localVideoRef!: ElementRef<HTMLVideoElement>;

  meeting: Meeting | null = null;
  participants: Participant[] = [];
  messages: ChatMessage[] = [];
  remotePeers: RemotePeer[] = [];
  meetingId = '';

  isAudioOn = true;
  isVideoOn = true;
  isScreenSharing = false;
  showChat = false;
  showParticipants = false;
  newMessage = '';
  elapsedTime = '00:00';
  copied = false;
  copiedInvite = false;
  localStreamReady = false;

  private localStream: MediaStream | null = null;
  private screenStream: MediaStream | null = null;
  private subs: Subscription[] = [];
  private timer: any;
  private startTime = 0;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private meetingService: MeetingService,
    public signalR: SignalRService,
    public auth: AuthService,
    private webrtcService: WebRTCService,
    private cdr: ChangeDetectorRef
  ) {}

  async ngOnInit(): Promise<void> {
    this.meetingId = this.route.snapshot.params['id'];
    this.webrtcService.setMeetingId(this.meetingId);
    this.loadMeeting();
    this.startTimer();

    // Get local media FIRST
    try {
      this.localStream = await navigator.mediaDevices.getUserMedia({
        video: { width: { ideal: 1280 }, height: { ideal: 720 }, facingMode: 'user' },
        audio: { echoCancellation: true, noiseSuppression: true }
      });
      this.localStreamReady = true;
      await this.webrtcService.setLocalStream(this.localStream);
      this.cdr.detectChanges(); // Trigger view update so #localVideo exists

      // Now attach to video element — use setTimeout to ensure ViewChild is resolved
      setTimeout(() => this.attachLocalStream(), 50);
    } catch (err) {
      console.warn('Could not access camera/microphone:', err);
    }

    // Connect to SignalR hub AFTER media is ready
    try {
      await this.signalR.startConnection();
      await this.signalR.joinMeeting(this.meetingId);
    } catch (e) {
      console.warn('Could not connect to SignalR', e);
    }

    // Subscribe to events
    this.subs.push(
      this.signalR.messages$.subscribe(msgs => this.messages = msgs),
      this.signalR.userJoined$.subscribe(() => this.loadParticipants()),
      this.signalR.userLeft$.subscribe(() => this.loadParticipants()),
      this.signalR.participantMuted$.subscribe(data => {
        if (data.userId === this.auth.currentUser?.id) this.toggleAudio();
      }),
      this.signalR.participantRemoved$.subscribe(data => {
        if (data.userId === this.auth.currentUser?.id) this.leaveMeeting();
      }),
      // Subscribe to remote peer updates from WebRTC service
      this.webrtcService.remotePeerUpdated$.subscribe(peers => {
        this.remotePeers = Array.from(peers.values());
        this.cdr.detectChanges();
        // Attach after Angular has rendered the video elements
        setTimeout(() => this.attachRemoteStreams(), 200);
      })
    );

    this.loadParticipants();
  }

  private attachLocalStream(): void {
    if (this.localStream) {
      // Try ViewChild first
      if (this.localVideoRef?.nativeElement) {
        this.localVideoRef.nativeElement.srcObject = this.localStream;
        console.log('Local video attached via ViewChild');
        return;
      }
      // Fallback: find by ID
      const el = document.getElementById('local-video-el') as HTMLVideoElement;
      if (el) {
        el.srcObject = this.localStream;
        console.log('Local video attached via getElementById');
      } else {
        // Retry after a short delay
        setTimeout(() => this.attachLocalStream(), 200);
      }
    }
  }

  loadMeeting(): void {
    this.meetingService.getById(this.meetingId).subscribe({
      next: (m) => this.meeting = m,
      error: () => this.router.navigate(['/dashboard'])
    });
  }

  loadParticipants(): void {
    this.meetingService.getParticipants(this.meetingId).subscribe({
      next: (p) => this.participants = p,
      error: () => {}
    });
  }

  attachRemoteStreams(): void {
    this.remotePeers.forEach(peer => {
      if (peer.stream) {
        const videoEl = document.getElementById('remote-video-' + peer.connectionId) as HTMLVideoElement;
        if (videoEl) {
          if (videoEl.srcObject !== peer.stream) {
            videoEl.srcObject = peer.stream;
            console.log('Remote video attached for:', peer.displayName);
          }
        } else {
          // Element not yet in DOM, retry
          setTimeout(() => this.attachRemoteStreams(), 300);
        }
      }
    });
  }

  toggleAudio(): void {
    this.isAudioOn = !this.isAudioOn;
    this.localStream?.getAudioTracks().forEach(t => t.enabled = this.isAudioOn);
    this.signalR.toggleAudio(this.meetingId, this.isAudioOn);
  }

  toggleVideo(): void {
    this.isVideoOn = !this.isVideoOn;
    this.localStream?.getVideoTracks().forEach(t => t.enabled = this.isVideoOn);
    this.signalR.toggleVideo(this.meetingId, this.isVideoOn);
  }

  async toggleScreenShare(): Promise<void> {
    if (this.isScreenSharing) {
      this.isScreenSharing = false;
      if (this.localStream) {
        await this.webrtcService.setLocalStream(this.localStream);
        this.attachLocalStream();
      }
      await this.signalR.stopScreenShare(this.meetingId);
    } else {
      try {
        this.screenStream = await (navigator.mediaDevices as any).getDisplayMedia({ video: true });
        this.isScreenSharing = true;
        await this.webrtcService.setLocalStream(this.screenStream!);
        const el = document.getElementById('local-video-el') as HTMLVideoElement;
        if (el) el.srcObject = this.screenStream;
        await this.signalR.startScreenShare(this.meetingId);
        this.screenStream!.getVideoTracks()[0].onended = async () => {
          this.isScreenSharing = false;
          if (this.localStream) {
            await this.webrtcService.setLocalStream(this.localStream);
            this.attachLocalStream();
          }
          await this.signalR.stopScreenShare(this.meetingId);
        };
      } catch {
        console.warn('Screen share cancelled');
      }
    }
  }

  sendMessage(): void {
    if (!this.newMessage.trim()) return;
    this.signalR.sendMessage(this.meetingId, this.newMessage.trim());
    this.newMessage = '';
  }

  async leaveMeeting(): Promise<void> {
    try {
      await this.signalR.leaveMeeting(this.meetingId);
      await this.signalR.stopConnection();
    } catch {}

    this.webrtcService.cleanup();
    this.localStream?.getTracks().forEach(t => t.stop());
    this.screenStream?.getTracks().forEach(t => t.stop());
    clearInterval(this.timer);
    this.signalR.clearMessages();
    this.router.navigate(['/dashboard']);
  }

  async endMeeting(): Promise<void> {
    this.meetingService.end(this.meetingId).subscribe({
      next: () => this.leaveMeeting(),
      error: () => this.leaveMeeting()
    });
  }

  muteParticipant(userId: string): void {
    this.signalR.muteParticipant(this.meetingId, userId);
  }

  removeParticipant(userId: string): void {
    this.signalR.removeParticipant(this.meetingId, userId);
  }

  isHost(): boolean {
    return this.meeting?.hostId === this.auth.currentUser?.id;
  }

  copyMeetingCode(): void {
    if (this.meeting?.meetingCode) {
      navigator.clipboard.writeText(this.meeting.meetingCode).then(() => {
        this.copied = true;
        setTimeout(() => this.copied = false, 2000);
      });
    }
  }

  copyInviteLink(): void {
    if (this.meeting) {
      const link = `${window.location.origin}/meeting/${this.meeting.id}`;
      const text = `Join my LinkMeet meeting!\n\nMeeting: ${this.meeting.title}\nCode: ${this.meeting.meetingCode}\nLink: ${link}`;
      navigator.clipboard.writeText(text).then(() => {
        this.copiedInvite = true;
        setTimeout(() => this.copiedInvite = false, 2000);
      });
    }
  }

  getTotalParticipants(): number {
    return 1 + this.remotePeers.length;
  }

  getGridClass(): string {
    const total = this.getTotalParticipants();
    if (total <= 1) return 'grid-1';
    if (total === 2) return 'grid-2';
    if (total <= 4) return 'grid-4';
    if (total <= 6) return 'grid-6';
    return 'grid-many';
  }

  private startTimer(): void {
    this.startTime = Date.now();
    this.timer = setInterval(() => {
      const elapsed = Math.floor((Date.now() - this.startTime) / 1000);
      const hrs = Math.floor(elapsed / 3600);
      const mins = Math.floor((elapsed % 3600) / 60).toString().padStart(2, '0');
      const secs = (elapsed % 60).toString().padStart(2, '0');
      this.elapsedTime = hrs > 0 ? `${hrs}:${mins}:${secs}` : `${mins}:${secs}`;
    }, 1000);
  }

  ngOnDestroy(): void {
    this.subs.forEach(s => s.unsubscribe());
    this.webrtcService.cleanup();
    this.localStream?.getTracks().forEach(t => t.stop());
    this.screenStream?.getTracks().forEach(t => t.stop());
    clearInterval(this.timer);
  }
}
