import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { BehaviorSubject, Subject } from 'rxjs';
import { ChatMessage } from '../../shared/models/models';
import { AuthService } from './auth.service';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class SignalRService {
  private hubConnection!: signalR.HubConnection;
  private _messages = new BehaviorSubject<ChatMessage[]>([]);
  messages$ = this._messages.asObservable();

  userJoined$ = new Subject<any>();
  userLeft$ = new Subject<any>();
  existingParticipants$ = new Subject<any[]>();
  receiveSignal$ = new Subject<any>();
  audioToggled$ = new Subject<any>();
  videoToggled$ = new Subject<any>();
  screenShareStarted$ = new Subject<any>();
  screenShareStopped$ = new Subject<any>();
  participantMuted$ = new Subject<any>();
  participantRemoved$ = new Subject<any>();

  constructor(private authService: AuthService) {}

  async startConnection(): Promise<void> {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(environment.hubUrl, {
        accessTokenFactory: () => this.authService.token || ''
      })
      .withAutomaticReconnect()
      .build();

    this.registerHandlers();

    await this.hubConnection.start();
  }

  async joinMeeting(meetingId: string): Promise<void> {
    await this.hubConnection.invoke('JoinMeeting', meetingId);
  }

  async leaveMeeting(meetingId: string): Promise<void> {
    await this.hubConnection.invoke('LeaveMeeting', meetingId);
  }

  async sendMessage(meetingId: string, content: string): Promise<void> {
    await this.hubConnection.invoke('SendMessage', meetingId, content);
  }

  async sendSignal(meetingId: string, targetConnectionId: string, signalType: string, signalData: string): Promise<void> {
    await this.hubConnection.invoke('SendSignal', meetingId, targetConnectionId, signalType, signalData);
  }

  async toggleAudio(meetingId: string, isOn: boolean): Promise<void> {
    await this.hubConnection.invoke('ToggleAudio', meetingId, isOn);
  }

  async toggleVideo(meetingId: string, isOn: boolean): Promise<void> {
    await this.hubConnection.invoke('ToggleVideo', meetingId, isOn);
  }

  async startScreenShare(meetingId: string): Promise<void> {
    await this.hubConnection.invoke('StartScreenShare', meetingId);
  }

  async stopScreenShare(meetingId: string): Promise<void> {
    await this.hubConnection.invoke('StopScreenShare', meetingId);
  }

  async muteParticipant(meetingId: string, userId: string): Promise<void> {
    await this.hubConnection.invoke('MuteParticipant', meetingId, userId);
  }

  async removeParticipant(meetingId: string, userId: string): Promise<void> {
    await this.hubConnection.invoke('RemoveParticipant', meetingId, userId);
  }

  async stopConnection(): Promise<void> {
    if (this.hubConnection) {
      await this.hubConnection.stop();
    }
  }

  private registerHandlers(): void {
    this.hubConnection.on('ReceiveMessage', (msg: ChatMessage) => {
      const current = this._messages.value;
      this._messages.next([...current, msg]);
    });

    this.hubConnection.on('UserJoined', (data) => this.userJoined$.next(data));
    this.hubConnection.on('UserLeft', (data) => this.userLeft$.next(data));
    this.hubConnection.on('ExistingParticipants', (data) => this.existingParticipants$.next(data));
    this.hubConnection.on('ReceiveSignal', (data) => this.receiveSignal$.next(data));
    this.hubConnection.on('AudioToggled', (data) => this.audioToggled$.next(data));
    this.hubConnection.on('VideoToggled', (data) => this.videoToggled$.next(data));
    this.hubConnection.on('ScreenShareStarted', (data) => this.screenShareStarted$.next(data));
    this.hubConnection.on('ScreenShareStopped', (data) => this.screenShareStopped$.next(data));
    this.hubConnection.on('ParticipantMuted', (data) => this.participantMuted$.next(data));
    this.hubConnection.on('ParticipantRemoved', (data) => this.participantRemoved$.next(data));
  }

  clearMessages(): void {
    this._messages.next([]);
  }
}
