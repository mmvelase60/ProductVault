import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Category, Dashboard, Product, ProductPage } from './models';
import { apiUrl } from './api.config';

@Injectable({ providedIn: 'root' })
export class ApiService {
  constructor(private readonly http: HttpClient) {}
  dashboard(): Observable<Dashboard> { return this.http.get<Dashboard>(`${apiUrl}/dashboard`); }
  seedDemoData(): Observable<{ message: string }> { return this.http.post<{ message: string }>(`${apiUrl}/dashboard/demo-data`, {}); }
  importCatalogue(file: File): Observable<{ categoriesCreated: number; productsCreated: number; productsSkipped: number }> { const body = new FormData(); body.append('file', file); return this.http.post<{ categoriesCreated: number; productsCreated: number; productsSkipped: number }>(`${apiUrl}/catalogue-imports/file`, body); }
  catalogueTemplate(): Observable<Blob> { return this.http.get(`${apiUrl}/catalogue-imports/template`, { responseType: 'blob' }); }
  categories(): Observable<Category[]> { return this.http.get<Category[]>(`${apiUrl}/categories`); }
  createCategory(input: { name: string; categoryCode: string; isActive: boolean }): Observable<Category> { return this.http.post<Category>(`${apiUrl}/categories`, input); }
  updateCategory(category: Category): Observable<void> { return this.http.put<void>(`${apiUrl}/categories/${category.categoryId}`, category); }
  products(filters: { page: number; search?: string; categoryId?: number; sort?: string }): Observable<ProductPage> {
    let params = new HttpParams().set('page', filters.page).set('pageSize', 10).set('sort', filters.sort ?? 'newest');
    if (filters.search) params = params.set('search', filters.search);
    if (filters.categoryId) params = params.set('categoryId', filters.categoryId);
    return this.http.get<ProductPage>(`${apiUrl}/products`, { params });
  }
  createProduct(input: { name: string; description?: string; price: number; categoryId: number }): Observable<Product> { return this.http.post<Product>(`${apiUrl}/products`, input); }
  updateProduct(product: Product): Observable<void> { return this.http.put<void>(`${apiUrl}/products/${product.productId}`, product); }
  deleteProduct(id: number): Observable<void> { return this.http.delete<void>(`${apiUrl}/products/${id}`); }
  uploadImage(id: number, file: File): Observable<Product> { const body = new FormData(); body.append('file', file); return this.http.post<Product>(`${apiUrl}/products/${id}/image`, body); }
  importProducts(file: File): Observable<{ imported: number }> { const body = new FormData(); body.append('file', file); return this.http.post<{ imported: number }>(`${apiUrl}/products/import`, body); }
  exportProducts(): Observable<Blob> { return this.http.get(`${apiUrl}/products/export`, { responseType: 'blob' }); }
}
