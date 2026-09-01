import { NgIf } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'pv-verify-email',
  imports: [FormsModule, NgIf, RouterLink],
  template: `
    <section class="auth-layout">
      <form class="auth-card" #verifyForm="ngForm" (ngSubmit)="submit()" [attr.aria-busy]="loading">
        <span class="eyebrow">Account security</span>
        <h1>Verify your email</h1>
        <p>Enter the six-digit verification code sent to <strong>{{ email }}</strong>.</p>
        <label>
          Verification code
          <input type="text" inputmode="numeric" autocomplete="one-time-code" maxlength="6" pattern="[0-9]{6}"
                 [(ngModel)]="code" (ngModelChange)="formatCode()" name="code" required autofocus>
          <small>The code expires after 10 minutes.</small>
        </label>
        <p class="error" role="alert" *ngIf="error">{{ error }}</p>
        <button class="button" type="submit" [disabled]="verifyForm.invalid || loading || !email">
          {{ loading ? 'Verifying…' : 'Verify email' }}
        </button>
        <p class="muted">Did not receive a code? <a routerLink="/resend-confirmation" [queryParams]="{ email }">Resend code</a></p>
        <p class="muted">Already verified? <a routerLink="/login" [queryParams]="{ email }">Sign in</a></p>
      </form>
    </section>
  `
})
export class VerifyEmailComponent {
  email = '';
  code = '';
  error = '';
  loading = false;

  constructor(private readonly auth: AuthService, private readonly router: Router, route: ActivatedRoute) {
    this.email = route.snapshot.queryParamMap.get('email') ?? '';
    if (!this.email) this.error = 'Enter your email address on the registration page, then verify the code we send.';
  }

  formatCode(): void {
    this.code = this.code.replace(/\D/g, '').slice(0, 6);
  }

  submit(): void {
    if (this.loading || !this.email) return;
    this.loading = true;
    this.error = '';
    this.auth.verifyEmailCode(this.email, this.code).subscribe({
      next: () => this.router.navigate(['/login'], { queryParams: { email: this.email, verified: '1' } }),
      error: response => {
        this.error = response.error?.message ?? 'The verification code could not be used.';
        this.loading = false;
      }
    });
  }
}
