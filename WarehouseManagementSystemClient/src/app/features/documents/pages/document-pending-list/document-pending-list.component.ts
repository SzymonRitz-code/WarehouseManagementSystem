import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { catchError, finalize, map, Observable, of, shareReplay, startWith, Subject, switchMap, take } from 'rxjs';
import { DocumentType } from '../../../../core/enums/documentType';
import { DocumentList } from '../../model/document';
import { DocumentListQuery, DocumentService } from '../../services/document-service';
import { ComponentCardComponent } from '../../../../shared/components/common/component-card/component-card.component';
import { ModalComponent } from '../../../../shared/components/common/modal/modal.component';
import { PageBreadcrumbComponent } from '../../../../shared/components/common/page-breadcrumb/page-breadcrumb.component';
import { TableComponent } from '../../../../shared/components/table/table.component';
import { WarehouseList } from '../../../warehouses/model/warehouse';
import { WarehouseService } from '../../../warehouses/services/warehouse-service';

@Component({
  selector: 'app-document-pending-list',
  standalone: true,
  imports: [CommonModule, FormsModule, PageBreadcrumbComponent, ComponentCardComponent, TableComponent, ModalComponent],
  templateUrl: './document-pending-list.component.html'
})
export class DocumentPendingListComponent implements OnInit {
  documents$!: Observable<DocumentList[]>;
  warehouses$!: Observable<WarehouseList[]>;
  isLoading = false;
  errorMessage = '';
  page = 1;
  pageSize = 10;
  totalItems = 0;
  sortBy = 'createdAt';
  sortDirection: 'asc' | 'desc' = 'desc';
  filters = {
    search: '',
    type: '',
    warehouseId: '',
    createdFrom: '',
    createdTo: ''
  };
  selectedDocument: DocumentList | null = null;
  actionMode: 'confirm' | 'cancel' | null = null;
  isActionPending = false;
  actionError: string | null = null;
  readonly documentTypes = Object.values(DocumentType);
  private readonly reloadDocuments$ = new Subject<void>();

  columns = [
    { key: 'documentNumber', label: 'Document Number', sortable: true },
    { key: 'type', label: 'Type', sortable: true },
    { key: 'status', label: 'Status', sortable: true },
    { key: 'sourceWarehouse', label: 'From Warehouse', sortable: false },
    { key: 'destinationWarehouse', label: 'To Warehouse', sortable: false },
    { key: 'createdBy', label: 'Created By', sortable: true },
    { key: 'approvedBy', label: 'Approved By', sortable: true },
    { key: 'createdAt', label: 'Created At', sortable: true, type: 'date' },
    { key: 'approvedAt', label: 'Approved At', sortable: true, type: 'date' },
    { key: 'itemCount', label: 'Items', sortable: false },
    { key: 'totalQuantity', label: 'Total Qty', sortable: false }
  ];

  documentActions = [
    { label: 'Edit', action: 'edit', visible: (row: DocumentList) => row.status === 'Draft' },
    { label: 'Confirm', action: 'confirm', visible: (row: DocumentList) => row.status === 'Draft' },
    {
      label: 'Cancel',
      action: 'cancel',
      visible: (row: DocumentList) => row.status === 'Draft' || (row.status === 'Confirmed' && row.type === DocumentType.MM)
    }
  ];

  constructor(
    private router: Router,
    private documentService: DocumentService,
    private warehouseService: WarehouseService
  ) {}

  ngOnInit(): void {
    // RxJS insight: warehouse options are read-only lookup data for this view. shareReplay(1)
    // caches the latest successful emission for all async-pipe subscribers and avoids repeated
    // HTTP calls caused by template re-rendering or conditional DOM changes.
    this.warehouses$ = this.warehouseService.getWarehouses().pipe(
      catchError(() => of([])),
      shareReplay({ bufferSize: 1, refCount: true })
    );

    // RxJS insight: every table event only emits "reload". Compared to manually assigning data
    // in every handler, one stream centralizes loading/error/request behavior. switchMap also
    // cancels the previous HTTP request when a newer table query is requested.
    this.documents$ = this.reloadDocuments$.pipe(
      startWith(void 0),
      switchMap(() => {
        // Put loading inside switchMap. On a new reload, switchMap disposes the old request first;
        // then this block marks the new request as loading, so the old finalize cannot hide it.
        this.isLoading = true;
        this.errorMessage = '';
        return this.documentService.getPendingDocuments(this.buildQuery()).pipe(
          catchError(() => {
            this.errorMessage = 'Pending documents could not be loaded. Please try again.';
            this.totalItems = 0;
            return of({ items: [], page: this.page, pageSize: this.pageSize, totalItems: 0, totalPages: 0 });
          }),
          finalize(() => this.isLoading = false)
        );
      }),
      map(result => this.setPageResult(result)),
      shareReplay({ bufferSize: 1, refCount: true })
    );
  }

  retry(): void {
    this.loadPendingDocuments();
  }

  applyFilters(): void {
    this.page = 1;
    this.loadPendingDocuments();
  }

  resetFilters(): void {
    this.filters = {
      search: '',
      type: '',
      warehouseId: '',
      createdFrom: '',
      createdTo: ''
    };
    this.page = 1;
    this.sortBy = 'createdAt';
    this.sortDirection = 'desc';
    this.loadPendingDocuments();
  }

  onPageChange(page: number): void {
    this.page = page;
    this.loadPendingDocuments();
  }

  onPageSizeChange(pageSize: number): void {
    this.pageSize = pageSize;
    this.page = 1;
    this.loadPendingDocuments();
  }

  onSortChange(sort: { key: string; direction: 'asc' | 'desc' }): void {
    this.sortBy = sort.key;
    this.sortDirection = sort.direction;
    this.page = 1;
    this.loadPendingDocuments();
  }

  private loadPendingDocuments(): void {
    // Handlers do not fetch data directly. They only signal that the current query should be
    // executed again, and the observable pipeline decides how to do that safely.
    this.reloadDocuments$.next();
  }

  private buildQuery(): DocumentListQuery {
    // Pending documents use the same server-side table contract as the main list: page, filters
    // and sort are sent to the API so the browser never has to load the whole document table.
    return {
      page: this.page,
      pageSize: this.pageSize,
      search: this.emptyToUndefined(this.filters.search),
      type: this.emptyToUndefined(this.filters.type),
      warehouseId: this.emptyToUndefined(this.filters.warehouseId),
      createdFrom: this.emptyToUndefined(this.filters.createdFrom),
      createdTo: this.emptyToUndefined(this.filters.createdTo),
      sortBy: this.sortBy,
      sortDirection: this.sortDirection
    };
  }

  private emptyToUndefined(value: string): string | undefined {
    const trimmed = value.trim();
    return trimmed.length > 0 ? trimmed : undefined;
  }

  private setPageResult(result: { items: DocumentList[]; page: number; pageSize: number; totalItems: number }): DocumentList[] {
    // Keep the paged response normalized: items go to the table stream, metadata updates the
    // pagination controls.
    this.page = result.page;
    this.pageSize = result.pageSize;
    this.totalItems = result.totalItems;

    return result.items;
  }

  goToForm() {
    this.router.navigateByUrl('/documents/form');
  }

  onDocumentAction(event: { row: DocumentList; action: string }) {
    const { row, action } = event;

    switch (action) {
      case 'edit':
        this.onEdit(row);
        break;
      case 'confirm':
        this.openActionModal(row, 'confirm');
        break;
      case 'cancel':
        this.openActionModal(row, 'cancel');
        break;
    }
  }

  openActionModal(row: DocumentList, action: 'confirm' | 'cancel') {
    this.selectedDocument = row;
    this.actionMode = action;
    this.actionError = null;
  }

  closeActionModal() {
    if (this.isActionPending) return;
    this.selectedDocument = null;
    this.actionMode = null;
    this.actionError = null;
  }

  confirmAction() {
    if (!this.selectedDocument || !this.actionMode) return;

    this.isActionPending = true;
    this.actionError = null;

    const request$ = this.actionMode === 'confirm'
      ? this.documentService.confirmDocument(this.selectedDocument)
      : this.documentService.cancelDocument(this.selectedDocument);

    // RxJS insight: confirm/cancel is a command stream, not a view stream. We subscribe here
    // because the operation has side effects. take(1) makes it a one-response command, while
    // finalize guarantees the modal button is unlocked on both success and error.
    request$.pipe(
      take(1),
      finalize(() => this.isActionPending = false)
    ).subscribe({
      next: () => {
        this.loadPendingDocuments();
        this.selectedDocument = null;
        this.actionMode = null;
        this.actionError = null;
      },
      error: (err) => {
        this.actionError = this.resolveServerError(err);
      }
    });
  }

  onDetails(row: DocumentList) {
    this.router.navigateByUrl(`/documents/detail/${row.id}`);
  }

  onEdit(document: DocumentList) {
    this.router.navigateByUrl(`/documents/form/${document.id}`);
  }

  private resolveServerError(err: any): string {
    return err?.error?.detail
      || err?.error?.title
      || err?.message
      || 'Operation failed. Please try again.';
  }
}
