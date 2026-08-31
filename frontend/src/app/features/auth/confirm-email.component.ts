import { NgIf } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'pv-confirm-email',
  imports: [NgIf, RouterLink],
  template: `
    <section class="auth-layout">
      <section class="auth-card" aria-live="polite">
        <span class="eyebrow">Account security</span>
        <h1>Confirm your email</h1>
        <p *ngIf="loading">Confirming your email address…</p>
        <p class="notice" *ngIf="message">{{ message }}</p>
        <p class="error" role="alert" *ngIf="error">{{ error }}</p>
        <p class="muted" *ngIf="message || error"><a routerLink="/login">Go to sign in</a></p>
      </section>
    </section>
  `
})
export class ConfirmEmailComponent implements OnInit {
  loading = true;
  message = '';
  error = '';

  constructor(private readonly auth: AuthService, private readonly route: ActivatedRoute) {}

  ngOnInit(): void {
    const userId = this.route.snapshot.queryParamMap.get('userId');
    const token = this.route.snapshot.queryParamMap.get('token');
    if (!userId || !token) {
      this.loading = false;
      this.error = 'This confirmation link is incomplete or invalid.';
      return;
    }

    this.auth.confirmEmail(userId, token).subscribe({
      next: result => {
        this.loading = false;
        this.message = result.message;
      },
      error: response => {
        this.loading = false;
        this.error = response.error?.message ?? 'This confirmation link could not be used.';
      }
    });
  }
}
