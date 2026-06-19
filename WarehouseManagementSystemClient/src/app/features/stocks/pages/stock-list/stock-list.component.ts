import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ComponentCardComponent } from "../../../../shared/components/common/component-card/component-card.component";
import { TableComponent } from "../../../../shared/components/table/table.component";
import { Stock } from '../../model/stock';
import { StockListQuery, StockService } from '../../services/stock-service';
import { PageBreadcrumbComponent } from "../../../../shared/components/common/page-breadcrumb/page-breadcrumb.component";
import { catchError, finalize, map, Observable, of } from 'rxjs';
import { CommonModule } from '@angular/common';
import { WarehouseList } from '../../../warehouses/model/warehouse';
import { WarehouseService } from '../../../warehouses/services/warehouse-service';
import { ZoneList } from '../../../zones/model/zone';
import { ZoneService } from '../../../zones/services/zone-service';

@Component({
  selector: 'app-stock-list',
  standalone: true,
  imports: [CommonModule, FormsModule, ComponentCardComponent, TableComponent, PageBreadcrumbComponent],
  templateUrl: './stock-list.component.html'
})
export class StockListComponent implements OnInit {

  stocks$: Observable<Stock[]> = of([]);
  warehouses$: Observable<WarehouseList[]> = of([]);
  zones$: Observable<ZoneList[]> = of([]);
  isLoading = false;
  errorMessage = '';
  page = 1;
  pageSize = 10;
  totalItems = 0;
  sortBy = 'lastUpdated';
  sortDirection: 'asc' | 'desc' = 'desc';
  filters = {
    search: '',
    warehouseId: '',
    zoneId: '',
    availableOnly: false
  };

  constructor(
    private stockService: StockService,
    private warehouseService: WarehouseService,
    private zoneService: ZoneService
  ) { }

  ngOnInit(): void {
    this.warehouses$ = this.warehouseService.getWarehouses().pipe(
      catchError(() => of([]))
    );
    this.zones$ = this.zoneService.getZones().pipe(
      catchError(() => of([]))
    );
    this.loadStocks();
  }

  retry(): void {
    this.loadStocks();
  }

  private loadStocks(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.stocks$ = this.stockService.getStocks(this.buildQuery()).pipe(
      catchError(() => {
        this.errorMessage = 'Stocks could not be loaded. Please try again.';
        this.totalItems = 0;
        return of({ items: [], page: this.page, pageSize: this.pageSize, totalItems: 0, totalPages: 0 });
      }),
      map(result => this.setPageResult(result)),
      finalize(() => this.isLoading = false)
    );
  }

  applyFilters(): void {
    this.page = 1;
    this.loadStocks();
  }

  resetFilters(): void {
    this.filters = {
      search: '',
      warehouseId: '',
      zoneId: '',
      availableOnly: false
    };
    this.page = 1;
    this.sortBy = 'lastUpdated';
    this.sortDirection = 'desc';
    this.loadStocks();
  }

  onPageChange(page: number): void {
    this.page = page;
    this.loadStocks();
  }

  onPageSizeChange(pageSize: number): void {
    this.pageSize = pageSize;
    this.page = 1;
    this.loadStocks();
  }

  onSortChange(sort: { key: string; direction: 'asc' | 'desc' }): void {
    this.sortBy = sort.key;
    this.sortDirection = sort.direction;
    this.page = 1;
    this.loadStocks();
  }

  private buildQuery(): StockListQuery {
    return {
      page: this.page,
      pageSize: this.pageSize,
      search: this.emptyToUndefined(this.filters.search),
      warehouseId: this.emptyToUndefined(this.filters.warehouseId),
      zoneId: this.emptyToUndefined(this.filters.zoneId),
      availableOnly: this.filters.availableOnly ? true : undefined,
      sortBy: this.sortBy,
      sortDirection: this.sortDirection
    };
  }

  private emptyToUndefined(value: string): string | undefined {
    const trimmed = value.trim();
    return trimmed.length > 0 ? trimmed : undefined;
  }

  private setPageResult(result: { items: Stock[]; page: number; pageSize: number; totalItems: number }): Stock[] {
    this.page = result.page;
    this.pageSize = result.pageSize;
    this.totalItems = result.totalItems;

    return result.items;
  }

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
}
