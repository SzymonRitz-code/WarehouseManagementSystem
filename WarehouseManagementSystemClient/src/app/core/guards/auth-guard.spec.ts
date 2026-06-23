import { TestBed } from '@angular/core/testing';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { firstValueFrom, Observable, of } from 'rxjs';
import { authChildGuard, authGuard } from './auth-guard';

describe('auth guards', () => {
  let oidc: {
    isAuthenticated$: Observable<{ isAuthenticated: boolean }>;
    authorize: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    oidc = {
      isAuthenticated$: of({ isAuthenticated: true }),
      authorize: vi.fn()
    };

    TestBed.configureTestingModule({
      providers: [{ provide: OidcSecurityService, useValue: oidc }]
    });
  });

  it('allows route activation when the user is authenticated', async () => {
    oidc.isAuthenticated$ = of({ isAuthenticated: true });

    const result$ = TestBed.runInInjectionContext(() => authGuard({} as any, {} as any));

    await expect(firstValueFrom(result$ as any)).resolves.toBe(true);
    expect(oidc.authorize).not.toHaveBeenCalled();
  });

  it('starts authorize flow and blocks route activation when the user is unauthenticated', async () => {
    oidc.isAuthenticated$ = of({ isAuthenticated: false });

    const result$ = TestBed.runInInjectionContext(() => authGuard({} as any, {} as any));

    await expect(firstValueFrom(result$ as any)).resolves.toBe(false);
    expect(oidc.authorize).toHaveBeenCalledTimes(1);
  });

  it('uses the same authentication decision for child routes', async () => {
    oidc.isAuthenticated$ = of({ isAuthenticated: false });

    const result$ = TestBed.runInInjectionContext(() => authChildGuard({} as any, {} as any));

    await expect(firstValueFrom(result$ as any)).resolves.toBe(false);
    expect(oidc.authorize).toHaveBeenCalledTimes(1);
  });
});
