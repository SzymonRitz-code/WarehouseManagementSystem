import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { switchMap } from 'rxjs';
import { environment } from '../../environments/environment';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(OidcSecurityService);

  if (!req.url.startsWith(environment.apiUrl)) {
    return next(req);
  }

  return authService.getAccessToken().pipe(
    switchMap(token => {
      if (!token) return next(req);

      const authReq = req.clone({
        headers: req.headers.set('Authorization', `Bearer ${token}`)
      });

      return next(authReq);
    })
  );
};
