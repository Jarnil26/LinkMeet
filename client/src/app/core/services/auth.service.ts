import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { AuthResponse, LoginRequest, RegisterRequest, User } from '../../shared/models/models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private currentUserSubject = new BehaviorSubject<User | null>(null);
  currentUser$ = this.currentUserSubject.asObservable();

  constructor(private http: HttpClient) {
    const stored = localStorage.getItem('linkmeet_user');
    if (stored) {
      this.currentUserSubject.next(JSON.parse(stored));
    }
  }

  get currentUser(): User | null {
    return this.currentUserSubject.value;
  }

  get token(): string | null {
    return localStorage.getItem('linkmeet_token');
  }

  get isLoggedIn(): boolean {
    return !!this.token;
  }

  register(data: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${environment.apiUrl}/auth/register`, data)
      .pipe(tap(res => this.storeAuth(res)));
  }

  login(data: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${environment.apiUrl}/auth/login`, data)
      .pipe(tap(res => this.storeAuth(res)));
  }

  logout(): void {
    localStorage.removeItem('linkmeet_token');
    localStorage.removeItem('linkmeet_user');
    this.currentUserSubject.next(null);
  }

  private storeAuth(res: AuthResponse): void {
    localStorage.setItem('linkmeet_token', res.token);
    localStorage.setItem('linkmeet_user', JSON.stringify(res.user));
    this.currentUserSubject.next(res.user);
  }
}
