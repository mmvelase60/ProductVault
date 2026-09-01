import { NgIf } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'pv-resend-confirmation',
  imports: [FormsModule, NgIf, RouterLink],
  template: `
    <section class="auth-layout">
      <form class="auth-card" #resendForm="ngForm" (ngSubmit)="submit()" [attr.aria-busy]="loading">
        <span class="eyebrow">Account security</span>
        <h1>Resend verification code</h1>
        <p>Enter your email address and we will send a new verification code if it is needed.</p>
        <label>Email<input type="email" [(ngModel)]="email" name="email" autocomplete="email" required autofocus></label>
        <p class="notice" role="status" *ngIf="message">{{ message }}</p>
        <p class="error" role="alert" *ngIf="error">{{ error }}</p>
        <button class="button" type="submit" [disabled]="resendForm.invalid || loading">{{ loading ? 'Sending…' : 'Resend code' }}</button>
        <p class="muted"><a routerLink="/login">Back to sign in</a></p>
      </form>
    </section>
  `
})
export class ResendConfirmationComponent {
  email = '';
  message = '';
  error = '';
  loading = false;

  constructor(private readonly auth: AuthService, route: ActivatedRoute) {
    this.email = route.snapshot.queryParamMap.get('email') ?? '';
  }

  submit(): void {
    if (this.loading) return;
    this.loading = true;
    this.message = '';
    this.error = '';
    this.auth.resendConfirmation(this.email).subscribe({
      next: result => {
        this.loading = false;
        this.message = result.message;
      },
      error: response => {
        this.loading = false;
        this.error = response.error?.message ?? 'The verification code could not be sent.';
      }
    });
  }
}
