import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of } from 'rxjs';
import { ProductService } from '../../../services/product-service';
import { ProductBatchService } from '../../../services/product-batch-service';
import { Batch } from '../../../model/product-batch';

import { ProductBatchFormComponent } from './product-batch-form.component';

describe('ProductBatchFormComponent', () => {
  let component: ProductBatchFormComponent;
  let fixture: ComponentFixture<ProductBatchFormComponent>;
  let productService: { getProduct: ReturnType<typeof vi.fn> };
  let batchService: {
    getBatch: ReturnType<typeof vi.fn>;
    createBatch: ReturnType<typeof vi.fn>;
    updateBatch: ReturnType<typeof vi.fn>;
  };
  let router: { navigateByUrl: ReturnType<typeof vi.fn> };

  it('loads product option from route product id', async () => {
    await setup();

    expect(productService.getProduct).toHaveBeenCalledWith('prod-1');
    expect(component.productOptions).toEqual([
      { value: 'prod-1', label: 'Steel Screw' }
    ]);
  });

  it('lets a user fill and submit the create form from the rendered UI', async () => {
    await setup();
    batchService.createBatch.mockReturnValue(of(batchFixture({ id: 'batch-created' })));

    setInputValue('app-input-field[formcontrolname="batchNumber"] input', 'BATCH-001');
    setSelectValue('app-input-select[formcontrolname="productId"] select', 'prod-1');
    fixture.detectChanges();

    buttonByText('Save').click();
    await fixture.whenStable();

    expect(batchService.createBatch).toHaveBeenCalledWith('prod-1', {
      batchNumber: 'BATCH-001',
      productId: 'prod-1',
      expirationDate: null,
      manufacturedDate: null
    });
    expect(router.navigateByUrl).toHaveBeenCalledWith('/products/prod-1/batches/detail/batch-created');
  });

  it('keeps save disabled in the rendered UI when required fields are missing', async () => {
    await setup();

    setInputValue('app-input-field[formcontrolname="batchNumber"] input', '');
    setSelectValue('app-input-select[formcontrolname="productId"] select', '');
    fixture.detectChanges();

    const saveButton = buttonByText('Save');
    expect(saveButton.disabled).toBe(true);

    saveButton.click();

    expect(batchService.createBatch).not.toHaveBeenCalled();
  });

  it('loads existing batch and updates it from the rendered UI', async () => {
    await setup('batch-1', {
      batchServiceOverrides: {
        getBatch: vi.fn().mockReturnValue(of(batchFixture()))
      }
    });
    batchService.updateBatch.mockReturnValue(of(batchFixture({ batchNumber: 'BATCH-002' })));

    setInputValue('app-input-field[formcontrolname="batchNumber"] input', 'BATCH-002');
    fixture.detectChanges();

    buttonByText('Save').click();
    await fixture.whenStable();

    expect(batchService.updateBatch).toHaveBeenCalledWith('prod-1', 'batch-1', {
      id: 'batch-1',
      batchNumber: 'BATCH-002',
      expirationDate: null,
      manufacturedDate: null
    });
    expect(router.navigateByUrl).toHaveBeenCalledWith('/products/prod-1/batches/detail/batch-1');
  });

  async function setup(
    batchId: string | null = null,
    options?: {
      batchServiceOverrides?: Partial<typeof batchService>;
    }
  ): Promise<void> {
    productService = {
      getProduct: vi.fn().mockReturnValue(of({
        id: 'prod-1',
        name: 'Steel Screw'
      }))
    };
    batchService = {
      getBatch: vi.fn(),
      createBatch: vi.fn(),
      updateBatch: vi.fn(),
      ...(options?.batchServiceOverrides ?? {})
    };
    router = { navigateByUrl: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [ProductBatchFormComponent],
      providers: [
        { provide: ProductService, useValue: productService },
        { provide: ProductBatchService, useValue: batchService },
        { provide: Router, useValue: router },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: {
                get: vi.fn((key: string) => {
                  if (key === 'id') return 'prod-1';
                  if (key === 'batchId') return batchId;
                  return null;
                })
              }
            }
          }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ProductBatchFormComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
  }

  function batchFixture(overrides: Partial<Batch> = {}): Batch {
    return {
      id: 'batch-1',
      batchNumber: 'BATCH-001',
      productId: 'prod-1',
      productName: 'Steel Screw',
      expirationDate: null,
      manufacturedDate: null,
      ...overrides
    };
  }

  function setInputValue(selector: string, value: string): void {
    const input = fixture.nativeElement.querySelector(selector) as HTMLInputElement;
    input.value = value;
    input.dispatchEvent(new Event('input', { bubbles: true }));
  }

  function setSelectValue(selector: string, value: string): void {
    const select = fixture.nativeElement.querySelector(selector) as HTMLSelectElement;
    select.value = value;
    select.dispatchEvent(new Event('change', { bubbles: true }));
  }

  function buttonByText(text: string): HTMLButtonElement {
    const buttons = Array.from(fixture.nativeElement.querySelectorAll('button')) as HTMLButtonElement[];
    const button = buttons.find(candidate => candidate.textContent?.trim() === text);
    if (!button) {
      throw new Error(`Button "${text}" was not found.`);
    }
    return button;
  }
});
