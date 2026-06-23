import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { of, Subject, throwError } from 'rxjs';
import { TemperatureType } from '../../../../core/enums/temperatureType';
import { ZoneList } from '../../model/zone';
import { ZoneService } from '../../services/zone-service';
import { ZoneListComponent } from './zone-list.component';

describe('ZoneListComponent', () => {
  let component: ZoneListComponent;
  let fixture: ComponentFixture<ZoneListComponent>;
  let zoneService: {
    getZones: ReturnType<typeof vi.fn>;
  };
  let router: {
    navigate: ReturnType<typeof vi.fn>;
    navigateByUrl: ReturnType<typeof vi.fn>;
  };

  const zoneRow: ZoneList = {
    id: 'zone-1',
    code: 'A-01',
    name: 'Picking A-01',
    temperatureType: TemperatureType.Ambient,
    isPickingZone: true,
    warehouseId: 'wh-1',
    warehouseName: 'Main Warehouse',
    stockQty: 1200,
    createdAt: '2026-06-22T08:00:00Z'
  };

  beforeEach(async () => {
    zoneService = {
      getZones: vi.fn().mockReturnValue(of([zoneRow]))
    };
    router = {
      navigate: vi.fn(),
      navigateByUrl: vi.fn()
    };

    await TestBed.configureTestingModule({
      imports: [ZoneListComponent],
      providers: [
        { provide: ZoneService, useValue: zoneService },
        provideRouter([])
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ZoneListComponent);
    component = fixture.componentInstance;

    const angularRouter = TestBed.inject(Router);
    vi.spyOn(angularRouter, 'navigate').mockImplementation((commands: readonly any[]) => {
      (router.navigate as any)(commands);
      return Promise.resolve(true);
    });
    vi.spyOn(angularRouter, 'navigateByUrl').mockImplementation((url: any) => {
      (router.navigateByUrl as any)(url);
      return Promise.resolve(true);
    });

    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('loads zones on init and clears loading state', async () => {
    const zones = await firstZonesEmission();

    expect(zoneService.getZones).toHaveBeenCalledTimes(1);
    expect(zones).toEqual([zoneRow]);
    expect(component.isLoading).toBe(false);
    expect(component.errorMessage).toBe('');
  });

  it('keeps loading true while the current zones request is pending', () => {
    const pendingRequest$ = new Subject<ZoneList[]>();
    zoneService.getZones.mockReturnValue(pendingRequest$);

    fixture = TestBed.createComponent(ZoneListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    component.zones$.subscribe();

    expect(component.isLoading).toBe(true);

    pendingRequest$.next([]);
    pendingRequest$.complete();

    expect(component.isLoading).toBe(false);
  });

  it('exposes an error state and empty rows when API fails', async () => {
    await firstZonesEmission();
    zoneService.getZones.mockReturnValue(throwError(() => new Error('timeout')));

    component.retry();
    const zones = await firstZonesEmission();

    expect(zones).toEqual([]);
    expect(component.errorMessage).toBe('Zones could not be loaded. Please try again.');
    expect(component.isLoading).toBe(false);
  });

  it('reloads zones when retry is triggered', async () => {
    await firstZonesEmission();
    const refreshedZone = { ...zoneRow, id: 'zone-2', code: 'B-01', name: 'Reserve B-01' };
    zoneService.getZones.mockReturnValue(of([refreshedZone]));

    component.retry();
    const zones = await firstZonesEmission();

    expect(zoneService.getZones).toHaveBeenCalledTimes(2);
    expect(zones).toEqual([refreshedZone]);
  });

  it('navigates to create, detail and edit routes', () => {
    component.goToForm();
    component.onZoneAction({ row: zoneRow, action: 'details' });
    component.onZoneAction({ row: zoneRow, action: 'edit' });

    expect(router.navigate).toHaveBeenCalledWith(['/zones/form']);
    expect(router.navigateByUrl).toHaveBeenCalledWith('/zones/detail/zone-1');
    expect(router.navigateByUrl).toHaveBeenCalledWith('/zones/form/zone-1');
  });

  function firstZonesEmission(): Promise<ZoneList[]> {
    return new Promise(resolve => {
      component.zones$.subscribe(zones => resolve(zones));
    });
  }
});
