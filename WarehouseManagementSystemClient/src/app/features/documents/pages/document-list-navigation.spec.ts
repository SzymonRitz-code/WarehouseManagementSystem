import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { provideEffects } from '@ngrx/effects';
import { provideStore, Store } from '@ngrx/store';
import { of, Subject } from 'rxjs';
import { DocumentStatus } from '../../../core/enums/documentStatus';
import { DocumentType } from '../../../core/enums/documentType';
import { DocumentListEffects } from '../../../store/documents/document-list.effects';
import { documentListReducer } from '../../../store/documents/document-list.reducer';
import { DOCUMENT_LIST_FEATURE_KEY, selectDocumentListState } from '../../../store/documents/document-list.selectors';
import { WarehouseService } from '../../warehouses/services/warehouse-service';
import { DocumentList } from '../model/document';
import { DocumentService, PagedResult } from '../services/document-service';
import { DocumentListComponent } from './document-list/document-list.component';
import { DocumentPendingListComponent } from './document-pending-list/document-pending-list.component';

describe('Documents list navigation', () => {
  it.each([
    { from: 'all', to: 'pending' },
    { from: 'pending', to: 'all' },
  ])('uses the new query and cancels the old request from $from to $to', async ({ from, to }) => {
    const priorRow: DocumentList = {
      id: 'prior',
      documentNumber: 'DOC/PRIOR',
      type: DocumentType.PZ,
      status: DocumentStatus.Draft,
      sourceWarehouse: 'MAIN',
      createdAt: new Date('2026-06-01T10:00:00Z'),
      createdBy: 'operator',
      itemCount: 1,
      totalQuantity: 1,
    };
    const nextRow = { ...priorRow, id: 'next', documentNumber: 'DOC/NEXT' };
    const firstResponse = { items: [priorRow], page: 1, pageSize: 10, totalItems: 1, totalPages: 1 };
    const oldRequest = new Subject<PagedResult<DocumentList>>();
    const newRequest = new Subject<PagedResult<DocumentList>>();
    const sourceEndpoint = vi.fn().mockReturnValue(of(firstResponse));
    const targetEndpoint = vi.fn().mockReturnValue(newRequest);

    TestBed.configureTestingModule({
      providers: [
        provideRouter([
          { path: 'documents/pending', component: DocumentPendingListComponent },
          { path: 'documents', component: DocumentListComponent },
        ]),
        provideStore({ [DOCUMENT_LIST_FEATURE_KEY]: documentListReducer }),
        provideEffects(DocumentListEffects),
        { provide: WarehouseService, useValue: { getWarehouses: () => of([]) } },
        {
          provide: DocumentService,
          useValue: {
            getDocuments: from === 'all' ? sourceEndpoint : targetEndpoint,
            getPendingDocuments: from === 'pending' ? sourceEndpoint : targetEndpoint,
          },
        },
      ],
    });

    const harness = await RouterTestingHarness.create();
    const state = TestBed.inject(Store).selectSignal(selectDocumentListState);
    const source = from === 'all'
      ? await harness.navigateByUrl('/documents', DocumentListComponent)
      : await harness.navigateByUrl('/documents/pending', DocumentPendingListComponent);
    expect(harness.routeNativeElement?.textContent).toContain(priorRow.documentNumber);

    sourceEndpoint.mockReturnValue(oldRequest);
    source.onPageSizeChange(25);
    source.onSortChange({ key: 'documentNumber', direction: 'asc' });
    source.onPageChange(2);
    expect(oldRequest.observed).toBe(true);

    await harness.navigateByUrl(to === 'all' ? '/documents' : '/documents/pending');
    expect(targetEndpoint).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({
      page: 1,
      pageSize: 10,
      sortBy: 'createdAt',
      sortDirection: 'desc',
    }));
    expect(oldRequest.observed).toBe(false);
    expect(state()).toMatchObject({ mode: to, items: [], totalItems: 0, isLoading: true });
    expect(harness.routeNativeElement?.textContent).not.toContain(priorRow.documentNumber);

    newRequest.next({ ...firstResponse, items: [nextRow] });
    newRequest.complete();
    oldRequest.next({ ...firstResponse, page: 2, pageSize: 25, totalItems: 30, totalPages: 2 });
    oldRequest.complete();
    harness.detectChanges();

    expect(state()).toMatchObject({
      mode: to,
      items: [nextRow],
      totalItems: 1,
      query: { page: 1, pageSize: 10 },
      isLoading: false,
    });
    expect(harness.routeNativeElement?.textContent).toContain(nextRow.documentNumber);
  });
});
