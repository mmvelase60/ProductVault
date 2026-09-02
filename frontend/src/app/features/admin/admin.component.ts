import { NgFor, NgIf } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { ApiService } from '../../core/api.service';
import { AdminUser } from '../../core/models';

@Component({
  selector: 'pv-admin',
  imports: [NgFor, NgIf],
  template: `
    <section class="heading"><div><span class="eyebrow">Administration</span><h1>Workspace users</h1><p>Read-only account visibility for the configured ProductVault administrator.</p></div></section>
    <p class="error" role="alert" *ngIf="error">{{ error }}</p>
    <div class="card loading-state" role="status" aria-live="polite" *ngIf="loading"><span class="spinner" aria-hidden="true"></span><span>Loading users…</span></div>
    <section class="card table-card" *ngIf="users.length; else empty"><table><thead><tr><th>User</th><th>Username</th><th>Roles</th><th>Email status</th></tr></thead><tbody><tr *ngFor="let user of users"><td><strong>{{ user.firstName }} {{ user.surname }}</strong><small>{{ user.email }}</small></td><td><code>{{ user.username }}</code></td><td><span class="badge" *ngFor="let role of user.roles">{{ role }}</span></td><td><span [class]="user.emailConfirmed ? 'badge active' : 'badge'">{{ user.emailConfirmed ? 'Verified' : 'Unverified' }}</span></td></tr></tbody></table></section>
    <ng-template #empty><div class="card empty" *ngIf="!error && !loading"><h3>No user accounts found.</h3></div></ng-template>
  `
})
export class AdminComponent implements OnInit {
  users: AdminUser[] = []; error = ''; loading = true;
  constructor(private readonly api: ApiService, private readonly changeDetector: ChangeDetectorRef) {}
  ngOnInit(): void { this.api.adminUsers().subscribe({ next: users => { this.users = users; this.loading = false; this.changeDetector.detectChanges(); }, error: response => { this.loading = false; this.error = response.status === 403 ? 'Administrator access is required for this page.' : 'User accounts could not be loaded.'; this.changeDetector.detectChanges(); } }); }
}
