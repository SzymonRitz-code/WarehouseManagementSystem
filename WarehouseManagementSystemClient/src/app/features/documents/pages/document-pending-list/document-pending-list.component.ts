import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { catchError, finalize, map, Observable, of } from 'rxjs';
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
  documents$: Observable<DocumentList[]> = of([]);
  warehouses$: Observable<WarehouseList[]> = of([]);
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
    this.warehouses$ = this.warehouseService.getWarehouses().pipe(
      catchError(() => of([]))
    );
    this.loadPendingDocuments();
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
    this.isLoading = true;
    this.errorMessage = '';

    this.documents$ = this.documentService.getPendingDocuments(this.buildQuery()).pipe(
      catchError(() => {
        this.errorMessage = 'Pending documents could not be loaded. Please try again.';
        this.totalItems = 0;
        return of({ items: [], page: this.page, pageSize: this.pageSize, totalItems: 0, totalPages: 0 });
      }),
      map(result => this.setPageResult(result)),
      finalize(() => this.isLoading = false)
    );
  }

  private buildQuery(): DocumentListQuery {
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
    console.log(`Action "${action}" triggered for document:`, row);

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
    console.log(this.selectedDocument);
    
    this.isActionPending = true;
    this.actionError = null;

    const request$ = this.actionMode === 'confirm'
      ? this.documentService.confirmDocument(this.selectedDocument)
      : this.documentService.cancelDocument(this.selectedDocument);

    request$.subscribe({
      next: () => {
        this.loadPendingDocuments();
        this.closeActionModal();
      },
      error: (err) => {
        this.actionError = this.resolveServerError(err);
      }
    }).add(() => {
      this.isActionPending = false;
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
