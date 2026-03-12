import { Component } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../core/services/auth.service';
import { MeetingService } from '../../core/services/meeting.service';

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [RouterLink, FormsModule],
  templateUrl: './landing.component.html',
  styleUrl: './landing.component.css'
})
export class LandingComponent {
  meetingCode = '';
  error = '';

  constructor(
    private router: Router,
    public auth: AuthService,
    private meetingService: MeetingService
  ) {}

  joinMeeting(): void {
    if (!this.meetingCode.trim()) return;
    if (!this.auth.isLoggedIn) {
      this.router.navigate(['/login']);
      return;
    }
    this.meetingService.join({ meetingCode: this.meetingCode.trim() }).subscribe({
      next: (meeting) => this.router.navigate(['/meeting', meeting.id]),
      error: (err) => this.error = err.error?.message || 'Meeting not found'
    });
  }
}
