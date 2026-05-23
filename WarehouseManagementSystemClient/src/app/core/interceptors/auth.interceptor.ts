// import { HttpInterceptorFn } from "@angular/common/http";
// import { inject } from "@angular/core";
// import { OidcSecurityService } from "angular-auth-oidc-client";
// import { firstValueFrom } from "rxjs";

// export const authInterceptor:HttpInterceptorFn = 
//   (req, next, authService = inject(OidcSecurityService)) => {
//     const allowedOrigins = [
//       'https://localhost:7251/api'
//     ];

//     if(!!allowedOrigins.find(origin => req.url.includes(origin))) {
//     //   const authToken = firstValueFrom();
//       authService.getAccessToken().subscribe(token => {
//         console.log('Adding auth token to request', token);
//         const headers = req.headers.set('Authorization', `Bearer ${token}`);
//         req = req.clone({ headers });
//       });
//     //   console.log('Adding auth token to request', authToken);
//     //   const headers = req.headers.set('Authorization', `Bearer ${authToken}`);
//     //   req = req.clone({ headers });
//     }

//     return next(req);
// }

import { HttpInterceptorFn } from "@angular/common/http";
import { inject } from "@angular/core";
import { OidcSecurityService } from "angular-auth-oidc-client";
import { switchMap } from "rxjs";

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(OidcSecurityService);

  const allowedOrigins = ['https://localhost:44377/api'];

  if (!allowedOrigins.find(origin => req.url.startsWith(origin))) {
    // console.log('Request URL does not match allowed origins, skipping auth token');
    // console.log('Request URL:', req.url);
    // console.log('Origin:', origin);
    return next(req);
  }

  return authService.getAccessToken().pipe(
    switchMap(token => {
      // console.log('Adding auth token to request', token);
      if (!token) return next(req);
      const authReq = req.clone({
        headers: req.headers.set('Authorization', `Bearer ${token}`)
      });
      return next(authReq);
    })
  );
};