import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { TemperatureType } from '../../../../core/enums/temperatureType';
import { WarehouseService } from '../../../warehouses/services/warehouse-service';
import { Zone } from '../../model/zone';
import { ZoneService } from '../../services/zone-service';
import { ZoneFormComponent } from './zone-form.component';

describe('ZoneFormComponent', () => {
  let component: ZoneFormComponent;
  let fixture: ComponentFixture<ZoneFormComponent>;
  let zoneService: {
    getZone: ReturnType<typeof vi.fn>;
    addZone: ReturnType<typeof vi.fn>;
    updateZone: ReturnType<typeof vi.fn>;
  };
  let warehouseService: {
    getWarehouses: ReturnType<typeof vi.fn>;
  };
  let router: { navigateByUrl: ReturnType<typeof vi.fn> };

  it('builds a form with required zone fields and temperature options', async () => {
    await setup();

    component.zoneForm.patchValue({
      code: '',
      name: '',
      temperatureType: '',
      warehouseId: ''
    });

    expect(component.zoneForm.valid).toBe(false);
    expect(component.zoneForm.get('code')?.hasError('required')).toBe(true);
    expect(component.zoneForm.get('name')?.hasError('required')).toBe(true);
    expect(component.zoneForm.get('temperatureType')?.hasError('required')).toBe(true);
    expect(component.zoneForm.get('warehouseId')?.hasError('required')).toBe(true);
    expect(component.temperatureTypeOptions).toContainEqual({ value: TemperatureType.Ambient, label: TemperatureType.Ambient });
  });

  it('loads warehouse options for the warehouse selector', async () => {
    await setup(null, {
      warehouseServiceOverrides: {
        getWarehouses: vi.fn().mockReturnValue(of([
          { id: 'wh-1', code: 'MAIN', name: 'Main Warehouse' },
          { id: 'wh-2', code: 'DOCK', name: 'Dock Warehouse' }
        ]))
      }
    });

    expect(warehouseService.getWarehouses).toHaveBeenCalledTimes(1);
    expect(component.warehouseOptions).toEqual([
      { value: 'wh-1', label: 'Main Warehouse' },
      { value: 'wh-2', label: 'Dock Warehouse' }
    ]);
  });

  it('does not call API when create form is invalid', async () => {
    await setup();

    component.zoneForm.patchValue({ code: '', name: '' });
    component.onSave();

    expect(zoneService.addZone).not.toHaveBeenCalled();
    expect(zoneService.updateZone).not.toHaveBeenCalled();
    expect(router.navigateByUrl).not.toHaveBeenCalled();
  });

  it('maps create form value to API payload and navigates to created zone detail', async () => {
    await setup();
    zoneService.addZone.mockReturnValue(of({ id: 'zone-created', ...validZonePayload() }));

    fillValidForm();
    component.onSave();

    expect(zoneService.addZone).toHaveBeenCalledWith({
      code: 'A-01',
      name: 'Picking A-01',
      temperatureType: TemperatureType.Ambient,
      isPickingZone: true,
      warehouseId: 'wh-1'
    });
    expect(router.navigateByUrl).toHaveBeenCalledWith('/zones/detail/zone-created');
  });

  it('loads existing zone into edit form', async () => {
    const existingZone = zoneFixture();
    await setup('zone-1', {
      zoneServiceOverrides: {
        getZone: vi.fn().mockReturnValue(of(existingZone))
      }
    });

    expect(zoneService.getZone).toHaveBeenCalledWith('zone-1');
    expect(component.zoneForm.value).toEqual(expect.objectContaining({
      id: 'zone-1',
      code: existingZone.code,
      name: existingZone.name,
      temperatureType: existingZone.temperatureType,
      isPickingZone: existingZone.isPickingZone,
      warehouseId: existingZone.warehouseId
    }));
  });

  it('uses update endpoint for edit mode', async () => {
    await setup('zone-1', {
      zoneServiceOverrides: {
        getZone: vi.fn().mockReturnValue(of(zoneFixture()))
      }
    });
    zoneService.updateZone.mockReturnValue(of(zoneFixture()));

    component.zoneForm.patchValue({
      name: 'Updated Zone',
      isPickingZone: false
    });
    component.onSave();

    expect(zoneService.updateZone).toHaveBeenCalledWith('zone-1', {
      code: 'A-01',
      name: 'Updated Zone',
      temperatureType: TemperatureType.Ambient,
      isPickingZone: false,
      warehouseId: 'wh-1'
    });
    expect(router.navigateByUrl).toHaveBeenCalledWith('/zones/detail/zone-1');
  });

  it('updates warehouseId when select change handler is used', async () => {
    await setup();

    component.handleSelectChange('wh-2');

    expect(component.zoneForm.get('warehouseId')?.value).toBe('wh-2');
  });

  it('maps backend validation errors onto zone form controls', async () => {
    await setup();
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => undefined);
    zoneService.addZone.mockReturnValue(throwError(() => ({
      error: {
        errors: {
          code: ['Zone code already exists in this warehouse.']
        }
      }
    })));

    fillValidForm();
    component.onSave();

    expect(component.zoneForm.get('code')?.errors?.['server']).toBe('Zone code already exists in this warehouse.');
    expect(router.navigateByUrl).not.toHaveBeenCalled();

    consoleError.mockRestore();
  });

  it('navigates back to zone list', async () => {
    await setup();

    component.onBack();

    expect(router.navigateByUrl).toHaveBeenCalledWith('/zones');
  });

  async function setup(
    zoneId: string | null = null,
    options?: {
      zoneServiceOverrides?: Partial<typeof zoneService>;
      warehouseServiceOverrides?: Partial<typeof warehouseService>;
    }
  ): Promise<void> {
    zoneService = {
      getZone: vi.fn(),
      addZone: vi.fn(),
      updateZone: vi.fn(),
      ...(options?.zoneServiceOverrides ?? {})
    };
    warehouseService = {
      getWarehouses: vi.fn().mockReturnValue(of([])),
      ...(options?.warehouseServiceOverrides ?? {})
    };
    router = { navigateByUrl: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [ZoneFormComponent],
      providers: [
        { provide: ZoneService, useValue: zoneService },
        { provide: WarehouseService, useValue: warehouseService },
        { provide: Router, useValue: router },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: {
                get: vi.fn().mockReturnValue(zoneId)
              }
            }
          }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ZoneFormComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
  }

  function fillValidForm(): void {
    component.zoneForm.patchValue(validZonePayload());
  }

  function validZonePayload() {
    return {
      code: 'A-01',
      name: 'Picking A-01',
      temperatureType: TemperatureType.Ambient,
      isPickingZone: true,
      warehouseId: 'wh-1'
    };
  }

  function zoneFixture(): Zone {
    return {
      id: 'zone-1',
      ...validZonePayload()
    };
  }
});
