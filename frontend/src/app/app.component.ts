import { Component } from '@angular/core';
import { AsyncPipe, NgIf } from '@angular/common';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from './core/auth.service';

@Component({
  selector: 'pv-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, AsyncPipe, NgIf],
  template: `
    <header class="app-header"><nav><a class="brand" routerLink="/dashboard"><span>PV</span>ProductVault</a>
      <ng-container *ngIf="auth.session$ | async as session; else guest"><div class="nav-links"><a routerLink="/dashboard" routerLinkActive="active">Overview</a><a routerLink="/products" routerLinkActive="active">Products</a><a routerLink="/categories" routerLinkActive="active">Categories</a></div><div class="account"><small>{{ session.email }}</small><button class="text-button" (click)="logout()">Sign out</button></div></ng-container>
      <ng-template #guest><div class="account"><a routerLink="/login">Sign in</a><a class="button compact" routerLink="/register">Create account</a></div></ng-template>
    </nav></header>
    <main class="page-shell"><router-outlet /></main>
  `
})
export class AppComponent {
  constructor(readonly auth: AuthService) {}
  logout(): void { this.auth.logout(); }
}
