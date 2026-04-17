import { Component } from '@angular/core';
import { ComponentCardComponent } from "../../../../shared/components/common/component-card/component-card.component";
import { TableComponent } from "../../../../shared/components/table/table.component";
import { PageBreadcrumbComponent } from "../../../../shared/components/common/page-breadcrumb/page-breadcrumb.component";
import { Stock } from '../../model/stock';
import { Observable } from 'rxjs';
import { CommonModule } from '@angular/common';
import { StockMove } from '../../model/stock-move';

@Component({
  selector: 'app-stock-move',
  standalone: true,
  imports: [CommonModule, ComponentCardComponent, TableComponent, PageBreadcrumbComponent],
  templateUrl: './stock-move.component.html'
})
export class StockMoveComponent {

  stockMoves!: Observable<StockMove[]>; // Observable z danymi o ruchach magazynowych
  columns = [
    { key: 'id', label: 'Move ID', sortable: true },                       // unikalny identyfikator ruchu
    { key: 'productSku', label: 'Product SKU', sortable: true },           // identyfikator produktu
    { key: 'productName', label: 'Product Name', sortable: true },
    { key: 'fromWarehouse', label: 'From Warehouse', sortable: true },
    { key: 'fromZone', label: 'From Zone', sortable: true },
    { key: 'toWarehouse', label: 'To Warehouse', sortable: true },
    { key: 'toZone', label: 'To Zone', sortable: true },
    { key: 'quantity', label: 'Quantity', sortable: true },
    { key: 'unit', label: 'Unit', sortable: true },
    { key: 'moveType', label: 'Move Type', sortable: true },              // In / Out / Transfer
    { key: 'status', label: 'Status', sortable: true },                   // Completed / Pending / Cancelled
    { key: 'movedBy', label: 'Moved By', sortable: true },
    { key: 'movedAt', label: 'Moved At', sortable: true, type: 'date' },
    { key: 'reference', label: 'Document', sortable: true }
  ];
  
}
