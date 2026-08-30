import { CurrencyPipe, NgFor, NgIf } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/api.service';
import { apiBaseUrl } from '../../core/api.config';
import { Category, Product, ProductPage } from '../../core/models';

@Component({
  selector: 'pv-products',
  imports: [FormsModule, NgFor, NgIf, CurrencyPipe],
  template: `
    <section class="heading">
      <div>
        <span class="eyebrow">Catalogue</span>
        <h1>Products</h1>
        <p>{{ productCount }} product{{ productCount === 1 ? '' : 's' }} in your private workspace.</p>
      </div>
      <div class="actions">
        <button class="button secondary" type="button" [disabled]="exporting" (click)="download()">
          {{ exporting ? 'Preparing…' : 'Download Excel' }}
        </button>
        <button class="button" type="button" (click)="newProduct()">Add product</button>
      </div>
    </section>

    <section class="card filters" aria-label="Product filters">
      <input [(ngModel)]="search" (keyup.enter)="apply()" aria-label="Search products" placeholder="Search name, code, or description">
      <select [(ngModel)]="categoryId" aria-label="Filter by category">
        <option [ngValue]="undefined">All categories</option>
        <option *ngFor="let category of categories" [ngValue]="category.categoryId">{{ category.name }}</option>
      </select>
      <select [(ngModel)]="sort" aria-label="Sort products">
        <option value="newest">Newest first</option>
        <option value="name">Name A–Z</option>
        <option value="price-asc">Price: low to high</option>
        <option value="price-desc">Price: high to low</option>
        <option value="code">Product code</option>
      </select>
      <button class="button dark" type="button" (click)="apply()">Apply</button>
    </section>

    <details class="card importer">
      <summary><span><strong>Import from Excel</strong><small>Add up to 500 products at once</small></span><b>+</b></summary>
      <div>
        <input type="file" accept=".xlsx" aria-label="Choose an Excel workbook" (change)="selectImport($event)">
        <button class="button secondary" type="button" [disabled]="!importFile || importing" (click)="import()">
          {{ importing ? 'Importing…' : 'Import products' }}
        </button>
      </div>
    </details>

    <p class="notice" role="status" aria-live="polite" *ngIf="message">{{ message }}</p>
    <p class="error" role="alert" *ngIf="error">{{ error }}</p>

    <section class="split-layout products-layout">
      <section class="card table-card">
        <table *ngIf="page?.items?.length; else empty">
          <thead><tr><th>Product</th><th>Code</th><th>Category</th><th class="right">Price</th><th><span class="sr-only">Actions</span></th></tr></thead>
          <tbody>
            <tr *ngFor="let product of page.items">
              <td>
                <div class="product-name">
                  <img *ngIf="product.imagePath" [src]="apiBase + product.imagePath" [alt]="product.name + ' image'">
                  <span class="avatar" *ngIf="!product.imagePath" aria-hidden="true">{{ product.name.charAt(0) }}</span>
                  <span><strong>{{ product.name }}</strong><small>{{ product.description || 'No description' }}</small></span>
                </div>
              </td>
              <td><code>{{ product.productCode }}</code></td>
              <td><span class="badge">{{ product.categoryName }}</span></td>
              <td class="right"><strong>{{ product.price | currency:'ZAR':'symbol-narrow' }}</strong></td>
              <td>
                <button class="text-button" type="button" (click)="edit(product)">Edit</button>
                <button class="text-button danger" type="button" [disabled]="deletingId === product.productId" (click)="remove(product)">
                  {{ deletingId === product.productId ? 'Deleting…' : 'Delete' }}
                </button>
              </td>
            </tr>
          </tbody>
        </table>
        <ng-template #empty>
          <div class="empty"><h3>No products yet</h3><p>Add a product manually or import your existing catalogue.</p><button class="button" type="button" (click)="newProduct()">Add your first product</button></div>
        </ng-template>
        <div class="pagination" *ngIf="page && page.totalCount > 10">
          <button class="button secondary" type="button" [disabled]="page.page === 1" (click)="load(page.page - 1)">Previous</button>
          <span>Page {{ page.page }} of {{ totalPages }}</span>
          <button class="button secondary" type="button" [disabled]="page.page >= totalPages" (click)="load(page.page + 1)">Next</button>
        </div>
      </section>

      <form class="card form-card" *ngIf="showForm" #productForm="ngForm" (ngSubmit)="save()" [attr.aria-busy]="saving">
        <span class="eyebrow">{{ editing ? 'Edit product' : 'New product' }}</span>
        <h2>{{ editing ? editing.name : 'Add product' }}</h2>
        <label>Name<input [(ngModel)]="form.name" name="name" required></label>
        <label>Description<textarea [(ngModel)]="form.description" name="description" rows="3"></textarea></label>
        <div class="two-cols">
          <label>Price<input type="number" min="0.01" step="0.01" [(ngModel)]="form.price" name="price" required></label>
          <label>Category<select [(ngModel)]="form.categoryId" name="category" required><option [ngValue]="0">Select a category</option><option *ngFor="let category of activeCategories" [ngValue]="category.categoryId">{{ category.name }}</option></select></label>
        </div>
        <label>Image<input type="file" accept="image/jpeg,image/png,image/gif,image/webp" (change)="selectImage($event)"><small>JPG, PNG, GIF, or WEBP; maximum 5 MB.</small></label>
        <p class="error" role="alert" *ngIf="formError">{{ formError }}</p>
        <div class="actions">
          <button class="button" type="submit" [disabled]="productForm.invalid || saving">{{ saving ? 'Saving…' : 'Save product' }}</button>
          <button class="button secondary" type="button" [disabled]="saving" (click)="closeForm()">Cancel</button>
        </div>
      </form>
    </section>
  `
})
export class ProductsComponent implements OnInit {
  readonly apiBase = apiBaseUrl;
  categories: Category[] = [];
  page?: ProductPage;
  search = '';
  categoryId?: number;
  sort = 'newest';
  showForm = false;
  editing?: Product;
  image?: File;
  importFile?: File;
  message = '';
  error = '';
  formError = '';
  saving = false;
  importing = false;
  exporting = false;
  deletingId?: number;
  form = { name: '', description: '', price: 0, categoryId: 0 };

  constructor(private readonly api: ApiService) {}

  get productCount(): number { return this.page?.totalCount ?? 0; }
  get totalPages(): number { return this.page ? Math.max(1, Math.ceil(this.page.totalCount / this.page.pageSize)) : 1; }
  get activeCategories(): Category[] { return this.categories.filter(category => category.isActive); }

  ngOnInit(): void {
    this.api.categories().subscribe({
      next: categories => this.categories = categories,
      error: () => this.error = 'Categories could not be loaded.'
    });
    this.load(1);
  }

  load(page: number): void {
    this.error = '';
    this.api.products({ page, search: this.search, categoryId: this.categoryId, sort: this.sort }).subscribe({
      next: result => this.page = result,
      error: response => this.error = response.error?.message ?? 'Products could not be loaded.'
    });
  }

  apply(): void { this.load(1); }

  newProduct(): void {
    this.editing = undefined;
    this.image = undefined;
    this.form = { name: '', description: '', price: 0, categoryId: 0 };
    this.formError = '';
    this.showForm = true;
  }

  edit(product: Product): void {
    this.editing = product;
    this.image = undefined;
    this.form = { name: product.name, description: product.description ?? '', price: product.price, categoryId: product.categoryId };
    this.formError = '';
    this.showForm = true;
  }

  closeForm(): void {
    this.showForm = false;
    this.formError = '';
  }

  selectImage(event: Event): void { this.image = (event.target as HTMLInputElement).files?.[0]; }
  selectImport(event: Event): void { this.importFile = (event.target as HTMLInputElement).files?.[0]; }

  save(): void {
    if (this.saving || !this.form.categoryId || this.form.price <= 0) {
      this.formError = 'Select a category and enter a valid price.';
      return;
    }

    this.saving = true;
    this.formError = '';
    if (this.editing) {
      this.api.updateProduct({ ...this.editing, ...this.form }).subscribe({
        next: () => this.afterSave(this.editing!),
        error: response => this.handleSaveError(response)
      });
      return;
    }

    this.api.createProduct(this.form).subscribe({
      next: product => this.afterSave(product),
      error: response => this.handleSaveError(response)
    });
  }

  remove(product: Product): void {
    if (!confirm(`Delete ${product.name}?`)) return;
    this.deletingId = product.productId;
    this.error = '';
    this.api.deleteProduct(product.productId).subscribe({
      next: () => {
        this.deletingId = undefined;
        this.message = 'Product deleted.';
        this.load(this.page?.page ?? 1);
      },
      error: response => {
        this.deletingId = undefined;
        this.error = response.error?.message ?? 'Product could not be deleted.';
      }
    });
  }

  import(): void {
    if (!this.importFile || this.importing) return;
    this.importing = true;
    this.error = '';
    this.api.importProducts(this.importFile).subscribe({
      next: result => {
        this.importing = false;
        this.message = `${result.imported} products imported.`;
        this.importFile = undefined;
        this.load(1);
      },
      error: response => {
        this.importing = false;
        this.error = response.error?.message ?? 'Import failed.';
      }
    });
  }

  download(): void {
    if (this.exporting) return;
    this.exporting = true;
    this.error = '';
    this.api.exportProducts().subscribe({
      next: blob => {
        this.exporting = false;
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = 'products.xlsx';
        link.click();
        URL.revokeObjectURL(url);
      },
      error: () => {
        this.exporting = false;
        this.error = 'The Excel file could not be created.';
      }
    });
  }

  private afterSave(product: Product): void {
    if (!this.image) {
      this.completeSave();
      return;
    }

    this.api.uploadImage(product.productId, this.image).subscribe({
      next: () => this.completeSave(),
      error: response => this.handleSaveError(response, 'Product saved, but image upload failed.')
    });
  }

  private completeSave(): void {
    this.saving = false;
    this.message = this.editing ? 'Product updated.' : 'Product created.';
    this.showForm = false;
    this.load(this.page?.page ?? 1);
  }

  private handleSaveError(response: HttpErrorResponse, fallback = 'Product could not be saved.'): void {
    this.saving = false;
    this.formError = response.error?.message ?? fallback;
  }
}
