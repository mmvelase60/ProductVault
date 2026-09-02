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
  const isSessionRequest = request.url.endsWith('/auth/refresh') || request.url.endsWith('/auth/logout');
  return next(token && !isSessionRequest ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : request).pipe(
    catchError((response: HttpErrorResponse) => {
      if (response.status === 401 && auth.isAuthenticated) {
        auth.clearSession();
        notifications.show('Your session has ended. Please sign in again.', 'info');
        void router.navigateByUrl('/login');
      } else if (response.status === 429) {
        notifications.showDialog({ title: 'Please wait before trying again', message: response.error?.detail ?? 'Too many attempts were made. Wait one minute, then try again.', kind: 'info' });
      } else if (response.status >= 500) {
        notifications.showDialog({ title: 'We could not complete that request', message: response.error?.detail ?? response.error?.message ?? 'Something went wrong on our side. Your information has not been saved. Please try again, or request a new verification code if this was an email verification.', kind: 'error' });
      }
      return throwError(() => response);
    })
  );
};
