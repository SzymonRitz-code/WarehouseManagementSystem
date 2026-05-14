import { APP_INITIALIZER, ApplicationConfig, importProvidersFrom, provideBrowserGlobalErrorListeners, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { AuthModule, OidcSecurityService } from 'angular-auth-oidc-client';

import { routes } from './app.routes';
import { firstValueFrom } from 'rxjs';

export const authConfig = {
  authority: `https://localhost:7079`,
  redirectUrl: `http://localhost:4200/signin-oidc`,
  postLogoutRedirectUri: `http://localhost:4200/signout-callback-oidc`,

  clientId: 'angular_spa',

  scope: 'openid profile offline_access wms.api',

  responseType: 'code',

  silentRenew: true,
  useRefreshToken: true,
  renewTimeBeforeTokenExpiresInSeconds: 30,
};

export function initAuth(oidc: OidcSecurityService) {
  return () => firstValueFrom(oidc.checkAuth());
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    {
      provide: APP_INITIALIZER,
      useFactory: initAuth,
      deps: [OidcSecurityService],
      multi: true
    },
    importProvidersFrom(
      AuthModule.forRoot({
        config: authConfig
      })
    ),
    provideRouter(routes)
  ]
};
