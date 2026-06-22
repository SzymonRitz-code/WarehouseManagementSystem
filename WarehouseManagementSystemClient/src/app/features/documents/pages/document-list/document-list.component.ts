import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import {
  BehaviorSubject,
  catchError,
  finalize,
  map,
  merge,
  Observable,
  of,
  shareReplay,
  Subject,
  switchMap,
  take
} from 'rxjs';
import { DocumentStatus } from '../../../../core/enums/documentStatus';
import { DocumentType } from '../../../../core/enums/documentType';
import { ComponentCardComponent } from '../../../../shared/components/common/component-card/component-card.component';
import { PageBreadcrumbComponent } from '../../../../shared/components/common/page-breadcrumb/page-breadcrumb.component';
import { TableComponent } from '../../../../shared/components/table/table.component';
import { WarehouseList } from '../../../warehouses/model/warehouse';
import { WarehouseService } from '../../../warehouses/services/warehouse-service';
import { DocumentList } from '../../model/document';
import { DocumentListQuery, DocumentService } from '../../services/document-service';

@Component({
  selector: 'app-document-list',
  standalone: true,
  imports: [CommonModule, FormsModule, PageBreadcrumbComponent, ComponentCardComponent, TableComponent],
  templateUrl: './document-list.component.html'
})
export class DocumentListComponent implements OnInit {
  documents$!: Observable<DocumentList[]>;
  warehouses$!: Observable<WarehouseList[]>;

  readonly isLoading = signal(false);
  readonly errorMessage = signal('');
  readonly page = signal(1);
  readonly pageSize = signal(10);
  readonly totalItems = signal(0);
  readonly sortBy = signal('createdAt');
  readonly sortDirection = signal<'asc' | 'desc'>('desc');

  filters = {
    search: '',
    type: '',
    status: '',
    warehouseId: '',
    createdFrom: '',
    createdTo: ''
  };

  readonly documentTypes = Object.values(DocumentType);
  readonly documentStatuses = Object.values(DocumentStatus);

  private readonly refreshDocuments$ = new Subject<void>();
  private readonly queryState$ = new BehaviorSubject<DocumentListQuery>(this.buildQuery());

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
    { label: 'Details', action: 'details' }
  ];

  constructor(
    private router: Router,
    private documentService: DocumentService,
    private warehouseService: WarehouseService
  ) {}

  ngOnInit(): void {
    this.warehouses$ = this.warehouseService.getWarehouses().pipe(
      catchError(() => of([])),
      shareReplay({ bufferSize: 1, refCount: true })
    );

    this.documents$ = merge(
      this.queryState$,
      this.refreshDocuments$.pipe(map(() => this.queryState$.value))
    ).pipe(
      switchMap((query) => {
        this.isLoading.set(true);
        this.errorMessage.set('');

        return this.documentService.getDocuments(query).pipe(
          catchError(() => {
            this.errorMessage.set('Documents could not be loaded. Please try again.');
            this.totalItems.set(0);
            return of({
              items: [],
              page: this.page(),
              pageSize: this.pageSize(),
              totalItems: 0,
              totalPages: 0
            });
          }),
          finalize(() => this.isLoading.set(false))
        );
      }),
      map(result => this.setPageResult(result)),
      shareReplay({ bufferSize: 1, refCount: true })
    );
  }

  retry(): void {
    this.refreshDocuments$.next();
  }

  applyFilters(): void {
    this.page.set(1);
    this.commitQuery();
  }

  resetFilters(): void {
    this.filters = {
      search: '',
      type: '',
      status: '',
      warehouseId: '',
      createdFrom: '',
      createdTo: ''
    };
    this.page.set(1);
    this.sortBy.set('createdAt');
    this.sortDirection.set('desc');
    this.commitQuery();
  }

  onPageChange(page: number): void {
    this.page.set(page);
    this.commitQuery();
  }

  onPageSizeChange(pageSize: number): void {
    this.pageSize.set(pageSize);
    this.page.set(1);
    this.commitQuery();
  }

  onSortChange(sort: { key: string; direction: 'asc' | 'desc' }): void {
    this.sortBy.set(sort.key);
    this.sortDirection.set(sort.direction);
    this.page.set(1);
    this.commitQuery();
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
      case 'details':
        this.onDetails(row);
        break;
    }
  }

  onCancel(row: DocumentList): void {
    this.documentService.cancelDocument(row).pipe(take(1)).subscribe({
      next: () => this.retry(),
      error: (err) => console.error('Error cancelling document:', err)
    });
  }

  onConfirm(row: DocumentList): void {
    this.documentService.confirmDocument(row).pipe(take(1)).subscribe({
      next: () => this.retry(),
      error: (err) => console.error('Error confirming document:', err)
    });
  }

  onDetails(row: DocumentList): void {
    this.router.navigateByUrl(`/documents/detail/${row.id}`);
  }

  onEdit(document: DocumentList): void {
    this.router.navigateByUrl(`/documents/form/${document.id}`);
  }

  private commitQuery(): void {
    this.queryState$.next(this.buildQuery());
  }

  private buildQuery(): DocumentListQuery {
    return {
      page: this.page(),
      pageSize: this.pageSize(),
      search: this.emptyToUndefined(this.filters.search),
      type: this.emptyToUndefined(this.filters.type),
      status: this.emptyToUndefined(this.filters.status),
      warehouseId: this.emptyToUndefined(this.filters.warehouseId),
      createdFrom: this.emptyToUndefined(this.filters.createdFrom),
      createdTo: this.emptyToUndefined(this.filters.createdTo),
      sortBy: this.sortBy(),
      sortDirection: this.sortDirection()
    };
  }

  private emptyToUndefined(value: string): string | undefined {
    const trimmed = value.trim();
    return trimmed.length > 0 ? trimmed : undefined;
  }

  private setPageResult(result: { items: DocumentList[]; page: number; pageSize: number; totalItems: number }): DocumentList[] {
    this.page.set(result.page);
    this.pageSize.set(result.pageSize);
    this.totalItems.set(result.totalItems);

    return result.items;
  }
}
