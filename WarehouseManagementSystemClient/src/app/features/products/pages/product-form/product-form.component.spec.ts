import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { UnitOfMeasure } from '../../../../core/enums/unitOfMeasure';
import { Product } from '../../model/product';
import { ProductService } from '../../services/product-service';
import { ProductFormComponent } from './product-form.component';
import { ProductBatchService } from '../../services/product-batch-service';

describe('ProductFormComponent', () => {
  let component: ProductFormComponent;
  let fixture: ComponentFixture<ProductFormComponent>;
  let productService: {
    getProduct: ReturnType<typeof vi.fn>;
    addProduct: ReturnType<typeof vi.fn>;
    updateProduct: ReturnType<typeof vi.fn>;
  };
  let router: { navigateByUrl: ReturnType<typeof vi.fn> };
  let productBatchService: {
    getBatches: ReturnType<typeof vi.fn>;
    createBatch: ReturnType<typeof vi.fn>;
    updateBatch: ReturnType<typeof vi.fn>;
  };

  it('builds a form with required business fields and unit options', async () => {
    await setup();

    component.productForm.patchValue({
      name: '',
      sku: '',
      unit: '',
      weight: -1,
      volume: -1
    });

    expect(component.productForm.valid).toBe(false);
    expect(component.productForm.get('name')?.hasError('required')).toBe(true);
    expect(component.productForm.get('sku')?.hasError('required')).toBe(true);
    expect(component.productForm.get('unit')?.hasError('required')).toBe(true);
    expect(component.productForm.get('weight')?.hasError('min')).toBe(true);
    expect(component.productForm.get('volume')?.hasError('min')).toBe(true);
    expect(component.unitOptions).toContainEqual({ value: UnitOfMeasure.Piece, label: UnitOfMeasure.Piece });
  });

  it('does not call API when create form is invalid', async () => {
    await setup();

    component.productForm.patchValue({ name: '', sku: '', unit: '' });
    component.onSave();

    expect(productService.addProduct).not.toHaveBeenCalled();
    expect(productService.updateProduct).not.toHaveBeenCalled();
    expect(router.navigateByUrl).not.toHaveBeenCalled();
  });

  it('maps create form value to API payload and navigates to created product detail', async () => {
    await setup();
    productService.addProduct.mockReturnValue(of({ id: 'prod-created', ...validProductPayload(), isActive: true }));

    fillValidForm();
    component.onSave();

    expect(productService.addProduct).toHaveBeenCalledWith({
      name: 'Steel Screw',
      sku: 'SKU-001',
      description: 'Warehouse consumable',
      unit: UnitOfMeasure.Piece,
      requiresBatch: true,
      weight: 0.1,
      volume: 0.01
    });
    expect(router.navigateByUrl).toHaveBeenCalledWith('/products/detail/prod-created');
  });

  it('lets a user fill and submit the create form from the rendered UI', async () => {
    await setup();
    productService.addProduct.mockReturnValue(of({ id: 'prod-created', ...validProductPayload(), isActive: true }));

    setInputValue('app-input-field[formcontrolname="name"] input', 'Steel Screw');
    setInputValue('app-input-field[formcontrolname="sku"] input', 'SKU-001');
    setSelectValue('app-input-select[formcontrolname="unit"] select', UnitOfMeasure.Piece);
    setInputValue('app-input-field[formcontrolname="weight"] input', '0.1');
    setInputValue('app-input-field[formcontrolname="volume"] input', '0.01');
    setTextAreaValue('app-text-area[formcontrolname="description"] textarea', 'Warehouse consumable');
    fixture.detectChanges();

    clickButton('Save');
    await fixture.whenStable();

    expect(productService.addProduct).toHaveBeenCalledWith({
      name: 'Steel Screw',
      sku: 'SKU-001',
      description: 'Warehouse consumable',
      unit: UnitOfMeasure.Piece,
      requiresBatch: true,
      weight: '0.1',
      volume: '0.01'
    });
    expect(router.navigateByUrl).toHaveBeenCalledWith('/products/detail/prod-created');
  });

  it('keeps save disabled in the rendered UI until required fields are filled', async () => {
    await setup();

    setInputValue('app-input-field[formcontrolname="name"] input', '');
    setInputValue('app-input-field[formcontrolname="sku"] input', '');
    setSelectValue('app-input-select[formcontrolname="unit"] select', '');
    fixture.detectChanges();

    const saveButton = buttonByText('Save');
    expect(saveButton.disabled).toBe(true);

    saveButton.click();

    expect(productService.addProduct).not.toHaveBeenCalled();
  });

  it('loads existing product into edit form', async () => {
    const existingProduct = productFixture();
    await setup('prod-1', {
      productServiceOverrides: {
        getProduct: vi.fn().mockReturnValue(of(existingProduct))
      }
    });

    expect(productService.getProduct).toHaveBeenCalledWith('prod-1');
    expect(component.productForm.value).toEqual(expect.objectContaining({
      id: 'prod-1',
      name: existingProduct.name,
      sku: existingProduct.sku,
      description: existingProduct.description,
      unit: existingProduct.unit,
      requiresBatch: existingProduct.requiresBatch,
      isActive: existingProduct.isActive,
      weight: existingProduct.weight,
      volume: existingProduct.volume
    }));
  });

  it('uses update endpoint for edit mode and keeps isActive in the payload', async () => {
    await setup('prod-1', {
      productServiceOverrides: {
        getProduct: vi.fn().mockReturnValue(of(productFixture()))
      }
    });
    productService.updateProduct.mockReturnValue(of(productFixture()));

    component.productForm.patchValue({
      name: 'Updated Screw',
      isActive: false
    });
    component.onSave();

    expect(productService.updateProduct).toHaveBeenCalledWith('prod-1', {
      name: 'Updated Screw',
      sku: 'SKU-001',
      description: 'Warehouse consumable',
      unit: UnitOfMeasure.Piece,
      requiresBatch: true,
      isActive: false,
      weight: 0.1,
      volume: 0.01
    });
    expect(router.navigateByUrl).toHaveBeenCalledWith('/products/detail/prod-1');
  });

  it('creates an added batch after saving the product', async () => {
    await setup();
    productService.addProduct.mockReturnValue(of({ id: 'prod-created', ...validProductPayload(), isActive: true }));
    fillValidForm();
    component.addBatch();
    component.batches.at(0).patchValue({
      batchNumber: 'BATCH-001',
      manufacturedDate: '2026-01-10',
      expirationDate: '2027-01-10'
    });

    component.onSave();

    expect(productBatchService.createBatch).toHaveBeenCalledWith('prod-created', {
      batchNumber: 'BATCH-001',
      productId: 'prod-created',
      manufacturedDate: new Date('2026-01-10'),
      expirationDate: new Date('2027-01-10')
    });
    expect(router.navigateByUrl).toHaveBeenCalledWith('/products/detail/prod-created');
  });

  it('maps backend validation errors onto product form controls', async () => {
    await setup();
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => undefined);
    productService.addProduct.mockReturnValue(throwError(() => ({
      error: {
        errors: {
          sku: ['SKU already exists.']
        }
      }
    })));

    fillValidForm();
    component.onSave();

    expect(component.productForm.get('sku')?.errors?.['server']).toBe('SKU already exists.');
    expect(router.navigateByUrl).not.toHaveBeenCalled();

    consoleError.mockRestore();
  });

  it('navigates back to product list', async () => {
    await setup();

    component.onBack();

    expect(router.navigateByUrl).toHaveBeenCalledWith('/products');
  });

  async function setup(
    productId: string | null = null,
    options?: {
      productServiceOverrides?: Partial<typeof productService>;
    }
  ): Promise<void> {
    productService = {
      getProduct: vi.fn(),
      addProduct: vi.fn(),
      updateProduct: vi.fn(),
      ...(options?.productServiceOverrides ?? {})
    };
    router = { navigateByUrl: vi.fn() };
    productBatchService = {
      getBatches: vi.fn().mockReturnValue(of([])),
      createBatch: vi.fn().mockReturnValue(of({ id: 'batch-created' })),
      updateBatch: vi.fn().mockReturnValue(of({ id: 'batch-updated' }))
    };

    await TestBed.configureTestingModule({
      imports: [ProductFormComponent],
      providers: [
        { provide: ProductService, useValue: productService },
        { provide: ProductBatchService, useValue: productBatchService },
        { provide: Router, useValue: router },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: {
                get: vi.fn().mockReturnValue(productId)
              }
            }
          }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ProductFormComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
  }

  function fillValidForm(): void {
    component.productForm.patchValue(validProductPayload());
  }

  function validProductPayload() {
    return {
      name: 'Steel Screw',
      sku: 'SKU-001',
      description: 'Warehouse consumable',
      unit: UnitOfMeasure.Piece,
      requiresBatch: true,
      isActive: true,
      weight: 0.1,
      volume: 0.01
    };
  }

  function productFixture(): Product {
    return {
      id: 'prod-1',
      ...validProductPayload()
    };
  }

  function setInputValue(selector: string, value: string): void {
    const input = fixture.nativeElement.querySelector(selector) as HTMLInputElement;
    input.value = value;
    input.dispatchEvent(new Event('input', { bubbles: true }));
  }

  function setTextAreaValue(selector: string, value: string): void {
    const textarea = fixture.nativeElement.querySelector(selector) as HTMLTextAreaElement;
    textarea.value = value;
    textarea.dispatchEvent(new Event('input', { bubbles: true }));
  }

  function setSelectValue(selector: string, value: string): void {
    const select = fixture.nativeElement.querySelector(selector) as HTMLSelectElement;
    select.value = value;
    select.dispatchEvent(new Event('change', { bubbles: true }));
  }

  function clickButton(text: string): void {
    buttonByText(text).click();
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
