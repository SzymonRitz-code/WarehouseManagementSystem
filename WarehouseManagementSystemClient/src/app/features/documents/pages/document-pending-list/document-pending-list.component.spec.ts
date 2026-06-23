import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { provideRouter } from '@angular/router';
import { of, Subject, throwError } from 'rxjs';
import { DocumentStatus } from '../../../../core/enums/documentStatus';
import { DocumentType } from '../../../../core/enums/documentType';
import { WarehouseService } from '../../../warehouses/services/warehouse-service';
import { DocumentList } from '../../model/document';
import { DocumentService, PagedResult } from '../../services/document-service';
import { DocumentPendingListComponent } from './document-pending-list.component';

describe('DocumentPendingListComponent', () => {
  let component: DocumentPendingListComponent;
  let fixture: ComponentFixture<DocumentPendingListComponent>;
  let documentService: {
    getPendingDocuments: ReturnType<typeof vi.fn>;
    confirmDocument: ReturnType<typeof vi.fn>;
    cancelDocument: ReturnType<typeof vi.fn>;
  };
  let router: { navigateByUrl: ReturnType<typeof vi.fn> };

  const pendingDocument: DocumentList = {
    id: 'doc-pending-1',
    documentNumber: 'PENDING/001',
    type: DocumentType.MM,
    status: DocumentStatus.Draft,
    sourceWarehouse: 'MAIN',
    destinationWarehouse: 'DOCK',
    createdBy: 'operator',
    createdAt: new Date('2026-06-10T10:00:00Z'),
    itemCount: 3,
    totalQuantity: 30
  };

  beforeEach(async () => {
    documentService = {
      getPendingDocuments: vi.fn().mockReturnValue(of(pageResult([pendingDocument], 1, 10, 1))),
      confirmDocument: vi.fn(),
      cancelDocument: vi.fn()
    };
    router = { navigateByUrl: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [DocumentPendingListComponent],
      providers: [
        { provide: DocumentService, useValue: documentService },
        { provide: WarehouseService, useValue: { getWarehouses: vi.fn().mockReturnValue(of([])) } },
        provideRouter([])
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(DocumentPendingListComponent);
    component = fixture.componentInstance;
    vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockImplementation((url: any) => {
      (router.navigateByUrl as any)(url);
      return Promise.resolve(true);
    });
    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('loads pending documents with default workflow query', async () => {
    await firstDocumentsEmission();

    expect(documentService.getPendingDocuments).toHaveBeenCalledWith({
      page: 1,
      pageSize: 10,
      search: undefined,
      type: undefined,
      warehouseId: undefined,
      createdFrom: undefined,
      createdTo: undefined,
      sortBy: 'createdAt',
      sortDirection: 'desc'
    });
    expect(component.totalItems()).toBe(1);
  });

  it('opens confirm modal from row action and resets previous action error', () => {
    component.actionError.set('Previous error');

    component.onDocumentAction({ row: pendingDocument, action: 'confirm' });

    expect(component.selectedDocument()).toBe(pendingDocument);
    expect(component.actionMode()).toBe('confirm');
    expect(component.actionError()).toBeNull();
  });

  it('does not close action modal while command is pending', () => {
    component.openActionModal(pendingDocument, 'cancel');
    component.isActionPending.set(true);

    component.closeActionModal();

    expect(component.selectedDocument()).toBe(pendingDocument);
    expect(component.actionMode()).toBe('cancel');
  });

  it('confirms document, closes modal and refreshes pending list', async () => {
    await firstDocumentsEmission();
    component.openActionModal(pendingDocument, 'confirm');
    documentService.confirmDocument.mockReturnValue(of({ id: pendingDocument.id }));

    component.confirmAction();

    expect(documentService.confirmDocument).toHaveBeenCalledWith(pendingDocument);
    expect(component.selectedDocument()).toBeNull();
    expect(component.actionMode()).toBeNull();
    expect(component.actionError()).toBeNull();
    expect(component.isActionPending()).toBe(false);
    expect(documentService.getPendingDocuments).toHaveBeenCalledTimes(2);
  });

  it('cancels document when cancel mode is selected', () => {
    component.openActionModal(pendingDocument, 'cancel');
    documentService.cancelDocument.mockReturnValue(of({ id: pendingDocument.id }));

    component.confirmAction();

    expect(documentService.cancelDocument).toHaveBeenCalledWith(pendingDocument);
  });

  it('keeps modal open and shows business error when confirm fails', () => {
    component.openActionModal(pendingDocument, 'confirm');
    documentService.confirmDocument.mockReturnValue(throwError(() => ({
      error: { detail: 'Document has already been processed.' }
    })));

    component.confirmAction();

    expect(component.selectedDocument()).toBe(pendingDocument);
    expect(component.actionError()).toBe('Document has already been processed.');
    expect(component.isActionPending()).toBe(false);
  });

  it('sets loading while action request is pending and unlocks after completion', () => {
    const request$ = new Subject<any>();
    component.openActionModal(pendingDocument, 'confirm');
    documentService.confirmDocument.mockReturnValue(request$);

    component.confirmAction();
    expect(component.isActionPending()).toBe(true);

    request$.next({ id: pendingDocument.id });
    request$.complete();

    expect(component.isActionPending()).toBe(false);
  });

  it('sends filters, page size and sort changes to pending endpoint', async () => {
    await firstDocumentsEmission();

    component.filters.search = 'PENDING';
    component.filters.type = DocumentType.MM;
    component.filters.warehouseId = 'wh-2';
    component.applyFilters();
    await firstDocumentsEmission();

    expect(lastPendingQuery()).toEqual(expect.objectContaining({
      page: 1,
      search: 'PENDING',
      type: DocumentType.MM,
      warehouseId: 'wh-2'
    }));

    component.onPageSizeChange(25);
    await firstDocumentsEmission();
    expect(lastPendingQuery()).toEqual(expect.objectContaining({ page: 1, pageSize: 25 }));

    component.onSortChange({ key: 'documentNumber', direction: 'asc' });
    await firstDocumentsEmission();
    expect(lastPendingQuery()).toEqual(expect.objectContaining({
      sortBy: 'documentNumber',
      sortDirection: 'asc'
    }));
  });

  function firstDocumentsEmission(): Promise<DocumentList[]> {
    return new Promise(resolve => {
      component.documents$.subscribe(rows => resolve(rows));
    });
  }

  function lastPendingQuery() {
    const calls = documentService.getPendingDocuments.mock.calls;
    return calls[calls.length - 1][0];
  }

  function pageResult(items: DocumentList[], page: number, pageSize: number, totalItems: number): PagedResult<DocumentList> {
    return {
      items,
      page,
      pageSize,
      totalItems,
      totalPages: Math.ceil(totalItems / pageSize)
    };
  }
});
