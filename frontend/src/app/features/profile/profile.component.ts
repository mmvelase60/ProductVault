import { NgFor, NgIf } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/api.service';
import { Profile } from '../../core/models';

@Component({
  selector: 'pv-profile',
  imports: [FormsModule, NgFor, NgIf],
  template: `
    <section class="heading"><div><span class="eyebrow">Account settings</span><h1>Your profile</h1><p>Manage your identity and keep your workspace access secure.</p></div></section>
    <p class="notice" role="status" *ngIf="message">{{ message }}</p><p class="error" role="alert" *ngIf="error">{{ error }}</p>
    <section class="split-layout" *ngIf="profile">
      <form class="card form-card" #profileForm="ngForm" (ngSubmit)="saveProfile()">
        <span class="eyebrow">Personal details</span><h2>{{ profile.email }}</h2>
        <label>First name<input [(ngModel)]="form.firstName" name="firstName" required maxlength="100"></label>
        <label>Surname<input [(ngModel)]="form.surname" name="surname" required maxlength="100"></label>
        <div class="identity-summary"><span>Generated username</span><strong>{{ profile.username }}</strong><small>It updates from your first name and surname, for example Mthokozisi Mvelase → MMvelase.</small></div>
        <div class="identity-summary"><span>Access roles</span><div><span class="badge" *ngFor="let role of profile.roles">{{ role }}</span></div></div>
        <button class="button" type="submit" [disabled]="profileForm.invalid || savingProfile">{{ savingProfile ? 'Saving…' : 'Save profile' }}</button>
      </form>
      <form class="card form-card" #passwordForm="ngForm" (ngSubmit)="changePassword()">
        <span class="eyebrow">Security</span><h2>Change password</h2>
        <label>Current password<input type="password" [(ngModel)]="password.currentPassword" name="currentPassword" required autocomplete="current-password"></label>
        <label>New password<input type="password" [(ngModel)]="password.newPassword" name="newPassword" required minlength="8" autocomplete="new-password"><small>Use at least 8 characters.</small></label>
        <button class="button secondary" type="submit" [disabled]="passwordForm.invalid || savingPassword">{{ savingPassword ? 'Updating…' : 'Change password' }}</button>
      </form>
    </section>
  `
})
export class ProfileComponent implements OnInit {
  profile?: Profile;
  form = { firstName: '', surname: '' };
  password = { currentPassword: '', newPassword: '' };
  message = ''; error = ''; savingProfile = false; savingPassword = false;
  constructor(private readonly api: ApiService) {}
  ngOnInit(): void { this.load(); }
  saveProfile(): void {
    if (this.savingProfile) return;
    this.savingProfile = true; this.error = ''; this.message = '';
    this.api.updateProfile(this.form).subscribe({ next: profile => { this.profile = profile; this.form = { firstName: profile.firstName, surname: profile.surname }; this.savingProfile = false; this.message = 'Profile updated.'; }, error: response => this.handleError(response, 'Profile could not be updated.', 'profile') });
  }
  changePassword(): void {
    if (this.savingPassword) return;
    this.savingPassword = true; this.error = ''; this.message = '';
    this.api.changePassword(this.password).subscribe({ next: response => { this.savingPassword = false; this.password = { currentPassword: '', newPassword: '' }; this.message = response.message; }, error: response => this.handleError(response, 'Password could not be changed.', 'password') });
  }
  private load(): void { this.api.profile().subscribe({ next: profile => { this.profile = profile; this.form = { firstName: profile.firstName, surname: profile.surname }; }, error: () => this.error = 'Profile details could not be loaded.' }); }
  private handleError(response: HttpErrorResponse, fallback: string, operation: 'profile' | 'password'): void { if (operation === 'profile') this.savingProfile = false; else this.savingPassword = false; this.error = response.error?.message ?? response.error?.errors?.profile ?? fallback; }
}
