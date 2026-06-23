import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { of, Subject, throwError } from 'rxjs';
import { WarehouseList } from '../../model/warehouse';
import { WarehouseService } from '../../services/warehouse-service';
import { WarehouseListComponent } from './warehouse-list.component';

describe('WarehouseListComponent', () => {
  let component: WarehouseListComponent;
  let fixture: ComponentFixture<WarehouseListComponent>;
  let warehouseService: {
    getWarehouses: ReturnType<typeof vi.fn>;
  };
  let router: {
    navigate: ReturnType<typeof vi.fn>;
    navigateByUrl: ReturnType<typeof vi.fn>;
  };

  const warehouseRow: WarehouseList = {
    id: 'wh-1',
    code: 'MAIN',
    name: 'Main Warehouse',
    country: 'Poland',
    address: 'Main Street 1',
    zonesCount: 4,
    totalStock: 120,
    totalQty: 4500,
    isActive: true,
    createdAt: new Date('2026-06-22T08:00:00Z')
  };

  beforeEach(async () => {
    warehouseService = {
      getWarehouses: vi.fn().mockReturnValue(of([warehouseRow]))
    };
    router = {
      navigate: vi.fn(),
      navigateByUrl: vi.fn()
    };

    await TestBed.configureTestingModule({
      imports: [WarehouseListComponent],
      providers: [
        { provide: WarehouseService, useValue: warehouseService },
        provideRouter([])
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(WarehouseListComponent);
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

  it('loads warehouses on init and clears loading state', async () => {
    const warehouses = await firstWarehousesEmission();

    expect(warehouseService.getWarehouses).toHaveBeenCalledTimes(1);
    expect(warehouses).toEqual([warehouseRow]);
    expect(component.isLoading).toBe(false);
    expect(component.errorMessage).toBe('');
  });

  it('keeps loading true while the current warehouses request is pending', () => {
    const pendingRequest$ = new Subject<WarehouseList[]>();
    warehouseService.getWarehouses.mockReturnValue(pendingRequest$);

    fixture = TestBed.createComponent(WarehouseListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    component.warehouses$.subscribe();

    expect(component.isLoading).toBe(true);

    pendingRequest$.next([]);
    pendingRequest$.complete();

    expect(component.isLoading).toBe(false);
  });

  it('exposes an error state and empty rows when API fails', async () => {
    await firstWarehousesEmission();
    warehouseService.getWarehouses.mockReturnValue(throwError(() => new Error('timeout')));

    component.retry();
    const warehouses = await firstWarehousesEmission();

    expect(warehouses).toEqual([]);
    expect(component.errorMessage).toBe('Warehouses could not be loaded. Please try again.');
    expect(component.isLoading).toBe(false);
  });

  it('reloads warehouses when retry is triggered', async () => {
    await firstWarehousesEmission();
    const refreshedWarehouse = { ...warehouseRow, id: 'wh-2', code: 'DOCK', name: 'Dock Warehouse' };
    warehouseService.getWarehouses.mockReturnValue(of([refreshedWarehouse]));

    component.retry();
    const warehouses = await firstWarehousesEmission();

    expect(warehouseService.getWarehouses).toHaveBeenCalledTimes(2);
    expect(warehouses).toEqual([refreshedWarehouse]);
  });

  it('navigates to create, detail and edit routes', () => {
    component.goToForm();
    component.onWarehouseAction({ row: warehouseRow, action: 'details' });
    component.onWarehouseAction({ row: warehouseRow, action: 'edit' });

    expect(router.navigate).toHaveBeenCalledWith(['/warehouses/form']);
    expect(router.navigateByUrl).toHaveBeenCalledWith('/warehouses/detail/wh-1');
    expect(router.navigateByUrl).toHaveBeenCalledWith('/warehouses/form/wh-1');
  });

  function firstWarehousesEmission(): Promise<WarehouseList[]> {
    return new Promise(resolve => {
      component.warehouses$.subscribe(warehouses => resolve(warehouses));
    });
  }
});
