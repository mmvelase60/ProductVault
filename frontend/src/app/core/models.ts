export interface AuthResponse { accessToken: string; expiresAt: string; email: string; }
export interface Category { categoryId: number; name: string; categoryCode: string; isActive: boolean; productCount: number; rowVersion: string; }
export interface Product { productId: number; productCode: string; name: string; description?: string; price: number; categoryId: number; categoryName: string; imagePath?: string; rowVersion: string; }
export interface ProductPage { items: Product[]; page: number; pageSize: number; totalCount: number; }
export interface Dashboard { productCount: number; activeCategoryCount: number; totalCategoryCount: number; catalogueValue: number; recentProducts: RecentProduct[]; }
export interface RecentProduct { productId: number; name: string; productCode: string; price: number; categoryName: string; imagePath?: string; }
export interface ApiError { message?: string; errors?: Record<string, string>; }
