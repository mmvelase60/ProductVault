import { NgIf } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'pv-reset-password',
  imports: [FormsModule, NgIf, RouterLink],
  template: `
    <section class="auth-layout">
      <form class="auth-card" #resetForm="ngForm" (ngSubmit)="submit()" [attr.aria-busy]="loading">
        <span class="eyebrow">Account recovery</span>
        <h1>Choose a new password</h1>
        <p>Use at least 8 characters and avoid reusing a password from another service.</p>
        <label>New password<input type="password" [(ngModel)]="password" name="password" autocomplete="new-password" required minlength="8"></label>
        <label>Confirm password<input type="password" [(ngModel)]="confirmation" name="confirmation" autocomplete="new-password" required minlength="8"></label>
        <p class="notice" role="status" *ngIf="message">{{ message }}</p>
        <p class="error" role="alert" *ngIf="error">{{ error }}</p>
        <button class="button" type="submit" [disabled]="resetForm.invalid || loading || !!message">{{ loading ? 'Resetting…' : 'Reset password' }}</button>
        <p class="muted" *ngIf="message"><a routerLink="/login">Go to sign in</a></p>
      </form>
    </section>
  `
})
export class ResetPasswordComponent implements OnInit {
  userId = '';
  token = '';
  password = '';
  confirmation = '';
  message = '';
  error = '';
  loading = false;

  constructor(private readonly auth: AuthService, private readonly route: ActivatedRoute) {}

  ngOnInit(): void {
    this.userId = this.route.snapshot.queryParamMap.get('userId') ?? '';
    this.token = this.route.snapshot.queryParamMap.get('token') ?? '';
    if (!this.userId || !this.token) this.error = 'This password reset link is incomplete or invalid.';
  }

  submit(): void {
    if (this.loading || this.message || !this.userId || !this.token) return;
    if (this.password !== this.confirmation) {
      this.error = 'The passwords do not match.';
      return;
    }

    this.loading = true;
    this.error = '';
    this.auth.resetPassword(this.userId, this.token, this.password).subscribe({
      next: result => {
        this.loading = false;
        this.message = result.message;
      },
      error: response => {
        this.loading = false;
        const errors = response.error?.errors;
        this.error = errors ? Object.values(errors).join(' ') : response.error?.message ?? 'This password reset link could not be used.';
      }
    });
  }
}
