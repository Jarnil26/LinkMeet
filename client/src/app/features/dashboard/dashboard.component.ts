import { Component, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../core/services/auth.service';
import { MeetingService } from '../../core/services/meeting.service';
import { Meeting } from '../../shared/models/models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent implements OnInit {
  upcomingMeetings: Meeting[] = [];
  pastMeetings: Meeting[] = [];
  activeTab = 'upcoming';
  showCreateModal = false;
  showInviteModal = false;
  joinCode = '';
  joinError = '';
  copiedText = '';

  // Created meeting (for invite modal)
  createdMeeting: Meeting | null = null;

  // Create meeting form
  newMeeting = { title: '', scheduledAt: '', password: '', hasWaitingRoom: false };
  createError = '';

  constructor(
    public auth: AuthService,
    private meetingService: MeetingService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadMeetings();
  }

  loadMeetings(): void {
    this.meetingService.getUpcoming().subscribe({
      next: (data) => this.upcomingMeetings = data,
      error: () => {}
    });
    this.meetingService.getPast().subscribe({
      next: (data) => this.pastMeetings = data,
      error: () => {}
    });
  }

  createMeeting(): void {
    if (!this.newMeeting.title.trim()) {
      this.createError = 'Meeting title is required';
      return;
    }
    this.meetingService.create({
      title: this.newMeeting.title,
      scheduledAt: this.newMeeting.scheduledAt || undefined,
      password: this.newMeeting.password || undefined,
      hasWaitingRoom: this.newMeeting.hasWaitingRoom
    }).subscribe({
      next: (meeting) => {
        this.showCreateModal = false;
        this.createdMeeting = meeting;
        this.showInviteModal = true;
        this.loadMeetings();
      },
      error: (err) => this.createError = err.error?.message || 'Failed to create meeting'
    });
  }

  joinMeeting(): void {
    if (!this.joinCode.trim()) return;
    this.joinError = '';
    this.meetingService.join({ meetingCode: this.joinCode.trim() }).subscribe({
      next: (meeting) => {
        this.joinCode = '';
        this.router.navigate(['/meeting', meeting.id]);
      },
      error: (err) => this.joinError = err.error?.message || 'Meeting not found. Please check the code and try again.'
    });
  }

  startInstantMeeting(): void {
    this.meetingService.create({
      title: 'Instant Meeting',
      hasWaitingRoom: false
    }).subscribe({
      next: (meeting) => {
        this.createdMeeting = meeting;
        this.showInviteModal = true;
        this.loadMeetings();
      },
      error: () => {}
    });
  }

  goToMeetingRoom(): void {
    if (this.createdMeeting) {
      this.showInviteModal = false;
      this.router.navigate(['/meeting', this.createdMeeting.id]);
    }
  }

  openMeeting(meeting: Meeting): void {
    // Call join API to register as participant, then navigate
    this.meetingService.join({ meetingCode: meeting.meetingCode }).subscribe({
      next: (m) => this.router.navigate(['/meeting', m.id]),
      error: () => this.router.navigate(['/meeting', meeting.id]) // fallback
    });
  }

  cancelMeeting(id: string, event: Event): void {
    event.stopPropagation();
    this.meetingService.cancel(id).subscribe({
      next: () => this.loadMeetings(),
      error: () => {}
    });
  }

  copyMeetingCode(code: string, event?: Event): void {
    if (event) event.stopPropagation();
    navigator.clipboard.writeText(code).then(() => {
      this.copiedText = code;
      setTimeout(() => this.copiedText = '', 2000);
    });
  }

  copyInviteLink(): void {
    if (this.createdMeeting) {
      const link = `${window.location.origin}/meeting/${this.createdMeeting.id}`;
      const text = `Join my LinkMeet meeting!\n\nMeeting: ${this.createdMeeting.title}\nCode: ${this.createdMeeting.meetingCode}\nLink: ${link}`;
      navigator.clipboard.writeText(text).then(() => {
        this.copiedText = 'invite';
        setTimeout(() => this.copiedText = '', 2000);
      });
    }
  }

  logout(): void {
    this.auth.logout();
    this.router.navigate(['/']);
  }

  getStatusBadgeClass(status: string): string {
    switch (status) {
      case 'Active': return 'badge-success';
      case 'Scheduled': return 'badge-info';
      case 'Ended': return 'badge-warning';
      case 'Cancelled': return 'badge-danger';
      default: return 'badge-info';
    }
  }
}
