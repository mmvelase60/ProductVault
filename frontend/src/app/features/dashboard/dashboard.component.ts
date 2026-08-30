import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { CurrencyPipe, NgFor, NgIf } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { Dashboard } from '../../core/models';

@Component({ selector: 'pv-dashboard', imports: [NgIf, NgFor, CurrencyPipe, RouterLink], template: `
  <section class="heading"><div><span class="eyebrow">Private workspace</span><h1>Welcome back.</h1><p>Keep your catalogue accurate, organised, and ready to share.</p></div><div class="actions"><a class="button secondary" routerLink="/categories">Add category</a><a class="button" routerLink="/products">Add product</a></div></section>
  <ng-container *ngIf="dashboard as data; else loading"><section class="metrics"><article><span>Products</span><strong>{{ data.productCount }}</strong><small>Your private catalogue</small></article><article><span>Active categories</span><strong>{{ data.activeCategoryCount }}</strong><small>of {{ data.totalCategoryCount }} total</small></article><article><span>Catalogue value</span><strong>{{ data.catalogueValue | currency:'ZAR':'symbol-narrow' }}</strong><small>Based on current prices</small></article></section>
  <section class="card"><div class="card-title"><div><h2>Recently added</h2><p>Latest products in this workspace.</p></div><a routerLink="/products">View all</a></div><div class="recent" *ngIf="data.recentProducts.length; else empty"><a *ngFor="let product of data.recentProducts" routerLink="/products"><span class="avatar">{{ product.name.charAt(0) }}</span><span><strong>{{ product.name }}</strong><small>{{ product.categoryName }} · {{ product.productCode }}</small></span><b>{{ product.price | currency:'ZAR':'symbol-narrow' }}</b></a></div><ng-template #empty><div class="empty"><h3>Your catalogue is ready.</h3><p>Create a category, then add your first product.</p><a class="button" routerLink="/categories">Create a category</a></div></ng-template></section></ng-container>
  <ng-template #loading><p class="muted">{{ error || 'Loading dashboard…' }}</p></ng-template>
` })
export class DashboardComponent implements OnInit {
  dashboard?: Dashboard;
  error = '';
  constructor(private readonly api: ApiService, private readonly changeDetector: ChangeDetectorRef) {}
  ngOnInit(): void {
    this.api.dashboard().subscribe({
      next: result => { this.dashboard = result; this.changeDetector.detectChanges(); },
      error: () => { this.error = 'Dashboard could not be loaded.'; this.changeDetector.detectChanges(); }
    });
  }
}
