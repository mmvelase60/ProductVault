import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NgFor, NgIf } from '@angular/common';
import { ApiService } from '../../core/api.service';
import { Category } from '../../core/models';

@Component({ selector: 'pv-categories', imports: [FormsModule, NgFor, NgIf], template: `
  <section class="heading"><div><span class="eyebrow">Organisation</span><h1>Categories</h1><p>Organise products with categories that only you can manage.</p></div><button class="button" (click)="newCategory()">Add category</button></section>
  <p class="notice" *ngIf="message">{{ message }}</p>
  <section class="split-layout"><section class="card table-card"><table *ngIf="categories.length; else empty"><thead><tr><th>Category</th><th>Code</th><th>Products</th><th>Status</th><th></th></tr></thead><tbody><tr *ngFor="let category of categories"><td><strong>{{ category.name }}</strong></td><td><code>{{ category.categoryCode }}</code></td><td>{{ category.productCount }}</td><td><span [class]="category.isActive ? 'badge active' : 'badge'">{{ category.isActive ? 'Active' : 'Inactive' }}</span></td><td><button class="text-button" (click)="edit(category)">Edit</button></td></tr></tbody></table><ng-template #empty><div class="empty"><h3>No categories yet</h3><p>Create your first category before adding products.</p></div></ng-template></section>
  <form class="card form-card" *ngIf="showForm" (ngSubmit)="save()"><span class="eyebrow">{{ editing ? 'Edit category' : 'New category' }}</span><h2>{{ editing ? editing.name : 'Add category' }}</h2><label>Name<input [(ngModel)]="form.name" name="name" required></label><label>Category code<input [(ngModel)]="form.categoryCode" name="categoryCode" maxlength="6" (input)="formatCode()" placeholder="ABC123" required><small>3 letters followed by 3 numbers.</small></label><label class="check"><input type="checkbox" [(ngModel)]="form.isActive" name="isActive"> Active and available for products</label><p class="error" *ngIf="error">{{ error }}</p><div class="actions"><button class="button">Save category</button><button type="button" class="button secondary" (click)="showForm=false">Cancel</button></div></form></section>
` })
export class CategoriesComponent implements OnInit {
  categories: Category[] = []; editing?: Category; showForm = false; message = ''; error = '';
  form = { name: '', categoryCode: '', isActive: true };
  constructor(private readonly api: ApiService) {}
  ngOnInit(): void { this.load(); }
  load(): void { this.api.categories().subscribe({ next: result => this.categories = result }); }
  newCategory(): void { this.editing = undefined; this.form = { name: '', categoryCode: '', isActive: true }; this.error = ''; this.showForm = true; }
  edit(category: Category): void { this.editing = category; this.form = { name: category.name, categoryCode: category.categoryCode, isActive: category.isActive }; this.error = ''; this.showForm = true; }
  formatCode(): void { this.form.categoryCode = this.form.categoryCode.toUpperCase().replace(/[^A-Z0-9]/g, ''); }
  save(): void {
    if (this.editing) {
      this.api.updateCategory({ ...this.editing, ...this.form }).subscribe({ next: () => this.completeSave('Category updated.'), error: response => this.error = response.error?.message ?? 'Category could not be saved.' });
    } else {
      this.api.createCategory(this.form).subscribe({ next: () => this.completeSave('Category created.'), error: response => this.error = response.error?.message ?? 'Category could not be saved.' });
    }
  }
  private completeSave(message: string): void { this.message = message; this.showForm = false; this.load(); }
}
