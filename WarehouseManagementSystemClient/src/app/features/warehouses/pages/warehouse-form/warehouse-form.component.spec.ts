import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { Warehouse } from '../../model/warehouse';
import { WarehouseService } from '../../services/warehouse-service';
import { WarehouseFormComponent } from './warehouse-form.component';

describe('WarehouseFormComponent', () => {
  let component: WarehouseFormComponent;
  let fixture: ComponentFixture<WarehouseFormComponent>;
  let warehouseService: {
    getWarehouse: ReturnType<typeof vi.fn>;
    addWarehouse: ReturnType<typeof vi.fn>;
    updateWarehouse: ReturnType<typeof vi.fn>;
  };
  let router: { navigateByUrl: ReturnType<typeof vi.fn> };

  it('builds a form with required warehouse fields', async () => {
    await setup();

    component.warehouseForm.patchValue({
      code: '',
      name: '',
      country: '',
      city: '',
      address: ''
    });

    expect(component.warehouseForm.valid).toBe(false);
    expect(component.warehouseForm.get('code')?.hasError('required')).toBe(true);
    expect(component.warehouseForm.get('name')?.hasError('required')).toBe(true);
    expect(component.warehouseForm.get('country')?.hasError('required')).toBe(true);
    expect(component.warehouseForm.get('city')?.hasError('required')).toBe(true);
    expect(component.warehouseForm.get('address')?.hasError('required')).toBe(true);
  });

  it('does not call API when create form is invalid', async () => {
    await setup();

    component.warehouseForm.patchValue({ code: '', name: '' });
    component.onSave();

    expect(warehouseService.addWarehouse).not.toHaveBeenCalled();
    expect(warehouseService.updateWarehouse).not.toHaveBeenCalled();
    expect(router.navigateByUrl).not.toHaveBeenCalled();
  });

  it('maps create form value to API payload and navigates to created warehouse detail', async () => {
    await setup();
    warehouseService.addWarehouse.mockReturnValue(of({ id: 'wh-created', ...validWarehousePayload(), isActive: true }));

    fillValidForm();
    component.onSave();

    expect(warehouseService.addWarehouse).toHaveBeenCalledWith({
      code: 'MAIN',
      name: 'Main Warehouse',
      country: 'Poland',
      city: 'Warsaw',
      address: 'Main Street 1'
    });
    expect(router.navigateByUrl).toHaveBeenCalledWith('/warehouses/detail/wh-created');
  });

  it('loads existing warehouse into edit form', async () => {
    const existingWarehouse = warehouseFixture();
    await setup('wh-1', {
      warehouseServiceOverrides: {
        getWarehouse: vi.fn().mockReturnValue(of(existingWarehouse))
      }
    });

    expect(warehouseService.getWarehouse).toHaveBeenCalledWith('wh-1');
    expect(component.warehouseForm.value).toEqual(expect.objectContaining({
      id: 'wh-1',
      code: existingWarehouse.code,
      name: existingWarehouse.name,
      country: existingWarehouse.country,
      city: existingWarehouse.city,
      address: existingWarehouse.address,
      isActive: existingWarehouse.isActive
    }));
  });

  it('uses update endpoint for edit mode and keeps isActive in the payload', async () => {
    await setup('wh-1', {
      warehouseServiceOverrides: {
        getWarehouse: vi.fn().mockReturnValue(of(warehouseFixture()))
      }
    });
    warehouseService.updateWarehouse.mockReturnValue(of(warehouseFixture()));

    component.warehouseForm.patchValue({
      name: 'Updated Warehouse',
      isActive: false
    });
    component.onSave();

    expect(warehouseService.updateWarehouse).toHaveBeenCalledWith('wh-1', {
      code: 'MAIN',
      name: 'Updated Warehouse',
      country: 'Poland',
      city: 'Warsaw',
      address: 'Main Street 1',
      isActive: false
    });
    expect(router.navigateByUrl).toHaveBeenCalledWith('/warehouses/detail/wh-1');
  });

  it('maps backend validation errors onto warehouse form controls', async () => {
    await setup();
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => undefined);
    warehouseService.addWarehouse.mockReturnValue(throwError(() => ({
      error: {
        errors: {
          code: ['Warehouse code already exists.']
        }
      }
    })));

    fillValidForm();
    component.onSave();

    expect(component.warehouseForm.get('code')?.errors?.['server']).toBe('Warehouse code already exists.');
    expect(router.navigateByUrl).not.toHaveBeenCalled();

    consoleError.mockRestore();
  });

  it('navigates back to warehouse list', async () => {
    await setup();

    component.onBack();

    expect(router.navigateByUrl).toHaveBeenCalledWith('/warehouses');
  });

  async function setup(
    warehouseId: string | null = null,
    options?: {
      warehouseServiceOverrides?: Partial<typeof warehouseService>;
    }
  ): Promise<void> {
    warehouseService = {
      getWarehouse: vi.fn(),
      addWarehouse: vi.fn(),
      updateWarehouse: vi.fn(),
      ...(options?.warehouseServiceOverrides ?? {})
    };
    router = { navigateByUrl: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [WarehouseFormComponent],
      providers: [
        { provide: WarehouseService, useValue: warehouseService },
        { provide: Router, useValue: router },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: {
                get: vi.fn().mockReturnValue(warehouseId)
              }
            }
          }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(WarehouseFormComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
  }

  function fillValidForm(): void {
    component.warehouseForm.patchValue(validWarehousePayload());
  }

  function validWarehousePayload() {
    return {
      code: 'MAIN',
      name: 'Main Warehouse',
      country: 'Poland',
      city: 'Warsaw',
      address: 'Main Street 1',
      isActive: true
    };
  }

  function warehouseFixture(): Warehouse {
    return {
      id: 'wh-1',
      ...validWarehousePayload()
    };
  }
});
