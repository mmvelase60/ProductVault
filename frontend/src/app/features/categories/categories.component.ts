import { NgFor, NgIf } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectorRef, Component, HostListener, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/api.service';
import { Category } from '../../core/models';

@Component({
  selector: 'pv-categories',
  imports: [FormsModule, NgFor, NgIf],
  template: `
    <section class="heading">
      <div>
        <span class="eyebrow">Organisation</span>
        <h1>Categories</h1>
        <p>Organise products with categories that only you can manage.</p>
      </div>
      <button class="button" type="button" (click)="newCategory()">Add category</button>
    </section>

    <p class="notice" role="status" aria-live="polite" *ngIf="message">{{ message }}</p>
    <p class="error" role="alert" *ngIf="error && !showForm">{{ error }}</p>

    <section class="table-layout">
      <section class="card table-card" [attr.aria-busy]="loading">
        <div class="loading-state" role="status" aria-live="polite" *ngIf="loading"><span class="spinner" aria-hidden="true"></span><span>Loading categories…</span></div>
        <ng-container *ngIf="!loading">
        <table *ngIf="categories.length; else empty">
          <thead><tr><th>Category</th><th>Code</th><th>Products</th><th>Status</th><th><span class="sr-only">Actions</span></th></tr></thead>
          <tbody>
            <tr *ngFor="let category of categories">
              <td><strong>{{ category.name }}</strong></td>
              <td><code>{{ category.categoryCode }}</code></td>
              <td>{{ category.productCount }}</td>
              <td><span [class]="category.isActive ? 'badge active' : 'badge'">{{ category.isActive ? 'Active' : 'Inactive' }}</span></td>
              <td><button class="text-button" type="button" (click)="edit(category)">Edit</button></td>
            </tr>
          </tbody>
        </table>
        <ng-template #empty><div class="empty"><h3>No categories yet</h3><p>Create your first category before adding products.</p></div></ng-template>
        </ng-container>
      </section>
    </section>

    <section class="editor-backdrop" *ngIf="showForm" (click)="closeForm()">
      <form class="editor-dialog form-card" role="dialog" aria-modal="true" aria-labelledby="category-editor-title" #categoryForm="ngForm" (click)="$event.stopPropagation()" (ngSubmit)="save()" [attr.aria-busy]="saving">
        <div class="editor-header"><div><span class="eyebrow">{{ editing ? 'Edit category' : 'New category' }}</span><h2 id="category-editor-title">{{ editing ? editing.name : 'Add category' }}</h2></div><button class="editor-close" type="button" [disabled]="saving" (click)="closeForm()" aria-label="Close category editor">×</button></div>
        <label>Name<input [(ngModel)]="form.name" name="name" required></label>
        <label>Category code<input [(ngModel)]="form.categoryCode" name="categoryCode" maxlength="6" (input)="formatCode()" placeholder="ABC123" required><small>3 letters followed by 3 numbers.</small></label>
        <label class="check"><input type="checkbox" [(ngModel)]="form.isActive" name="isActive"> Active and available for products</label>
        <p class="error" role="alert" *ngIf="error">{{ error }}</p>
        <div class="actions">
          <button class="button" type="submit" [disabled]="categoryForm.invalid || saving">{{ saving ? 'Saving…' : 'Save category' }}</button>
          <button class="button secondary" type="button" [disabled]="saving" (click)="closeForm()">Cancel</button>
        </div>
      </form>
    </section>
  `
})
export class CategoriesComponent implements OnInit {
  categories: Category[] = [];
  editing?: Category;
  showForm = false;
  message = '';
  error = '';
  loading = false;
  saving = false;
  form = { name: '', categoryCode: '', isActive: true };

  constructor(private readonly api: ApiService, private readonly changeDetector: ChangeDetectorRef) {}

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading = true;
    this.api.categories().subscribe({
      next: result => {
        this.categories = result;
        this.loading = false;
        this.changeDetector.detectChanges();
      },
      error: () => {
        this.error = 'Categories could not be loaded.';
        this.loading = false;
        this.changeDetector.detectChanges();
      }
    });
  }

  newCategory(): void {
    this.editing = undefined;
    this.form = { name: '', categoryCode: '', isActive: true };
    this.error = '';
    this.showForm = true;
  }

  edit(category: Category): void {
    this.editing = category;
    this.form = { name: category.name, categoryCode: category.categoryCode, isActive: category.isActive };
    this.error = '';
    this.showForm = true;
  }

  closeForm(): void {
    if (this.saving) return;
    this.showForm = false;
    this.error = '';
  }

  @HostListener('document:keydown.escape')
  closeOnEscape(): void { this.closeForm(); }

  formatCode(): void { this.form.categoryCode = this.form.categoryCode.toUpperCase().replace(/[^A-Z0-9]/g, ''); }

  save(): void {
    if (this.saving) return;
    this.saving = true;
    this.error = '';
    if (this.editing) {
      this.api.updateCategory({ ...this.editing, ...this.form }).subscribe({
        next: () => this.completeSave('Category updated.'),
        error: response => this.handleError(response)
      });
      return;
    }

    this.api.createCategory(this.form).subscribe({
      next: () => this.completeSave('Category created.'),
      error: response => this.handleError(response)
    });
  }

  private completeSave(message: string): void {
    this.saving = false;
    this.message = message;
    this.showForm = false;
    this.load();
  }

  private handleError(response: HttpErrorResponse): void {
    this.saving = false;
    this.error = response.error?.message ?? response.error?.errors?.category ?? 'Category could not be saved.';
  }
}
