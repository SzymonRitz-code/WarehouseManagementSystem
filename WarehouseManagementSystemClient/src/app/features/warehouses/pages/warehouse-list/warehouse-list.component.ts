import { Component, OnInit } from '@angular/core';
import { ComponentCardComponent } from "../../../../shared/components/common/component-card/component-card.component";
import { TableComponent } from "../../../../shared/components/table/table.component";
import { WarehouseService } from '../../../services/warehouse-service';
import { Warehouse } from '../../model/warehouse';
import { Router } from '@angular/router';
import { PageBreadcrumbComponent } from "../../../../shared/components/common/page-breadcrumb/page-breadcrumb.component";

@Component({
  selector: 'app-warehouse-list',
  standalone: true,
  imports: [ComponentCardComponent, TableComponent, PageBreadcrumbComponent],
  templateUrl: './warehouse-list.component.html'
})
export class WarehouseListComponent implements OnInit {

  warehouses: Warehouse[] = [];

  constructor(private warehouseService: WarehouseService, private router: Router) { }

  ngOnInit(): void {
    this.warehouses = this.warehouseService.warehouses;
  }
  columns = [
    { key: 'id', label: 'ID', sortable: true },
    { key: 'code', label: 'Code', sortable: true },
    { key: 'WarehouseName', label: 'Warehouse Name', sortable: true },
    { key: 'country', label: 'Country', sortable: true },
    { key: 'address', label: 'Address', sortable: true },
    { key: 'zonesCount', label: 'Zones Count', sortable: true },
    { key: 'totalStock', label: 'Total Stock', sortable: true },
    { key: 'totalQty', label: 'Total Qty', sortable: true },
    { key: 'Status', label: 'Status', sortable: true },
    { key: 'createdAt', label: 'CreatedAt', sortable: true, type: 'date' }
  ];
  warehouseActions = [
    { label: 'Edit', action: 'edit' },
    { label: 'Details', action: 'details' },
  ];
  goToForm() {
    this.router.navigate(['/warehouses/form']);
  }
  onWarehouseAction(event: { row: Warehouse; action: string }) {
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
  onDetails(warehouse: Warehouse) {
    this.router.navigateByUrl(`/warehouses/detail/${warehouse.id}`)
  }
  onEdit(warehouse: Warehouse) {
    this.router.navigateByUrl(`/warehouses/form/${warehouse.id}`)
  }
}
