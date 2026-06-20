import { inject } from '@angular/core';
import { CanActivateChildFn, CanActivateFn } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { map } from 'rxjs';

const checkAuthenticated = () => {
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

export const authGuard: CanActivateFn = () => checkAuthenticated();
export const authChildGuard: CanActivateChildFn = () => checkAuthenticated();
