import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { Warehouse } from '../../model/warehouse';
import { WarehouseService } from '../../services/warehouse-service';
import { WarehouseDetailComponent } from './warehouse-detail.component';

describe('WarehouseDetailComponent', () => {
  let component: WarehouseDetailComponent;
  let fixture: ComponentFixture<WarehouseDetailComponent>;
  let warehouseService: { getWarehouse: ReturnType<typeof vi.fn> };
  let router: { navigateByUrl: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    warehouseService = {
      getWarehouse: vi.fn().mockReturnValue(of(warehouseFixture()))
    };
    router = { navigateByUrl: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [WarehouseDetailComponent],
      providers: [
        { provide: WarehouseService, useValue: warehouseService },
        { provide: Router, useValue: router },
        {
          provide: ActivatedRoute,
          useValue: {
            paramMap: of(convertToParamMap({ id: 'wh-1' }))
          }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(WarehouseDetailComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('loads warehouse details from route id', async () => {
    const warehouse = await firstWarehouseEmission();

    expect(warehouseService.getWarehouse).toHaveBeenCalledWith('wh-1');
    expect(component.id).toBe('wh-1');
    expect(warehouse).toEqual(warehouseFixture());
  });

  it('returns undefined for the detail stream when API fails', async () => {
    warehouseService.getWarehouse.mockReturnValue(throwError(() => new Error('not found')));

    fixture = TestBed.createComponent(WarehouseDetailComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    const warehouse = await firstWarehouseEmission();

    expect(warehouse).toBeUndefined();
  });

  it('navigates back to list and to edit form', () => {
    component.id = 'wh-1';

    component.onBack();
    component.onEdit();

    expect(router.navigateByUrl).toHaveBeenCalledWith('/warehouses');
    expect(router.navigateByUrl).toHaveBeenCalledWith('/warehouses/form/wh-1');
  });

  function firstWarehouseEmission(): Promise<Warehouse | undefined> {
    return new Promise(resolve => {
      component.warehouse$.subscribe(warehouse => resolve(warehouse));
    });
  }

  function warehouseFixture(): Warehouse {
    return {
      id: 'wh-1',
      code: 'MAIN',
      name: 'Main Warehouse',
      country: 'Poland',
      city: 'Warsaw',
      address: 'Main Street 1',
      isActive: true
    };
  }
});
