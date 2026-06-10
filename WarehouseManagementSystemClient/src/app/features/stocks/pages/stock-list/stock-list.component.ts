import { Component, OnInit } from '@angular/core';
import { ComponentCardComponent } from "../../../../shared/components/common/component-card/component-card.component";
import { TableComponent } from "../../../../shared/components/table/table.component";
import { Stock } from '../../model/stock';
import { StockService } from '../../services/stock-service';
import { PageBreadcrumbComponent } from "../../../../shared/components/common/page-breadcrumb/page-breadcrumb.component";
import { Observable } from 'rxjs';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-stock-list',
  standalone: true,
  imports: [CommonModule, ComponentCardComponent, TableComponent, PageBreadcrumbComponent],
  templateUrl: './stock-list.component.html'
})
export class StockListComponent implements OnInit {

  stocks$: Observable<Stock[]> = new Observable<Stock[]>();
  constructor(private stockService: StockService) { }

  ngOnInit(): void {
    this.stocks$ = this.stockService.getStocks();
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