import { Component } from '@angular/core';
import { AsyncPipe, NgIf } from '@angular/common';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from './core/auth.service';

@Component({
  selector: 'pv-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, AsyncPipe, NgIf],
  template: `
    <header class="app-header"><nav><a class="brand" routerLink="/dashboard"><span>PV</span>ProductVault</a>
      <ng-container *ngIf="auth.session$ | async as session; else guest"><button class="menu-toggle" type="button" (click)="menuOpen = !menuOpen" [attr.aria-expanded]="menuOpen" aria-controls="primary-navigation" aria-label="Toggle navigation"><span></span><span></span><span></span></button><div id="primary-navigation" class="nav-links" [class.open]="menuOpen"><a routerLink="/dashboard" routerLinkActive="active" (click)="menuOpen = false">Overview</a><a routerLink="/products" routerLinkActive="active" (click)="menuOpen = false">Products</a><a routerLink="/categories" routerLinkActive="active" (click)="menuOpen = false">Categories</a><a routerLink="/profile" routerLinkActive="active" (click)="menuOpen = false">Profile</a><a *ngIf="session.roles.includes('Admin')" routerLink="/admin" routerLinkActive="active" (click)="menuOpen = false">Admin</a></div><div class="account"><small>{{ session.email }}</small><button class="text-button" (click)="logout()">Sign out</button></div></ng-container>
      <ng-template #guest><div class="account"><a *ngIf="!isSignInPage" routerLink="/login">Sign in</a><a *ngIf="!isRegistrationPage" class="button compact" routerLink="/register">Create account</a></div></ng-template>
    </nav></header>
    <main class="page-shell"><router-outlet /></main>
  `
})
export class AppComponent {
  menuOpen = false;
  constructor(readonly auth: AuthService, private readonly router: Router) {}

  get isSignInPage(): boolean { return this.router.url.startsWith('/login'); }
  get isRegistrationPage(): boolean { return this.router.url.startsWith('/register'); }

  logout(): void {
    this.menuOpen = false;
    this.auth.logout();
    void this.router.navigateByUrl('/login');
  }
}
