import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { importProvidersFrom } from '@angular/core';
import { provideRouter } from '@angular/router';
import { AuthModule } from 'angular-auth-oidc-client';

const providers = [
  provideRouter([]),
  importProvidersFrom(
    AuthModule.forRoot({
      config: {
        authority: 'https://localhost:44380',
        redirectUrl: 'https://localhost:4200/signin-oidc',
        postLogoutRedirectUri: 'https://localhost:4200/signout-callback-oidc',
        clientId: 'angular_spa',
        scope: 'openid profile offline_access wms.api',
        responseType: 'code',
        secureRoutes: ['https://localhost:44377/api']
      }
    })
  ),
  provideHttpClient(),
  provideHttpClientTesting()
];

export default providers;
