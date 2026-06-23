import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { provideRouter } from '@angular/router';
import { of, Subject, throwError } from 'rxjs';
import { DocumentStatus } from '../../../../core/enums/documentStatus';
import { DocumentType } from '../../../../core/enums/documentType';
import { WarehouseService } from '../../../warehouses/services/warehouse-service';
import { DocumentList } from '../../model/document';
import { DocumentService, PagedResult } from '../../services/document-service';
import { DocumentListComponent } from './document-list.component';

describe('DocumentListComponent', () => {
  let component: DocumentListComponent;
  let fixture: ComponentFixture<DocumentListComponent>;
  let documentService: {
    getDocuments: ReturnType<typeof vi.fn>;
    cancelDocument: ReturnType<typeof vi.fn>;
    confirmDocument: ReturnType<typeof vi.fn>;
  };
  let router: { navigateByUrl: ReturnType<typeof vi.fn> };

  const documentRow: DocumentList = {
    id: 'doc-1',
    documentNumber: 'DOC/001',
    type: DocumentType.PZ,
    status: DocumentStatus.Draft,
    sourceWarehouse: 'MAIN',
    destinationWarehouse: 'BUFFER',
    createdBy: 'operator',
    createdAt: new Date('2026-06-01T10:00:00Z'),
    itemCount: 2,
    totalQuantity: 15
  };

  beforeEach(async () => {
    documentService = {
      getDocuments: vi.fn(),
      cancelDocument: vi.fn(),
      confirmDocument: vi.fn()
    };
    router = { navigateByUrl: vi.fn() };

    documentService.getDocuments.mockImplementation((query) =>
      of(pageResult([documentRow], query.page, query.pageSize, 25))
    );

    await TestBed.configureTestingModule({
      imports: [DocumentListComponent],
      providers: [
        { provide: DocumentService, useValue: documentService },
        { provide: WarehouseService, useValue: { getWarehouses: vi.fn().mockReturnValue(of([])) } },
        provideRouter([])
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(DocumentListComponent);
    component = fixture.componentInstance;
    vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockImplementation((url: any) => {
      (router.navigateByUrl as any)(url);
      return Promise.resolve(true);
    });
    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('loads first page with the default server-side query', async () => {
    await firstDocumentsEmission();

    expect(documentService.getDocuments).toHaveBeenCalledWith({
      page: 1,
      pageSize: 10,
      search: undefined,
      type: undefined,
      status: undefined,
      warehouseId: undefined,
      createdFrom: undefined,
      createdTo: undefined,
      sortBy: 'createdAt',
      sortDirection: 'desc'
    });
    expect(component.totalItems()).toBe(25);
    expect(component.isLoading()).toBe(false);
  });

  it('sends filters to backend and resets to first page', async () => {
    await firstDocumentsEmission();
    documentService.getDocuments.mockReturnValue(of(pageResult([], 1, 10, 0)));

    component.page.set(4);
    component.filters.search = ' DOC/001 ';
    component.filters.type = DocumentType.PZ;
    component.filters.status = DocumentStatus.Draft;
    component.filters.warehouseId = 'wh-1';
    component.filters.createdFrom = '2026-06-01';
    component.filters.createdTo = '2026-06-22';

    component.applyFilters();
    await firstDocumentsEmission();

    expect(component.page()).toBe(1);
    expect(lastGetDocumentsQuery()).toEqual(expect.objectContaining({
      page: 1,
      search: 'DOC/001',
      type: DocumentType.PZ,
      status: DocumentStatus.Draft,
      warehouseId: 'wh-1',
      createdFrom: '2026-06-01',
      createdTo: '2026-06-22'
    }));
  });

  it('keeps the latest query and reruns it on retry without mutating filters', async () => {
    await firstDocumentsEmission();
    component.filters.search = 'operator';
    component.applyFilters();
    await firstDocumentsEmission();

    component.retry();
    await firstDocumentsEmission();

    expect(documentService.getDocuments).toHaveBeenCalledTimes(3);
    expect(lastGetDocumentsQuery()).toEqual(expect.objectContaining({ search: 'operator' }));
  });

  it('uses server paging and sorting events as backend query changes', async () => {
    await firstDocumentsEmission();

    component.onPageSizeChange(50);
    expect(lastGetDocumentsQuery()).toEqual(expect.objectContaining({ page: 1, pageSize: 50 }));

    component.onPageChange(3);
    expect(lastGetDocumentsQuery()).toEqual(expect.objectContaining({ page: 3, pageSize: 50 }));

    component.onSortChange({ key: 'documentNumber', direction: 'asc' });
    expect(lastGetDocumentsQuery()).toEqual(expect.objectContaining({
      page: 1,
      sortBy: 'documentNumber',
      sortDirection: 'asc'
    }));
  });

  it('exposes an error state and empty rows when API fails', async () => {
    await firstDocumentsEmission();
    documentService.getDocuments.mockReturnValue(throwError(() => new Error('timeout')));

    component.retry();

    expect(component.errorMessage()).toBe('Documents could not be loaded. Please try again.');
    expect(component.totalItems()).toBe(0);
    expect(component.isLoading()).toBe(false);
  });

  it('keeps loading true while the current HTTP request is pending', () => {
    const pendingRequest$ = new Subject<PagedResult<DocumentList>>();
    documentService.getDocuments.mockReturnValue(pendingRequest$);

    fixture = TestBed.createComponent(DocumentListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    component.documents$.subscribe();

    expect(component.isLoading()).toBe(true);

    pendingRequest$.next(pageResult([], 1, 10, 0));
    pendingRequest$.complete();

    expect(component.isLoading()).toBe(false);
  });

  it('navigates to details when details row action is emitted', () => {
    component.onDocumentAction({ row: documentRow, action: 'details' });

    expect(router.navigateByUrl).toHaveBeenCalledWith('/documents/detail/doc-1');
  });

  it('refreshes the current query after confirm command succeeds', async () => {
    await firstDocumentsEmission();
    documentService.confirmDocument.mockReturnValue(of({ id: documentRow.id }));

    component.onConfirm(documentRow);

    expect(documentService.confirmDocument).toHaveBeenCalledWith(documentRow);
    expect(documentService.getDocuments).toHaveBeenCalledTimes(2);
  });

  function firstDocumentsEmission(): Promise<DocumentList[]> {
    return new Promise(resolve => {
      component.documents$.subscribe(rows => resolve(rows));
    });
  }

  function lastGetDocumentsQuery() {
    const calls = documentService.getDocuments.mock.calls;
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
