import { CommonModule } from '@angular/common';
import { Component, OnInit, Signal, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Observable, catchError, of, shareReplay } from 'rxjs';
import { Store } from '@ngrx/store';
import { toSignal } from '@angular/core/rxjs-interop';
import { DocumentType } from '../../../../core/enums/documentType';
import { ComponentCardComponent } from '../../../../shared/components/common/component-card/component-card.component';
import { ModalComponent } from '../../../../shared/components/common/modal/modal.component';
import { PageBreadcrumbComponent } from '../../../../shared/components/common/page-breadcrumb/page-breadcrumb.component';
import { TableComponent } from '../../../../shared/components/table/table.component';
import { ButtonComponent } from '../../../../shared/ui/button/button.component';
import { WarehouseList } from '../../../warehouses/model/warehouse';
import { WarehouseService } from '../../../warehouses/services/warehouse-service';
import { DocumentList } from '../../model/document';
import { DocumentListQuery } from '../../services/document-service';
import { DocumentListActions } from '../../../../store/documents/document-list.actions';
import { defaultDocumentListQuery } from '../../../../store/documents/document-list.state';
import {
  selectDocumentItems,
  selectDocumentListLoading,
  selectDocumentListError,
  selectDocumentQuery,
  selectDocumentTotalItems,
  selectDocumentActionPending,
  selectDocumentActionError,
  selectDocumentActionSucceeded,
} from '../../../../store/documents/document-list.selectors';

@Component({
  selector: 'app-document-pending-list',
  standalone: true,
  imports: [CommonModule, FormsModule, PageBreadcrumbComponent, ComponentCardComponent, TableComponent, ModalComponent, ButtonComponent],
  templateUrl: './document-pending-list.component.html'
})
export class DocumentPendingListComponent implements OnInit {
  private readonly store = inject(Store);
  documents$: Observable<DocumentList[]> = this.store.select(selectDocumentItems);
  warehouses$!: Observable<WarehouseList[]>;

  readonly isLoading: Signal<boolean> = toSignal(this.store.select(selectDocumentListLoading), { initialValue: false });
  readonly errorMessage: Signal<string> = toSignal(this.store.select(selectDocumentListError), { initialValue: '' });
  readonly isActionPending: Signal<boolean> = toSignal(this.store.select(selectDocumentActionPending), { initialValue: false });
  readonly actionError: Signal<string | null> = toSignal(this.store.select(selectDocumentActionError), { initialValue: null });
  readonly actionSucceeded: Signal<boolean> = toSignal(this.store.select(selectDocumentActionSucceeded), { initialValue: false });
  private readonly query = this.store.selectSignal(selectDocumentQuery);
  readonly page = computed(() => this.query().page);
  readonly pageSize = computed(() => this.query().pageSize);
  readonly totalItems = this.store.selectSignal(selectDocumentTotalItems);
  readonly selectedDocument = signal<DocumentList | null>(null);
  readonly actionMode = signal<'confirm' | 'cancel' | null>(null);

  filters = {
    search: '',
    type: '',
    warehouseId: '',
    createdFrom: '',
    createdTo: ''
  };

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
    private warehouseService: WarehouseService
  ) {
    effect(() => {
      if (this.actionSucceeded()) {
        this.selectedDocument.set(null);
        this.actionMode.set(null);
      }
    });
  }

  ngOnInit(): void {
    this.warehouses$ = this.warehouseService.getWarehouses().pipe(
      catchError(() => of([])),
      shareReplay({ bufferSize: 1, refCount: true })
    );

    this.commitQuery(defaultDocumentListQuery);
  }

  retry(): void {
    this.commitQuery();
  }

  applyFilters(): void {
    this.commitQuery({ ...this.query(), page: 1 });
  }

  resetFilters(): void {
    this.filters = {
      search: '',
      type: '',
      warehouseId: '',
      createdFrom: '',
      createdTo: ''
    };
    this.commitQuery({ ...defaultDocumentListQuery, pageSize: this.pageSize() });
  }

  onPageChange(page: number): void {
    this.commitQuery({ ...this.query(), page });
  }

  onPageSizeChange(pageSize: number): void {
    this.commitQuery({ ...this.query(), page: 1, pageSize });
  }

  onSortChange(sort: { key: string; direction: 'asc' | 'desc' }): void {
    this.commitQuery({
      ...this.query(),
      page: 1,
      sortBy: sort.key,
      sortDirection: sort.direction,
    });
  }

  goToForm(): void {
    this.router.navigateByUrl('/documents/form');
  }

  onDocumentAction(event: { row: DocumentList; action: string }): void {
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

  openActionModal(row: DocumentList, action: 'confirm' | 'cancel'): void {
    this.selectedDocument.set(row);
    this.actionMode.set(action);
  }

  closeActionModal(): void {
    if (this.isActionPending()) return;
    this.selectedDocument.set(null);
    this.actionMode.set(null);
  }

  confirmAction(): void {
    const document = this.selectedDocument();
    const mode = this.actionMode();
    if (!document || !mode) return;

    if (mode === 'confirm') {
      this.store.dispatch(DocumentListActions.confirm({ document }));
    } else {
      this.store.dispatch(DocumentListActions.cancel({ document }));
    }
  }

  onDetails(row: DocumentList): void {
    this.router.navigateByUrl(`/documents/detail/${row.id}`);
  }

  onEdit(document: DocumentList): void {
    this.router.navigateByUrl(`/documents/form/${document.id}`);
  }

  private commitQuery(query: DocumentListQuery = this.query()): void {
    this.store.dispatch(DocumentListActions.load({
      mode: 'pending',
      query: {
        ...query,
        search: this.emptyToUndefined(this.filters.search),
        type: this.emptyToUndefined(this.filters.type),
        warehouseId: this.emptyToUndefined(this.filters.warehouseId),
        createdFrom: this.emptyToUndefined(this.filters.createdFrom),
        createdTo: this.emptyToUndefined(this.filters.createdTo),
      },
    }));
  }

  private emptyToUndefined(value: string): string | undefined {
    const trimmed = value.trim();
    return trimmed.length > 0 ? trimmed : undefined;
  }
}
