import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { TemperatureType } from '../../../../core/enums/temperatureType';
import { WarehouseService } from '../../../warehouses/services/warehouse-service';
import { Zone } from '../../model/zone';
import { ZoneService } from '../../services/zone-service';
import { ZoneDetailComponent } from './zone-detail.component';

describe('ZoneDetailComponent', () => {
  let component: ZoneDetailComponent;
  let fixture: ComponentFixture<ZoneDetailComponent>;
  let zoneService: { getZone: ReturnType<typeof vi.fn> };
  let warehouseService: { getWarehouse: ReturnType<typeof vi.fn> };
  let router: { navigateByUrl: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    zoneService = {
      getZone: vi.fn().mockReturnValue(of(zoneFixture()))
    };
    warehouseService = {
      getWarehouse: vi.fn().mockReturnValue(of({ id: 'wh-1', name: 'Main Warehouse' }))
    };
    router = { navigateByUrl: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [ZoneDetailComponent],
      providers: [
        { provide: ZoneService, useValue: zoneService },
        { provide: WarehouseService, useValue: warehouseService },
        { provide: Router, useValue: router },
        {
          provide: ActivatedRoute,
          useValue: {
            paramMap: of(convertToParamMap({ id: 'zone-1' }))
          }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ZoneDetailComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('loads zone details and enriches them with warehouse name', async () => {
    const zone = await firstZoneEmission();

    expect(zoneService.getZone).toHaveBeenCalledWith('zone-1');
    expect(warehouseService.getWarehouse).toHaveBeenCalledWith('wh-1');
    expect(component.id).toBe('zone-1');
    expect(zone).toEqual(expect.objectContaining({
      id: 'zone-1',
      warehouseName: 'Main Warehouse'
    }));
  });

  it('keeps zone details when warehouse lookup fails', async () => {
    warehouseService.getWarehouse.mockReturnValue(throwError(() => new Error('warehouse unavailable')));

    fixture = TestBed.createComponent(ZoneDetailComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    const zone = await firstZoneEmission();

    expect(zone).toEqual(zoneFixture());
  });

  it('returns undefined for the detail stream when zone API fails', async () => {
    zoneService.getZone.mockReturnValue(throwError(() => new Error('not found')));

    fixture = TestBed.createComponent(ZoneDetailComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    const zone = await firstZoneEmission();

    expect(zone).toBeUndefined();
  });

  it('navigates back to list and to edit form', () => {
    component.id = 'zone-1';

    component.onBack();
    component.onEdit();

    expect(router.navigateByUrl).toHaveBeenCalledWith('/zones');
    expect(router.navigateByUrl).toHaveBeenCalledWith('/zones/form/zone-1');
  });

  function firstZoneEmission(): Promise<Zone | undefined> {
    return new Promise(resolve => {
      component.zone$.subscribe(zone => resolve(zone));
    });
  }

  function zoneFixture(): Zone {
    return {
      id: 'zone-1',
      code: 'A-01',
      name: 'Picking A-01',
      temperatureType: TemperatureType.Ambient,
      isPickingZone: true,
      warehouseId: 'wh-1'
    };
  }
});
