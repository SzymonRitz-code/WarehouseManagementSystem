import { inject } from "@angular/core";
import { CanActivateFn } from "@angular/router";
import { OidcSecurityService } from "angular-auth-oidc-client";
import { map } from "rxjs";

// export const authGuard: CanActivateFn = () => {
//   const oidc = inject(OidcSecurityService);

//     return oidc.checkAuth().pipe(
//     map(({ isAuthenticated }) => {

//       if (isAuthenticated) {
//         return true;
//       }

//       oidc.authorize();
//       return false;
//     })
//   );

// //   if (oidc.isAuthenticated()) {
// //     console.log('User is authenticated');
// //     return true;
// //   }

// //   console.log('User is not authenticated, redirecting to login');
// //   oidc.authorize(); // 🔥 redirect
// //   return false;
// };
export const authGuard: CanActivateFn = () => {
  const oidc = inject(OidcSecurityService);

  return oidc.isAuthenticated$.pipe(
    map(({ isAuthenticated }) => {

      if (isAuthenticated) {
        return true;
      }

      oidc.authorize();
      return false;
    })
  );
};