import { Component, OnInit } from '@angular/core';
import { ComponentCardComponent } from "../../../../shared/components/common/component-card/component-card.component";
import { TableComponent } from "../../../../shared/components/table/table.component";
import { Stock } from '../../model/stock';
import { StockService } from '../../../services/stock-service';
import { PageBreadcrumbComponent } from "../../../../shared/components/common/page-breadcrumb/page-breadcrumb.component";

@Component({
  selector: 'app-stock-list',
  standalone: true,
  imports: [ComponentCardComponent, TableComponent, PageBreadcrumbComponent],
  templateUrl: './stock-list.component.html'
})
export class StockListComponent implements OnInit {

  stocks: Stock[] = [];
  constructor(private stockService: StockService) { }

  ngOnInit(): void {
    this.stocks = this.stockService.stocks
  }

  columns = [
    { key: 'id', label: 'ID', sortable: true },
    { key: 'productSku', label: 'Product SKU', sortable: true },
    { key: 'productName', label: 'Product Name', sortable: true },
    { key: 'warehouse', label: 'Warehouse', sortable: true },
    { key: 'zone', label: 'Zone', sortable: true },
    { key: 'availableQty', label: 'Available Qty', sortable: true },
    { key: 'reservedQty', label: 'Reserved Qty', sortable: true },
    { key: 'totalQty', label: 'Total Qty', sortable: true },
    { key: 'lastUpdated', label: 'Last Updated', sortable: true, type: 'date' }
  ];
}