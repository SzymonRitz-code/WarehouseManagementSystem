import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../environments/environment';
import { CreateWarehouse } from '../model/create-warehouse';
import { WarehouseService } from './warehouse-service';

describe('WarehouseService', () => {
  let service: WarehouseService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        WarehouseService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(WarehouseService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('gets warehouses from API', () => {
    service.getWarehouses().subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/warehouses`);

    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('gets a single warehouse by id', () => {
    service.getWarehouse('wh-1').subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/warehouses/wh-1`);

    expect(req.request.method).toBe('GET');
    req.flush({ id: 'wh-1' });
  });

  it('posts and puts warehouse payloads for create/update', () => {
    const payload: CreateWarehouse = {
      code: 'MAIN',
      name: 'Main Warehouse',
      country: 'Poland',
      city: 'Warsaw',
      address: 'Main Street 1'
    };

    service.addWarehouse(payload).subscribe();
    const createReq = httpMock.expectOne(`${environment.apiUrl}/warehouses`);
    expect(createReq.request.method).toBe('POST');
    expect(createReq.request.body).toEqual(payload);
    createReq.flush({ id: 'wh-created', ...payload, isActive: true });

    service.updateWarehouse('wh-1', { ...payload, isActive: false }).subscribe();
    const updateReq = httpMock.expectOne(`${environment.apiUrl}/warehouses/wh-1`);
    expect(updateReq.request.method).toBe('PUT');
    expect(updateReq.request.body).toEqual({ ...payload, isActive: false, id: 'wh-1' });
    updateReq.flush({ id: 'wh-1', ...payload, isActive: false });
  });
});
