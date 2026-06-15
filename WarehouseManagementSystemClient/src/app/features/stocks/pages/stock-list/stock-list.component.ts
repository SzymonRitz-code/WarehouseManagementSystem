import { Component, OnInit } from '@angular/core';
import { ComponentCardComponent } from "../../../../shared/components/common/component-card/component-card.component";
import { TableComponent } from "../../../../shared/components/table/table.component";
import { Stock } from '../../model/stock';
import { StockService } from '../../services/stock-service';
import { PageBreadcrumbComponent } from "../../../../shared/components/common/page-breadcrumb/page-breadcrumb.component";
import { catchError, finalize, Observable, of } from 'rxjs';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-stock-list',
  standalone: true,
  imports: [CommonModule, ComponentCardComponent, TableComponent, PageBreadcrumbComponent],
  templateUrl: './stock-list.component.html'
})
export class StockListComponent implements OnInit {

  stocks$: Observable<Stock[]> = of([]);
  isLoading = false;
  errorMessage = '';

  constructor(private stockService: StockService) { }

  ngOnInit(): void {
    this.loadStocks();
  }

  retry(): void {
    this.loadStocks();
  }

  private loadStocks(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.stocks$ = this.stockService.getStocks().pipe(
      catchError(() => {
        this.errorMessage = 'Stocks could not be loaded. Please try again.';
        return of([]);
      }),
      finalize(() => this.isLoading = false)
    );
  }

  columns = [
    { key: 'productSku', label: 'Product SKU', sortable: true },
    { key: 'productName', label: 'Product Name', sortable: true },
    { key: 'warehouseName', label: 'Warehouse', sortable: true },
    { key: 'zoneName', label: 'Zone', sortable: true },
    { key: 'quantityAvailable', label: 'Available Qty', sortable: true },
    { key: 'quantityReserved', label: 'Reserved Qty', sortable: true },
    { key: 'quantityTotal', label: 'Total Qty', sortable: true },
    { key: 'unit', label: 'Unit', sortable: true },
    { key: 'lastUpdated', label: 'Last Updated', sortable: true, type: 'date' }
  ];
}
