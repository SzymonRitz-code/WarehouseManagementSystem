import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { catchError, switchMap, throwError } from 'rxjs';
import { environment } from '../../environments/environment';

let isRedirectingToLogin = false;

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(OidcSecurityService);

  if (!req.url.startsWith(environment.apiUrl)) {
    return next(req);
  }

  return authService.getAccessToken().pipe(
    switchMap(token => {
      if (!token) {
        redirectToLogin(authService);

        return throwError(() => new HttpErrorResponse({
          status: 401,
          statusText: 'Unauthorized',
          url: req.url
        }));
      }

      const authReq = req.clone({
        headers: req.headers.set('Authorization', `Bearer ${token}`)
      });

      return next(authReq).pipe(
        catchError(error => {
          if (error instanceof HttpErrorResponse && error.status === 401) {
            redirectToLogin(authService);
          }

          return throwError(() => error);
        })
      );
    })
  );
};

function redirectToLogin(authService: OidcSecurityService): void {
  if (isRedirectingToLogin) {
    return;
  }

  isRedirectingToLogin = true;
  authService.logoffLocal();
  authService.authorize();
}
