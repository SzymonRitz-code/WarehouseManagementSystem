import { HttpClient, HttpErrorResponse, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { firstValueFrom, of } from 'rxjs';
import { environment } from '../../environments/environment';
import { authInterceptor } from './auth.interceptor';

describe('authInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let oidc: {
    getAccessToken: ReturnType<typeof vi.fn>;
    logoffLocal: ReturnType<typeof vi.fn>;
    authorize: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    oidc = {
      getAccessToken: vi.fn().mockReturnValue(of('access-token')),
      logoffLocal: vi.fn(),
      authorize: vi.fn()
    };

    TestBed.configureTestingModule({
      providers: [
        { provide: OidcSecurityService, useValue: oidc },
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting()
      ]
    });

    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('does not request a token or add Authorization header to non API requests', () => {
    http.get('https://example.com/public').subscribe();

    const req = httpMock.expectOne('https://example.com/public');

    expect(req.request.headers.has('Authorization')).toBe(false);
    expect(oidc.getAccessToken).not.toHaveBeenCalled();

    req.flush({});
  });

  it('adds bearer token to API requests', () => {
    http.get(`${environment.apiUrl}/warehouses`).subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/warehouses`);

    expect(oidc.getAccessToken).toHaveBeenCalledTimes(1);
    expect(req.request.headers.get('Authorization')).toBe('Bearer access-token');

    req.flush([]);
  });

  it('redirects to login and fails with 401 when API request has no token', async () => {
    oidc.getAccessToken.mockReturnValue(of(''));

    const error = await firstValueFrom(http.get(`${environment.apiUrl}/warehouses`).pipe()).catch(
      caughtError => caughtError
    );

    expect(error).toBeInstanceOf(HttpErrorResponse);
    expect(error.status).toBe(401);
    expect(oidc.logoffLocal).toHaveBeenCalledTimes(1);
    expect(oidc.authorize).toHaveBeenCalledTimes(1);
  });
});
