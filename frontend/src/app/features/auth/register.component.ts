import { NgIf } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { NotificationService } from '../../core/notification.service';

@Component({
  selector: 'pv-register',
  imports: [FormsModule, RouterLink, NgIf],
  template: `
    <section class="auth-layout">
      <form class="auth-card" #registerForm="ngForm" (ngSubmit)="submit()" [attr.aria-busy]="loading">
        <span class="eyebrow">Private workspace</span>
        <h1>Create your account</h1>
        <p>Start managing categories and products securely.</p>
        <label>First name<input type="text" [(ngModel)]="firstName" name="firstName" autocomplete="given-name" required></label>
        <label>Surname<input type="text" [(ngModel)]="surname" name="surname" autocomplete="family-name" required></label>
        <label>Email<input type="email" [(ngModel)]="email" name="email" autocomplete="email" required></label>
        <label>Password<input type="password" [(ngModel)]="password" name="password" autocomplete="new-password" required minlength="8"><small>At least 8 characters.</small></label>
        <p class="muted">Your username is generated automatically, for example: Mthokozisi Mvelase → MMvelase.</p>
        <p class="error" role="alert" *ngIf="error">{{ error }}</p>
        <button class="button" type="submit" [disabled]="registerForm.invalid || loading">{{ loading ? 'Creating…' : 'Create account' }}</button>
        <p class="muted">Already registered? <a routerLink="/login">Sign in</a></p>
      </form>
    </section>
  `
})
export class RegisterComponent {
  firstName = '';
  surname = '';
  email = '';
  password = '';
  error = '';
  loading = false;

  constructor(private readonly auth: AuthService, private readonly notifications: NotificationService, private readonly router: Router) {}

  submit(): void {
    if (this.loading) return;
    this.loading = true;
    this.error = '';
    this.auth.register(this.firstName, this.surname, this.email, this.password).subscribe({
      next: () => {
        void this.router.navigate(['/verify-email'], { queryParams: { email: this.email } });
        this.notifications.showDialog({ kind: 'success', title: 'Account created', message: `We sent a six-digit verification code to ${this.email}. Enter it on the next screen to activate your account.`, actionLabel: 'Enter verification code' });
      },
      error: response => {
        const errors = response.error?.errors;
        this.error = errors ? Object.values(errors).join(' ') : 'Registration failed.';
        this.notifications.showDialog({ kind: 'error', title: 'We could not create your account', message: this.error, actionLabel: 'Review details' });
        this.loading = false;
      }
    });
  }
}
