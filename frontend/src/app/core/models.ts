export interface AuthResponse { accessToken: string; expiresAt: string; email: string; roles: string[]; }
export interface EmailActionResponse { message: string; code?: string; }
export interface Category { categoryId: number; name: string; categoryCode: string; isActive: boolean; productCount: number; rowVersion: string; }
export interface Product { productId: number; productCode: string; name: string; description?: string; price: number; quantityInStock: number; reorderLevel: number; isLowStock: boolean; categoryId: number; categoryName: string; imagePath?: string; rowVersion: string; }
export interface ProductPage { items: Product[]; page: number; pageSize: number; totalCount: number; }
export interface Dashboard { productCount: number; activeCategoryCount: number; totalCategoryCount: number; catalogueValue: number; lowStockCount: number; recentProducts: RecentProduct[]; activity: AuditEvent[]; }
export interface RecentProduct { productId: number; name: string; productCode: string; price: number; categoryName: string; imagePath?: string; }
export interface AuditEvent { action: string; entityType: string; entityName: string; detail?: string; occurredAt: string; }
export interface Profile { firstName: string; surname: string; username: string; email: string; roles: string[]; }
export interface AdminUser { id: string; firstName: string; surname: string; username: string; email: string; emailConfirmed: boolean; roles: string[]; }
export interface CatalogueImportError { rowNumber: number; productName: string; message: string; }
export interface CatalogueImportResult { categoriesCreated: number; productsCreated: number; productsSkipped: number; errors: CatalogueImportError[]; }
export interface StockMovement { inventoryMovementId: number; operation: string; quantityBefore: number; quantityAfter: number; note?: string; occurredAt: string; }
export interface StockUpdate { product: Product; movement: StockMovement; }
export interface ApiError { message?: string; errors?: Record<string, string>; }
