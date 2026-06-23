import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormArray, FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { DocumentStatus } from '../../../../core/enums/documentStatus';
import { DocumentType } from '../../../../core/enums/documentType';
import { ProductService } from '../../../products/services/product-service';
import { WarehouseService } from '../../../warehouses/services/warehouse-service';
import { ZoneService } from '../../../zones/services/zone-service';
import { Document } from '../../model/document';
import { DocumentService } from '../../services/document-service';
import { DocumentFormComponent } from './document-form.component';

describe('DocumentFormComponent', () => {
  let fixture: ComponentFixture<DocumentFormComponent>;
  let component: DocumentFormComponent;
  let documentService: {
    getDocument: ReturnType<typeof vi.fn>;
    addDocument: ReturnType<typeof vi.fn>;
    updateDocument: ReturnType<typeof vi.fn>;
  };
  let router: { navigateByUrl: ReturnType<typeof vi.fn> };

  it('builds a form with required document fields and at least one item', async () => {
    await setup();

    expect(component.documentForm.valid).toBe(false);
    expect(component.documentForm.get('documentDate')?.hasError('required')).toBe(true);
    expect(component.documentForm.get('type')?.hasError('required')).toBe(true);
    expect(component.documentForm.get('sourceWarehouseId')?.hasError('required')).toBe(true);
    expect(component.documentItemsFormArray.hasError('minArrayLength')).toBe(true);
  });

  it('loads warehouse options once and shares them between source and target selects', async () => {
    const warehouseService = { getWarehouses: vi.fn().mockReturnValue(of([
      { id: 'wh-1', code: 'MAIN', name: 'Main Warehouse' },
      { id: 'wh-2', code: 'DOCK', name: 'Dock Warehouse' }
    ])) };

    await setup(null, { warehouseService });

    expect(warehouseService.getWarehouses).toHaveBeenCalledTimes(1);
    expect(component.sourceOptions).toEqual([
      { value: 'wh-1', label: 'Main Warehouse' },
      { value: 'wh-2', label: 'Dock Warehouse' }
    ]);
    expect(component.targetOptions).toEqual(component.sourceOptions);
  });

  it('maps create form value to API payload and navigates to created document detail', async () => {
    await setup();
    documentService.addDocument.mockReturnValue(of({ id: 'doc-created' }));

    fillValidForm();

    component.onSave();

    expect(documentService.addDocument).toHaveBeenCalledWith({
      documentDate: '2026-06-22',
      type: DocumentType.PZ,
      notes: 'Delivery from supplier',
      sourceWarehouseId: 'wh-1',
      targetWarehouseId: 'wh-2',
      items: [
        {
          productId: 'prod-1',
          quantity: 12,
          sourceZoneId: 'zone-1',
          targetZoneId: 'zone-2',
          productBatchId: 'batch-1'
        }
      ]
    });
    expect(router.navigateByUrl).toHaveBeenCalledWith('/documents/detail/doc-created');
  });

  it('loads existing document into form including document item FormArray', async () => {
    const existingDocument = documentFixture();
    await setup('doc-1', {
      documentServiceOverrides: {
        getDocument: vi.fn().mockReturnValue(of(existingDocument))
      }
    });

    expect(documentService.getDocument).toHaveBeenCalledWith('doc-1');
    expect(component.documentForm.value).toEqual(expect.objectContaining({
      documentDate: existingDocument.documentDate,
      type: existingDocument.type,
      notes: existingDocument.notes,
      sourceWarehouseId: existingDocument.sourceWarehouseId,
      targetWarehouseId: existingDocument.targetWarehouseId
    }));
    expect(component.documentItemsFormArray.length).toBe(1);
    expect(component.documentItemsFormArray.at(0).value).toEqual(expect.objectContaining({
      id: 'item-1',
      productId: 'prod-1',
      quantity: 7,
      productBatchId: 'batch-1',
      sourceZoneId: 'zone-1',
      targetZoneId: 'zone-2'
    }));
  });

  it('uses update endpoint for edit mode and navigates to edited document detail', async () => {
    await setup('doc-1', {
      documentServiceOverrides: {
        getDocument: vi.fn().mockReturnValue(of(documentFixture()))
      }
    });
    documentService.updateDocument.mockReturnValue(of({ id: 'doc-1' }));

    component.onSave();

    expect(documentService.updateDocument).toHaveBeenCalledWith('doc-1', expect.objectContaining({
      type: DocumentType.MM,
      sourceWarehouseId: 'wh-1',
      targetWarehouseId: 'wh-2',
      items: [
        expect.objectContaining({
          productId: 'prod-1',
          quantity: 7
        })
      ]
    }));
    expect(router.navigateByUrl).toHaveBeenCalledWith('/documents/detail/doc-1');
  });

  it('maps backend validation errors onto form controls', async () => {
    await setup();
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => undefined);
    documentService.addDocument.mockReturnValue(throwError(() => ({
      error: {
        errors: {
          sourceWarehouseId: ['Source warehouse is closed.']
        }
      }
    })));

    fillValidForm();
    component.onSave();

    expect(component.documentForm.get('sourceWarehouseId')?.errors?.['server']).toBe('Source warehouse is closed.');
    expect(router.navigateByUrl).not.toHaveBeenCalled();

    consoleError.mockRestore();
  });

  async function setup(
    documentId: string | null = null,
    options?: {
      warehouseService?: any;
      documentServiceOverrides?: Partial<typeof documentService>;
    }
  ): Promise<void> {
    documentService = {
      getDocument: vi.fn(),
      addDocument: vi.fn(),
      updateDocument: vi.fn(),
      ...(options?.documentServiceOverrides ?? {})
    };
    router = { navigateByUrl: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [DocumentFormComponent],
      providers: [
        { provide: DocumentService, useValue: documentService },
        { provide: WarehouseService, useValue: options?.warehouseService ?? { getWarehouses: vi.fn().mockReturnValue(of([])) } },
        { provide: ProductService, useValue: { getProducts: vi.fn().mockReturnValue(of([])) } },
        { provide: ZoneService, useValue: { getZones: vi.fn().mockReturnValue(of([])) } },
        { provide: Router, useValue: router },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: {
                get: vi.fn().mockReturnValue(documentId)
              }
            }
          }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(DocumentFormComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
  }

  function fillValidForm(): void {
    component.documentForm.patchValue({
      documentDate: new Date('2026-06-22T00:00:00Z'),
      type: DocumentType.PZ,
      notes: 'Delivery from supplier',
      sourceWarehouseId: 'wh-1',
      targetWarehouseId: 'wh-2'
    });

    const items = component.documentForm.get('items') as FormArray;
    items.push(new FormBuilder().group({
      productId: ['prod-1', Validators.required],
      quantity: [12, Validators.required],
      productBatchId: ['batch-1'],
      sourceZoneId: ['zone-1'],
      targetZoneId: ['zone-2']
    }));
  }

  function documentFixture(): Document {
    return {
      id: 'doc-1',
      number: 'DOC/001',
      documentDate: new Date('2026-06-20T00:00:00Z'),
      type: DocumentType.MM,
      notes: 'Move stock',
      sourceWarehouseId: 'wh-1',
      targetWarehouseId: 'wh-2',
      status: DocumentStatus.Draft,
      createdAt: new Date('2026-06-20T08:00:00Z'),
      items: [
        {
          id: 'item-1',
          documentId: 'doc-1',
          productId: 'prod-1',
          productName: 'Product 1',
          quantity: 7,
          productBatchId: 'batch-1',
          sourceZoneId: 'zone-1',
          targetZoneId: 'zone-2'
        }
      ]
    };
  }
});
