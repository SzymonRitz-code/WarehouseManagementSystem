import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { UnitOfMeasure } from '../../../core/enums/unitOfMeasure';
import { environment } from '../../../environments/environment';
import { CreateProduct } from '../model/create-product';
import { ProductService } from './product-service';

describe('ProductService', () => {
  let service: ProductService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        ProductService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(ProductService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('sends server-side list query parameters for paged products', () => {
    service.getProductsPage({
      page: 3,
      pageSize: 50,
      search: 'SKU-001',
      unit: UnitOfMeasure.Piece,
      requiresBatch: true,
      isActive: false,
      sortBy: 'name',
      sortDirection: 'desc'
    }).subscribe();

    const req = httpMock.expectOne(request =>
      request.method === 'GET' && request.url === `${environment.apiUrl}/products/paged`
    );

    expect(req.request.params.get('page')).toBe('3');
    expect(req.request.params.get('pageSize')).toBe('50');
    expect(req.request.params.get('search')).toBe('SKU-001');
    expect(req.request.params.get('unit')).toBe(UnitOfMeasure.Piece);
    expect(req.request.params.get('requiresBatch')).toBe('true');
    expect(req.request.params.get('isActive')).toBe('false');
    expect(req.request.params.get('sortBy')).toBe('name');
    expect(req.request.params.get('sortDirection')).toBe('desc');

    req.flush({ items: [], page: 3, pageSize: 50, totalItems: 0, totalPages: 0 });
  });

  it('does not send empty optional filters to paged products endpoint', () => {
    service.getProductsPage({
      page: 1,
      pageSize: 10,
      sortBy: 'sku',
      sortDirection: 'asc'
    }).subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/products/paged?page=1&pageSize=10&sortBy=sku&sortDirection=asc`);

    expect(req.request.params.has('search')).toBe(false);
    expect(req.request.params.has('unit')).toBe(false);
    expect(req.request.params.has('requiresBatch')).toBe(false);
    expect(req.request.params.has('isActive')).toBe(false);

    req.flush({ items: [], page: 1, pageSize: 10, totalItems: 0, totalPages: 0 });
  });

  it('keeps the legacy full products endpoint available for small lookup scenarios', () => {
    service.getProducts().subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/products`);

    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('gets a single product by id', () => {
    service.getProduct('prod-1').subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/products/prod-1`);

    expect(req.request.method).toBe('GET');
    req.flush({ id: 'prod-1' });
  });

  it('gets product stocks by product id', () => {
    service.getProductStocks('prod-1').subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/products/prod-1/stocks`);

    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('posts and puts product payloads for create/update', () => {
    const payload: CreateProduct = {
      name: 'Steel Screw',
      sku: 'SKU-001',
      description: 'Warehouse consumable',
      unit: UnitOfMeasure.Piece,
      requiresBatch: true,
      weight: 0.1,
      volume: 0.01
    };

    service.addProduct(payload).subscribe();
    const createReq = httpMock.expectOne(`${environment.apiUrl}/products`);
    expect(createReq.request.method).toBe('POST');
    expect(createReq.request.body).toEqual(payload);
    createReq.flush({ id: 'prod-created', ...payload, isActive: true });

    service.updateProduct('prod-1', { ...payload, isActive: false }).subscribe();
    const updateReq = httpMock.expectOne(`${environment.apiUrl}/products/prod-1`);
    expect(updateReq.request.method).toBe('PUT');
    expect(updateReq.request.body).toEqual({ ...payload, isActive: false, id: 'prod-1' });
    updateReq.flush({ id: 'prod-1', ...payload, isActive: false });
  });
});
