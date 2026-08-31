import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { apiUrl } from './api.config';
import { AuthResponse, EmailActionResponse } from './models';

const storageKey = 'productvault-session';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly sessionSubject = new BehaviorSubject<AuthResponse | null>(this.readSession());
  readonly session$ = this.sessionSubject.asObservable();

  constructor(private readonly http: HttpClient) {}

  get token(): string | null { return this.sessionSubject.value?.accessToken ?? null; }
  get isAuthenticated(): boolean { return this.token !== null; }

  login(email: string, password: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${apiUrl}/auth/login`, { email, password }).pipe(tap(session => this.store(session)));
  }

  register(email: string, password: string): Observable<EmailActionResponse> {
    return this.http.post<EmailActionResponse>(`${apiUrl}/auth/register`, { email, password });
  }

  confirmEmail(userId: string, token: string): Observable<EmailActionResponse> {
    return this.http.post<EmailActionResponse>(`${apiUrl}/auth/confirm-email`, { userId, token });
  }

  resendConfirmation(email: string): Observable<EmailActionResponse> {
    return this.http.post<EmailActionResponse>(`${apiUrl}/auth/resend-confirmation`, { email });
  }

  forgotPassword(email: string): Observable<EmailActionResponse> {
    return this.http.post<EmailActionResponse>(`${apiUrl}/auth/forgot-password`, { email });
  }

  resetPassword(userId: string, token: string, password: string): Observable<EmailActionResponse> {
    return this.http.post<EmailActionResponse>(`${apiUrl}/auth/reset-password`, { userId, token, password });
  }

  logout(): void {
    localStorage.removeItem(storageKey);
    this.sessionSubject.next(null);
  }

  private store(session: AuthResponse): void {
    localStorage.setItem(storageKey, JSON.stringify(session));
    this.sessionSubject.next(session);
  }

  private readSession(): AuthResponse | null {
    const value = localStorage.getItem(storageKey);
    if (!value) return null;
    try {
      const session = JSON.parse(value) as AuthResponse;
      return new Date(session.expiresAt) > new Date() ? session : null;
    } catch {
      return null;
    }
  }
}
