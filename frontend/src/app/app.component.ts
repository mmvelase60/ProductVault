import { Component } from '@angular/core';
import { AsyncPipe, NgFor, NgIf } from '@angular/common';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from './core/auth.service';
import { NotificationService } from './core/notification.service';

@Component({
  selector: 'pv-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, AsyncPipe, NgFor, NgIf],
  template: `
    <a class="skip-link" href="#main-content" (click)="skipToContent($event)">Skip to main content</a>
    <header class="app-header"><nav><a class="brand" routerLink="/dashboard"><span>PV</span>ProductVault</a>
      <ng-container *ngIf="auth.session$ | async as session; else guest"><button class="menu-toggle" type="button" (click)="menuOpen = !menuOpen" [attr.aria-expanded]="menuOpen" aria-controls="primary-navigation" aria-label="Toggle navigation"><span></span><span></span><span></span></button><div id="primary-navigation" class="nav-links" [class.open]="menuOpen"><a routerLink="/dashboard" routerLinkActive="active" (click)="menuOpen = false">Overview</a><a routerLink="/products" routerLinkActive="active" (click)="menuOpen = false">Products</a><a routerLink="/categories" routerLinkActive="active" (click)="menuOpen = false">Categories</a><a routerLink="/profile" routerLinkActive="active" (click)="menuOpen = false">Profile</a><a *ngIf="session.roles.includes('Admin')" routerLink="/admin" routerLinkActive="active" (click)="menuOpen = false">Admin</a></div><div class="account"><small>{{ session.email }}</small><button class="text-button" (click)="logout()">Sign out</button></div></ng-container>
      <ng-template #guest><div class="account"><a *ngIf="!isSignInPage" routerLink="/login">Sign in</a><a *ngIf="!isRegistrationPage" class="button compact" routerLink="/register">Create account</a></div></ng-template>
    </nav></header>
    <main id="main-content" class="page-shell" tabindex="-1"><router-outlet /></main>
    <aside class="toast-region" aria-label="Application notifications" aria-live="polite" aria-atomic="true">
      <div *ngFor="let notification of notifications.notifications$ | async" class="toast" [class.error-toast]="notification.kind === 'error'" [attr.role]="notification.kind === 'error' ? 'alert' : 'status'">
        <span>{{ notification.message }}</span><button type="button" class="toast-close" (click)="notifications.dismiss(notification.id)" aria-label="Dismiss notification">×</button>
      </div>
    </aside>
  `
})
export class AppComponent {
  menuOpen = false;
  constructor(readonly auth: AuthService, readonly notifications: NotificationService, private readonly router: Router) {}

  get isSignInPage(): boolean { return this.router.url.startsWith('/login'); }
  get isRegistrationPage(): boolean { return this.router.url.startsWith('/register'); }

  skipToContent(event: Event): void {
    event.preventDefault();
    document.getElementById('main-content')?.focus();
  }

  logout(): void {
    this.menuOpen = false;
    this.auth.logout();
    void this.router.navigateByUrl('/login');
  }
}
