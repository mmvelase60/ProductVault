import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { AuthResponse } from './models';
import { apiUrl } from './api.config';
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

  register(email: string, password: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${apiUrl}/auth/register`, { email, password }).pipe(tap(session => this.store(session)));
  }

  logout(): void { localStorage.removeItem(storageKey); this.sessionSubject.next(null); }

  private store(session: AuthResponse): void { localStorage.setItem(storageKey, JSON.stringify(session)); this.sessionSubject.next(session); }
  private readSession(): AuthResponse | null {
    const value = localStorage.getItem(storageKey);
    if (!value) return null;
    try { const session = JSON.parse(value) as AuthResponse; return new Date(session.expiresAt) > new Date() ? session : null; }
    catch { return null; }
  }
}
