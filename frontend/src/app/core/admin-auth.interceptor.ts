import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { from, switchMap } from 'rxjs';
import { AuthService } from './auth.service';

/** Hängt an Aufrufe von /api/admin/* das Entra-Access-Token an. */
export const adminAuthInterceptor: HttpInterceptorFn = (req, next) => {
  if (!req.url.includes('/api/admin')) return next(req);
  const auth = inject(AuthService);
  return from(auth.getToken()).pipe(
    switchMap(token =>
      next(token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req))
  );
};
