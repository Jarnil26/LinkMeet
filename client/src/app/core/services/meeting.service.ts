import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Meeting, CreateMeetingRequest, JoinMeetingRequest, Participant } from '../../shared/models/models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class MeetingService {
  constructor(private http: HttpClient) {}

  create(data: CreateMeetingRequest): Observable<Meeting> {
    return this.http.post<Meeting>(`${environment.apiUrl}/meetings`, data);
  }

  getById(id: string): Observable<Meeting> {
    return this.http.get<Meeting>(`${environment.apiUrl}/meetings/${id}`);
  }

  getByCode(code: string): Observable<Meeting> {
    return this.http.get<Meeting>(`${environment.apiUrl}/meetings/code/${code}`);
  }

  getUpcoming(): Observable<Meeting[]> {
    return this.http.get<Meeting[]>(`${environment.apiUrl}/meetings/upcoming`);
  }

  getPast(): Observable<Meeting[]> {
    return this.http.get<Meeting[]>(`${environment.apiUrl}/meetings/past`);
  }

  join(data: JoinMeetingRequest): Observable<Meeting> {
    return this.http.post<Meeting>(`${environment.apiUrl}/meetings/join`, data);
  }

  cancel(id: string): Observable<any> {
    return this.http.delete(`${environment.apiUrl}/meetings/${id}`);
  }

  end(id: string): Observable<any> {
    return this.http.post(`${environment.apiUrl}/meetings/${id}/end`, {});
  }

  getParticipants(meetingId: string): Observable<Participant[]> {
    return this.http.get<Participant[]>(`${environment.apiUrl}/meetings/${meetingId}/participants`);
  }
}
