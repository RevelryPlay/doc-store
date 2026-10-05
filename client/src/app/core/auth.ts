import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, throwError } from 'rxjs';
import { Session } from './session';

export const requireSignIn: CanActivateFn = (_route, state) => {
  const session = inject(Session);
  const router = inject(Router);
  const toLogin = router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });

  return session.isSignedIn()
    ? true
    : session.refresh().pipe(map((user) => (user ? true : toLogin)));
};

// Session endpoints answer 401 as part of their normal flow, so only other API calls redirect.
export const redirectOnUnauthorized: HttpInterceptorFn = (request, next) => {
  const session = inject(Session);
  const router = inject(Router);

  return next(request).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401 && !request.url.startsWith('/api/auth/')) {
        session.clear();
        router.navigate(['/login'], { queryParams: { returnUrl: router.url } });
      }
      return throwError(() => error);
    }),
  );
};
