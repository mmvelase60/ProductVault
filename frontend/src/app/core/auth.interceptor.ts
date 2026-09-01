import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';
import { NotificationService } from './notification.service';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const notifications = inject(NotificationService);
  const token = auth.token;
  return next(token ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : request).pipe(
    catchError((response: HttpErrorResponse) => {
      if (response.status === 401 && auth.isAuthenticated) {
        auth.logout();
        notifications.show('Your session has ended. Please sign in again.', 'info');
        void router.navigateByUrl('/login');
      } else if (response.status === 429) {
        notifications.show(response.error?.detail ?? 'Too many attempts. Wait a minute, then try again.', 'error');
      } else if (response.status >= 500) {
        notifications.show(response.error?.detail ?? response.error?.message ?? 'Something went wrong on our side. Please try again.', 'error');
      }
      return throwError(() => response);
    })
  );
};
