import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
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
  switchMap
} from 'rxjs';
import { ComponentCardComponent } from '../../../../shared/components/common/component-card/component-card.component';
import { PageBreadcrumbComponent } from '../../../../shared/components/common/page-breadcrumb/page-breadcrumb.component';
import { TableComponent } from '../../../../shared/components/table/table.component';
import { WarehouseList } from '../../../warehouses/model/warehouse';
import { WarehouseService } from '../../../warehouses/services/warehouse-service';
import { ZoneList } from '../../../zones/model/zone';
import { ZoneService } from '../../../zones/services/zone-service';
import { Stock } from '../../model/stock';
import { StockListQuery, StockService } from '../../services/stock-service';

@Component({
  selector: 'app-stock-list',
  standalone: true,
  imports: [CommonModule, FormsModule, ComponentCardComponent, TableComponent, PageBreadcrumbComponent],
  templateUrl: './stock-list.component.html'
})
export class StockListComponent implements OnInit {
  stocks$!: Observable<Stock[]>;
  warehouses$!: Observable<WarehouseList[]>;
  zones$!: Observable<ZoneList[]>;

  readonly isLoading = signal(false);
  readonly errorMessage = signal('');
  readonly page = signal(1);
  readonly pageSize = signal(10);
  readonly totalItems = signal(0);
  readonly sortBy = signal('lastUpdated');
  readonly sortDirection = signal<'asc' | 'desc'>('desc');

  filters = {
    search: '',
    warehouseId: '',
    zoneId: '',
    availableOnly: false
  };

  private readonly refreshStocks$ = new Subject<void>();
  private readonly queryState$ = new BehaviorSubject<StockListQuery>(this.buildQuery());

  columns = [
    { key: 'productSku', label: 'Product SKU', sortable: false },
    { key: 'productName', label: 'Product Name', sortable: false },
    { key: 'warehouseName', label: 'Warehouse', sortable: false },
    { key: 'zoneName', label: 'Zone', sortable: false },
    { key: 'quantityAvailable', label: 'Available Qty', sortable: true },
    { key: 'quantityReserved', label: 'Reserved Qty', sortable: true },
    { key: 'quantityTotal', label: 'Total Qty', sortable: true },
    { key: 'unit', label: 'Unit', sortable: false },
    { key: 'lastUpdated', label: 'Last Updated', sortable: true, type: 'date' }
  ];

  constructor(
    private stockService: StockService,
    private warehouseService: WarehouseService,
    private zoneService: ZoneService
  ) {}

  ngOnInit(): void {
    this.warehouses$ = this.warehouseService.getWarehouses().pipe(
      catchError(() => of([])),
      shareReplay({ bufferSize: 1, refCount: true })
    );

    this.zones$ = this.zoneService.getZones().pipe(
      catchError(() => of([])),
      shareReplay({ bufferSize: 1, refCount: true })
    );

    this.stocks$ = merge(
      this.queryState$,
      this.refreshStocks$.pipe(map(() => this.queryState$.value))
    ).pipe(
      switchMap((query) => {
        this.isLoading.set(true);
        this.errorMessage.set('');

        return this.stockService.getStocks(query).pipe(
          catchError(() => {
            this.errorMessage.set('Stocks could not be loaded. Please try again.');
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
    this.refreshStocks$.next();
  }

  applyFilters(): void {
    this.page.set(1);
    this.commitQuery();
  }

  resetFilters(): void {
    this.filters = {
      search: '',
      warehouseId: '',
      zoneId: '',
      availableOnly: false
    };
    this.page.set(1);
    this.sortBy.set('lastUpdated');
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

  private commitQuery(): void {
    this.queryState$.next(this.buildQuery());
  }

  private buildQuery(): StockListQuery {
    return {
      page: this.page(),
      pageSize: this.pageSize(),
      search: this.emptyToUndefined(this.filters.search),
      warehouseId: this.emptyToUndefined(this.filters.warehouseId),
      zoneId: this.emptyToUndefined(this.filters.zoneId),
      availableOnly: this.filters.availableOnly ? true : undefined,
      sortBy: this.sortBy(),
      sortDirection: this.sortDirection()
    };
  }

  private emptyToUndefined(value: string): string | undefined {
    const trimmed = value.trim();
    return trimmed.length > 0 ? trimmed : undefined;
  }

  private setPageResult(result: { items: Stock[]; page: number; pageSize: number; totalItems: number }): Stock[] {
    this.page.set(result.page);
    this.pageSize.set(result.pageSize);
    this.totalItems.set(result.totalItems);

    return result.items;
  }
}
