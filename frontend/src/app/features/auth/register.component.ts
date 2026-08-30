import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NgIf } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';

@Component({ selector: 'pv-register', imports: [FormsModule, RouterLink, NgIf], template: `<section class="auth-layout"><form class="auth-card" (ngSubmit)="submit()"><span class="eyebrow">Private workspace</span><h1>Create your account</h1><p>Start managing categories and products securely.</p><label>Email<input type="email" [(ngModel)]="email" name="email" required autofocus></label><label>Password<input type="password" [(ngModel)]="password" name="password" required minlength="8"><small>At least 8 characters.</small></label><p class="error" *ngIf="error">{{ error }}</p><button class="button" [disabled]="loading">{{ loading ? 'Creating…' : 'Create account' }}</button><p class="muted">Already registered? <a routerLink="/login">Sign in</a></p></form></section>` })
export class RegisterComponent {
  email = ''; password = ''; error = ''; loading = false;
  constructor(private readonly auth: AuthService, private readonly router: Router) {}
  submit(): void { this.loading = true; this.error = ''; this.auth.register(this.email, this.password).subscribe({ next: () => this.router.navigateByUrl('/dashboard'), error: error => { const errors = error.error?.errors; this.error = errors ? Object.values(errors).join(' ') : 'Registration failed.'; this.loading = false; } }); }
}
