import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../environments/environment';
import { StockService } from './stock-service';

describe('StockService', () => {
  let service: StockService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        StockService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(StockService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('sends server-side list query parameters for stocks', () => {
    service.getStocks({
      page: 4,
      pageSize: 100,
      search: 'SKU-001',
      warehouseId: 'wh-1',
      zoneId: 'zone-1',
      availableOnly: true,
      sortBy: 'quantityAvailable',
      sortDirection: 'asc'
    }).subscribe();

    const req = httpMock.expectOne(request =>
      request.method === 'GET' && request.url === `${environment.apiUrl}/stocks`
    );

    expect(req.request.params.get('page')).toBe('4');
    expect(req.request.params.get('pageSize')).toBe('100');
    expect(req.request.params.get('search')).toBe('SKU-001');
    expect(req.request.params.get('warehouseId')).toBe('wh-1');
    expect(req.request.params.get('zoneId')).toBe('zone-1');
    expect(req.request.params.get('availableOnly')).toBe('true');
    expect(req.request.params.get('sortBy')).toBe('quantityAvailable');
    expect(req.request.params.get('sortDirection')).toBe('asc');

    req.flush({ items: [], page: 4, pageSize: 100, totalItems: 0, totalPages: 0 });
  });

  it('does not send empty optional filters to stocks endpoint', () => {
    service.getStocks({
      page: 1,
      pageSize: 10,
      sortBy: 'lastUpdated',
      sortDirection: 'desc'
    }).subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/stocks?page=1&pageSize=10&sortBy=lastUpdated&sortDirection=desc`);

    expect(req.request.params.has('search')).toBe(false);
    expect(req.request.params.has('warehouseId')).toBe(false);
    expect(req.request.params.has('zoneId')).toBe(false);
    expect(req.request.params.has('availableOnly')).toBe(false);

    req.flush({ items: [], page: 1, pageSize: 10, totalItems: 0, totalPages: 0 });
  });

  it('gets stock availability from the dedicated endpoint', () => {
    service.getAvailableStocks().subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/stocks/availability`);

    expect(req.request.method).toBe('GET');
    req.flush([]);
  });
});
