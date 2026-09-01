import { Routes } from '@angular/router';
import { authGuard } from './core/auth.guard';
import { LoginComponent } from './features/auth/login.component';
import { RegisterComponent } from './features/auth/register.component';
import { VerifyEmailComponent } from './features/auth/verify-email.component';
import { ForgotPasswordComponent } from './features/auth/forgot-password.component';
import { ResetPasswordComponent } from './features/auth/reset-password.component';
import { ResendConfirmationComponent } from './features/auth/resend-confirmation.component';
import { DashboardComponent } from './features/dashboard/dashboard.component';
import { CategoriesComponent } from './features/categories/categories.component';
import { ProductsComponent } from './features/products/products.component';
import { CatalogueImportComponent } from './features/catalogue-import/catalogue-import.component';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { path: 'verify-email', component: VerifyEmailComponent },
  { path: 'forgot-password', component: ForgotPasswordComponent },
  { path: 'reset-password', component: ResetPasswordComponent },
  { path: 'resend-confirmation', component: ResendConfirmationComponent },
  { path: 'dashboard', component: DashboardComponent, canActivate: [authGuard] },
  { path: 'categories', component: CategoriesComponent, canActivate: [authGuard] },
  { path: 'products', component: ProductsComponent, canActivate: [authGuard] },
  { path: 'catalogue-import', component: CatalogueImportComponent, canActivate: [authGuard] },
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  { path: '**', redirectTo: 'dashboard' }
];
