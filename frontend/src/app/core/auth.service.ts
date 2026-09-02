import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, catchError, firstValueFrom, of, tap } from 'rxjs';
import { apiUrl } from './api.config';
import { AuthResponse, EmailActionResponse } from './models';

const csrfCookieName = 'productvault_csrf';
const csrfHeaderName = 'X-CSRF-TOKEN';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly sessionSubject = new BehaviorSubject<AuthResponse | null>(null);
  readonly session$ = this.sessionSubject.asObservable();

  constructor(private readonly http: HttpClient) {}

  get token(): string | null { return this.sessionSubject.value?.accessToken ?? null; }
  get isAuthenticated(): boolean { return this.token !== null; }

  login(email: string, password: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${apiUrl}/auth/login`, { email, password }, { withCredentials: true }).pipe(tap(session => this.store(session)));
  }

  register(firstName: string, surname: string, email: string, password: string): Observable<EmailActionResponse> {
    return this.http.post<EmailActionResponse>(`${apiUrl}/auth/register`, { firstName, surname, email, password });
  }

  verifyEmailCode(email: string, code: string): Observable<EmailActionResponse> {
    return this.http.post<EmailActionResponse>(`${apiUrl}/auth/verify-email-code`, { email, code });
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

  async restoreSession(): Promise<void> {
    await firstValueFrom(this.http.post<AuthResponse>(`${apiUrl}/auth/refresh`, {}, this.sessionCookieOptions()).pipe(
      tap(session => this.store(session)),
      catchError(() => {
        this.clearSession();
        return of(null);
      })
    ));
  }

  logout(): Observable<void> {
    this.clearSession();
    return this.http.post<void>(`${apiUrl}/auth/logout`, {}, this.sessionCookieOptions()).pipe(catchError(() => of(void 0)));
  }

  clearSession(): void {
    this.sessionSubject.next(null);
  }

  private store(session: AuthResponse): void {
    this.sessionSubject.next(session);
  }

  private sessionCookieOptions(): { withCredentials: true; headers: HttpHeaders } {
    return { withCredentials: true, headers: new HttpHeaders({ [csrfHeaderName]: this.readCookie(csrfCookieName) ?? '' }) };
  }

  private readCookie(name: string): string | null {
    const cookie = document.cookie.split('; ').find(value => value.startsWith(`${name}=`));
    return cookie ? decodeURIComponent(cookie.substring(name.length + 1)) : null;
  }
}
