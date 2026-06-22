import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { ComponentCardComponent } from "../../../../shared/components/common/component-card/component-card.component";
import { TableComponent } from "../../../../shared/components/table/table.component";
import { PageBreadcrumbComponent } from "../../../../shared/components/common/page-breadcrumb/page-breadcrumb.component";
import { Stock } from '../../model/stock';
import { StockService } from '../../services/stock-service';
import { catchError, finalize, map, Observable, of, shareReplay, startWith, Subject, switchMap } from 'rxjs';

type StockAvailabilityRow = Stock & {
  status: 'In Stock' | 'Low Stock' | 'Out of Stock';
};

@Component({
  selector: 'app-stock-availability',
  standalone: true,
  imports: [CommonModule, ComponentCardComponent, TableComponent, PageBreadcrumbComponent],
  templateUrl: './stock-availability.component.html'
})
export class StockAvailabilityComponent implements OnInit {

  stockAvailabilities$!: Observable<StockAvailabilityRow[]>;
  isLoading = false;
  errorMessage = '';
  private readonly reloadStockAvailability$ = new Subject<void>();

  constructor(private stockService: StockService) { }

  ngOnInit(): void {
    this.stockAvailabilities$ = this.reloadStockAvailability$.pipe(
      startWith(void 0),
      switchMap(() => {
        this.isLoading = true;
        this.errorMessage = '';

        return this.stockService.getAvailableStocks().pipe(
          map(stocks => stocks.map(stock => ({
            ...stock,
            status: this.getStatus(stock)
          }))),
          catchError(() => {
            this.errorMessage = 'Stock availability could not be loaded. Please try again.';
            return of([]);
          }),
          finalize(() => this.isLoading = false)
        );
      }),
      shareReplay({ bufferSize: 1, refCount: true })
    );
  }

  columns = [
    { key: 'productSku', label: 'Product SKU', sortable: true },
    { key: 'productName', label: 'Product Name', sortable: true },
    { key: 'warehouseName', label: 'Warehouse', sortable: true },
    { key: 'zoneName', label: 'Zone', sortable: true },
    { key: 'quantityAvailable', label: 'Available Quantity', sortable: true },
    { key: 'quantityReserved', label: 'Reserved Quantity', sortable: true },
    { key: 'quantityTotal', label: 'Total Quantity', sortable: true },
    { key: 'unit', label: 'Unit', sortable: true },
    { key: 'status', label: 'Status', sortable: true },
    { key: 'lastUpdated', label: 'Last Updated', sortable: true, type: 'date' }
  ];

  retry(): void {
    this.loadStockAvailability();
  }

  private loadStockAvailability(): void {
    this.reloadStockAvailability$.next();
  }

  private getStatus(stock: Stock): StockAvailabilityRow['status'] {
    if (stock.quantityAvailable <= 0) return 'Out of Stock';
    if (stock.quantityAvailable <= 10) return 'Low Stock';

    return 'In Stock';
  }
}
