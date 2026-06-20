import { Component, OnInit } from '@angular/core';
import { DocumentListQuery, DocumentService } from '../../services/document-service';
import { PageBreadcrumbComponent } from "../../../../shared/components/common/page-breadcrumb/page-breadcrumb.component";
import { ComponentCardComponent } from "../../../../shared/components/common/component-card/component-card.component";
import { TableComponent } from "../../../../shared/components/table/table.component";
import { Router } from '@angular/router';
import { DocumentList } from '../../model/document';
import { catchError, finalize, map, Observable, of, shareReplay, startWith, Subject, switchMap, take } from 'rxjs';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DocumentStatus } from '../../../../core/enums/documentStatus';
import { DocumentType } from '../../../../core/enums/documentType';
import { WarehouseList } from '../../../warehouses/model/warehouse';
import { WarehouseService } from '../../../warehouses/services/warehouse-service';

@Component({
  selector: 'app-document-list',
  standalone: true,
  imports: [CommonModule, FormsModule, PageBreadcrumbComponent, ComponentCardComponent, TableComponent],
  templateUrl: './document-list.component.html'
})
export class DocumentListComponent implements OnInit {

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
    status: '',
    warehouseId: '',
    createdFrom: '',
    createdTo: ''
  };

  readonly documentTypes = Object.values(DocumentType);
  readonly documentStatuses = Object.values(DocumentStatus);
  private readonly reloadDocuments$ = new Subject<void>();

  columns = [
    { key: 'documentNumber', label: 'Document Number', sortable: true },        // numer nadany w systemie
    { key: 'type', label: 'Type', sortable: true },                             // typ: Receipt / Issue / Transfer / Adjustment
    { key: 'status', label: 'Status', sortable: true },                         // Draft / Confirmed / Completed / Cancelled
    { key: 'sourceWarehouse', label: 'From Warehouse', sortable: false },       // magazyn źródłowy (dla transferów/wydań)
    { key: 'destinationWarehouse', label: 'To Warehouse', sortable: false },    // magazyn docelowy (dla przyjęć/transferów)
    { key: 'createdBy', label: 'Created By', sortable: true },                  // kto utworzył dokument
    { key: 'approvedBy', label: 'Approved By', sortable: true },                // kto zatwierdził
    { key: 'createdAt', label: 'Created At', sortable: true, type: 'date' },    // data utworzenia
    { key: 'approvedAt', label: 'Approved At', sortable: true, type: 'date' },  // data zatwierdzenia
    { key: 'itemCount', label: 'Items', sortable: false },                      // liczba produktów w dokumencie
    { key: 'totalQuantity', label: 'Total Qty', sortable: false }               // suma ilości wszystkich produktów

  ];

  documentActions = [    // np. podgląd, edycja, PDF, zatwierdzenie
    { label: 'Details', action: 'details' },
  ];

  constructor(
    private router: Router,
    private documentService: DocumentService,
    private warehouseService: WarehouseService
  ) { }

  ngOnInit(): void {
    // RxJS insight: HttpClient observables are cold, so each subscription would normally start
    // a new request. shareReplay(1) turns the warehouse lookup into a shared view dependency:
    // the template can resubscribe through async pipe without duplicating the HTTP call.
    this.warehouses$ = this.warehouseService.getWarehouses().pipe(
      catchError(() => of([])),
      shareReplay({ bufferSize: 1, refCount: true })
    );

    // RxJS insight: the old approach was "call loadDocuments() and replace the observable/state".
    // Here UI events only emit into reloadDocuments$, while one pipeline owns loading, errors,
    // query building and result mapping. switchMap is important because it cancels an older
    // request when the user quickly changes filters/page/sort, so a stale response cannot
    // overwrite the newest table state.
    this.documents$ = this.reloadDocuments$.pipe(
      startWith(void 0),
      switchMap(() => {
        // Keep loading inside switchMap. When switchMap cancels an old inner request, that old
        // request's finalize runs first; setting loading here keeps the newest request authoritative.
        this.isLoading = true;
        this.errorMessage = '';
        return this.documentService.getDocuments(this.buildQuery()).pipe(
          catchError(() => {
            this.errorMessage = 'Documents could not be loaded. Please try again.';
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
    this.loadDocuments();
  }

  private loadDocuments(): void {
    // This is intentionally tiny: commands such as retry, filtering and pagination do not know
    // how data is loaded. They only declare that the current query should be re-run.
    this.reloadDocuments$.next();
  }

  applyFilters(): void {
    this.page = 1;
    this.loadDocuments();
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
    this.page = 1;
    this.sortBy = 'createdAt';
    this.sortDirection = 'desc';
    this.loadDocuments();
  }

  onPageChange(page: number): void {
    this.page = page;
    this.loadDocuments();
  }

  onPageSizeChange(pageSize: number): void {
    this.pageSize = pageSize;
    this.page = 1;
    this.loadDocuments();
  }

  onSortChange(sort: { key: string; direction: 'asc' | 'desc' }): void {
    this.sortBy = sort.key;
    this.sortDirection = sort.direction;
    this.page = 1;
    this.loadDocuments();
  }

  private buildQuery(): DocumentListQuery {
    // The backend owns paging/filtering/sorting for documents. The frontend only serializes the
    // current table state into a query object, which keeps the heavy work away from the browser.
    return {
      page: this.page,
      pageSize: this.pageSize,
      search: this.emptyToUndefined(this.filters.search),
      type: this.emptyToUndefined(this.filters.type),
      status: this.emptyToUndefined(this.filters.status),
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
    // The table receives only the current page items, while pagination metadata stays as component
    // state because the shared table component expects totalItems/page/pageSize as inputs.
    this.page = result.page;
    this.pageSize = result.pageSize;
    this.totalItems = result.totalItems;

    return result.items;
  }


  goToForm() {
    this.router.navigateByUrl('/documents/form')
  }
  onDocumentAction(event: { row: DocumentList; action: string }) {
    const { row, action } = event;
    console.log(`Action "${action}" triggered for document:`, row);
    switch (action) {
      case 'edit':
        this.onEdit(row);
        break;

      case 'details':
        this.onDetails(row);
        break;
    }
  }
  onCancel(row: DocumentList) {
    // RxJS insight: this is a command, not view data. A direct subscribe is acceptable here because
    // the side effect is the point of the method. take(1) makes the one-response contract explicit
    // and protects us if the service implementation ever becomes long-lived.
    this.documentService.cancelDocument(row).pipe(take(1)).subscribe({
      next: (updatedDoc) => {
        // Aktualizuj widok lub pokaż powiadomienie
        console.log(`Document ${updatedDoc.id} cancelled.`);
        this.loadDocuments();
      },
      error: (err) => {
        console.error('Error cancelling document:', err);
      }
    });
  }
  onConfirm(row: DocumentList) {
    // Same command pattern as cancel: execute once, then refresh the table through the reload stream.
    this.documentService.confirmDocument(row).pipe(take(1)).subscribe({
      next: (updatedDoc) => {
        // Aktualizuj widok lub pokaż powiadomienie
        console.log(`Document ${updatedDoc.id} confirmed.`);
        this.loadDocuments();
      },
      error: (err) => {
        console.error('Error confirming document:', err);
      }
    });
  }
  onDetails(row: DocumentList) {
    this.router.navigateByUrl(`/documents/detail/${row.id}`)
  }
  onEdit(document: DocumentList) {
    console.log(`Edit: ${document.id}`)
    this.router.navigateByUrl(`/documents/form/${document.id}`)
  }
}
