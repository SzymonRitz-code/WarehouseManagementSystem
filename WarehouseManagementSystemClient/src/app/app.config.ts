import { APP_INITIALIZER, ApplicationConfig, importProvidersFrom, provideBrowserGlobalErrorListeners, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { AuthModule, OidcSecurityService } from 'angular-auth-oidc-client';

import { routes } from './app.routes';
import { firstValueFrom } from 'rxjs';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { authInterceptor } from './core/interceptors/auth.interceptor';

export const authConfig = {
  authority: `https://localhost:44380`,
  redirectUrl: `https://localhost:4200/signin-oidc`,
  postLogoutRedirectUri: `https://localhost:4200/signout-callback-oidc`,

  clientId: 'angular_spa',

  scope: 'openid profile offline_access wms.api',

  responseType: 'code',
  secureRoutes: ['https://localhost:44377/api'],
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
    provideHttpClient(withInterceptors([
      authInterceptor
    ])),
    importProvidersFrom(
      AuthModule.forRoot({
        config: authConfig
      })
    ),
    provideRouter(routes)
  ]
};
