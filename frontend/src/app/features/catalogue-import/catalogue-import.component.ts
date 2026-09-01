import { NgIf } from '@angular/common';
import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/api.service';

@Component({
  selector: 'pv-catalogue-import',
  imports: [NgIf, RouterLink],
  template: `
    <section class="heading"><div><span class="eyebrow">Integration hub</span><h1>Import catalogue</h1><p>Bring categories and products in from a trusted file source or starter provider.</p></div><div class="actions"><a class="button secondary" routerLink="/products">View products</a></div></section>
    <p class="notice" *ngIf="message">{{ message }}</p><p class="error" role="alert" *ngIf="error">{{ error }}</p>
    <section class="split-layout">
      <article class="card form-card"><span class="eyebrow">Starter provider</span><h2>Load demo catalogue</h2><p class="muted">Simulates a trusted source importing three categories and five products into an empty workspace.</p><button class="button" type="button" [disabled]="seeding" (click)="seed()">{{ seeding ? 'Loading…' : 'Load starter data' }}</button></article>
      <article class="card form-card"><span class="eyebrow">File import</span><h2>CSV or Excel</h2><p class="muted">One file can create categories and products together. Maximum 500 rows.</p><input type="file" accept=".csv,.xlsx" aria-label="Choose CSV or Excel catalogue file" (change)="select($event)"><p class="muted">Columns: Category Name, Category Code, Category Active, Product Name, Description, Price.</p><div class="actions"><button class="button" type="button" [disabled]="!file || importing" (click)="import()">{{ importing ? 'Importing…' : 'Import catalogue' }}</button><button class="button secondary" type="button" (click)="downloadTemplate()">CSV template</button></div></article>
    </section>
    <section class="card form-card"><span class="eyebrow">Integration contract</span><h2>Ready for a future source system</h2><p class="muted">A future ERP, supplier, or POS adapter can submit the same authenticated CSV or Excel contract to <code>POST /api/catalogue-imports/file</code>. This keeps source-specific code outside the catalogue rules.</p></section>
  `
})
export class CatalogueImportComponent {
  file?: File; error = ''; message = ''; importing = false; seeding = false;
  constructor(private readonly api: ApiService) {}
  select(event: Event): void { this.file = (event.target as HTMLInputElement).files?.[0]; this.error = ''; this.message = ''; }
  import(): void { if (!this.file || this.importing) return; this.importing = true; this.api.importCatalogue(this.file).subscribe({ next: r => { this.importing = false; this.message = `Import complete: ${r.categoriesCreated} categories and ${r.productsCreated} products created; ${r.productsSkipped} duplicates skipped.`; }, error: e => { this.importing = false; this.error = e.error?.message ?? 'Catalogue import failed.'; } }); }
  seed(): void { if (this.seeding) return; this.seeding = true; this.api.seedDemoData().subscribe({ next: r => { this.seeding = false; this.message = r.message; }, error: e => { this.seeding = false; this.error = e.error?.message ?? 'Starter catalogue could not be loaded.'; } }); }
  downloadTemplate(): void { this.api.catalogueTemplate().subscribe({ next: blob => { const url = URL.createObjectURL(blob); const link = document.createElement('a'); link.href = url; link.download = 'productvault-catalogue-template.csv'; link.click(); URL.revokeObjectURL(url); }, error: () => this.error = 'The template could not be downloaded.' }); }
}
