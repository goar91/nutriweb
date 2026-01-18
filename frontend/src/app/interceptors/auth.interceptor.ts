import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const token = authService.getToken();

  console.log('[AUTH INTERCEPTOR] URL:', req.url);
  console.log('[AUTH INTERCEPTOR] Token presente:', !!token);

  if (token && !req.url.includes('/auth/login')) {
    const cloned = req.clone({
      headers: req.headers.set('Authorization', `Bearer ${token}`)
    });
    console.log('[AUTH INTERCEPTOR] Token agregado al header');
    return next(cloned);
  }

  console.log('[AUTH INTERCEPTOR] Request sin token');
  return next(req);
};
