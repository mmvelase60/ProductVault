import { NgIf } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { NotificationService } from '../../core/notification.service';

@Component({
  selector: 'pv-forgot-password',
  imports: [FormsModule, NgIf, RouterLink],
  template: `
    <section class="auth-layout">
      <form class="auth-card" #forgotForm="ngForm" (ngSubmit)="submit()" [attr.aria-busy]="loading">
        <span class="eyebrow">Account recovery</span>
        <h1>Reset your password</h1>
        <p>Enter the email address associated with your ProductVault account.</p>
        <label>Email<input type="email" [(ngModel)]="email" name="email" autocomplete="email" required autofocus></label>
        <p class="notice" role="status" aria-live="polite" *ngIf="message">{{ message }}</p>
        <p class="error" role="alert" *ngIf="error">{{ error }}</p>
        <button class="button" type="submit" [disabled]="forgotForm.invalid || loading">{{ loading ? 'Sending…' : 'Send reset link' }}</button>
        <p class="muted"><a routerLink="/login">Back to sign in</a></p>
      </form>
    </section>
  `
})
export class ForgotPasswordComponent {
  email = '';
  message = '';
  error = '';
  loading = false;

  constructor(private readonly auth: AuthService, private readonly notifications: NotificationService) {}

  submit(): void {
    if (this.loading) return;
    this.loading = true;
    this.message = '';
    this.error = '';
    this.auth.forgotPassword(this.email).subscribe({
      next: result => {
        this.loading = false;
        this.message = result.message;
        this.notifications.showDialog({ kind: 'success', title: 'Password reset request received', message: 'If a confirmed account exists for that address, a reset link has been sent. Check your inbox and Spam folder.', actionLabel: 'Continue' });
      },
      error: response => {
        this.loading = false;
        this.error = response.error?.message ?? 'The reset link could not be sent.';
        this.notifications.showDialog({ kind: 'error', title: 'We could not send the reset link', message: `${this.error} Please try again in a moment.`, actionLabel: 'Try again' });
      }
    });
  }
}
