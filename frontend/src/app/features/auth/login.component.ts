import { NgIf } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'pv-login',
  imports: [FormsModule, RouterLink, NgIf],
  template: `
    <section class="auth-layout">
      <form class="auth-card" #loginForm="ngForm" (ngSubmit)="submit()" [attr.aria-busy]="loading">
        <span class="eyebrow">Welcome back</span>
        <h1>Sign in to ProductVault</h1>
        <p>Manage your private product catalogue.</p>
        <label>Email<input type="email" [(ngModel)]="email" name="email" autocomplete="email" required></label>
        <label>Password<input type="password" [(ngModel)]="password" name="password" autocomplete="current-password" required minlength="8"></label>
        <p class="error" role="alert" *ngIf="error">{{ error }}</p>
        <p class="notice" role="status" aria-live="polite" *ngIf="notice">{{ notice }}</p>
        <button class="button" type="submit" [disabled]="loginForm.invalid || loading">{{ loading ? 'Signing in…' : 'Sign in' }}</button>
        <p class="muted"><a routerLink="/forgot-password">Forgot your password?</a></p>
        <p class="muted" *ngIf="confirmationRequired"><a routerLink="/resend-confirmation" [queryParams]="{ email }">Resend verification code</a></p>
        <p class="muted">New here? <a routerLink="/register">Create an account</a></p>
      </form>
    </section>
  `
})
export class LoginComponent {
  email = '';
  password = '';
  error = '';
  notice = '';
  loading = false;
  confirmationRequired = false;

  constructor(private readonly auth: AuthService, private readonly router: Router, route: ActivatedRoute) {
    this.email = route.snapshot.queryParamMap.get('email') ?? '';
    if (route.snapshot.queryParamMap.get('verified') === '1')
      this.notice = 'Email verified. You can now sign in.';
    else if (route.snapshot.queryParamMap.get('registered') === '1')
      this.notice = 'Account created. Check your email and enter the verification code before signing in.';
  }

  submit(): void {
    if (this.loading) return;
    this.loading = true;
    this.error = '';
    this.confirmationRequired = false;
    this.auth.login(this.email, this.password).subscribe({
      next: () => this.router.navigateByUrl('/dashboard'),
      error: response => {
        this.error = response.error?.message ?? 'Sign in failed.';
        this.confirmationRequired = response.error?.code === 'email_confirmation_required';
        this.loading = false;
      }
    });
  }
}
