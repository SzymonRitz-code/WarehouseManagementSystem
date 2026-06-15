import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { ComponentCardComponent } from "../../../../shared/components/common/component-card/component-card.component";
import { TableComponent } from "../../../../shared/components/table/table.component";
import { PageBreadcrumbComponent } from "../../../../shared/components/common/page-breadcrumb/page-breadcrumb.component";
import { Stock } from '../../model/stock';
import { StockService } from '../../services/stock-service';
import { catchError, finalize, map, Observable, of } from 'rxjs';

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

  stockAvailabilities$: Observable<StockAvailabilityRow[]> = of([]);
  isLoading = false;
  errorMessage = '';

  constructor(private stockService: StockService) { }

  ngOnInit(): void {
    this.loadStockAvailability();
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
    this.isLoading = true;
    this.errorMessage = '';

    this.stockAvailabilities$ = this.stockService.getAvailableStocks().pipe(
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
  }

  private getStatus(stock: Stock): StockAvailabilityRow['status'] {
    if (stock.quantityAvailable <= 0) return 'Out of Stock';
    if (stock.quantityAvailable <= 10) return 'Low Stock';

    return 'In Stock';
  }
}
