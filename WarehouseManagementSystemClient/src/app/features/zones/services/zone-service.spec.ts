import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { TemperatureType } from '../../../core/enums/temperatureType';
import { environment } from '../../../environments/environment';
import { CreateZone } from '../model/create-zone';
import { ZoneService } from './zone-service';

describe('ZoneService', () => {
  let service: ZoneService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        ZoneService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(ZoneService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('gets zones from API', () => {
    service.getZones().subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/zones`);

    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('gets a single zone by id', () => {
    service.getZone('zone-1').subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/zones/zone-1`);

    expect(req.request.method).toBe('GET');
    req.flush({ id: 'zone-1' });
  });

  it('posts and puts zone payloads for create/update', () => {
    const payload: CreateZone = {
      code: 'A-01',
      name: 'Picking A-01',
      temperatureType: TemperatureType.Ambient,
      isPickingZone: true,
      warehouseId: 'wh-1'
    };

    service.addZone(payload).subscribe();
    const createReq = httpMock.expectOne(`${environment.apiUrl}/zones`);
    expect(createReq.request.method).toBe('POST');
    expect(createReq.request.body).toEqual(payload);
    createReq.flush({ id: 'zone-created', ...payload });

    service.updateZone('zone-1', { ...payload, isPickingZone: false }).subscribe();
    const updateReq = httpMock.expectOne(`${environment.apiUrl}/zones/zone-1`);
    expect(updateReq.request.method).toBe('PUT');
    expect(updateReq.request.body).toEqual({ ...payload, isPickingZone: false, id: 'zone-1' });
    updateReq.flush({ id: 'zone-1', ...payload, isPickingZone: false });
  });
});
