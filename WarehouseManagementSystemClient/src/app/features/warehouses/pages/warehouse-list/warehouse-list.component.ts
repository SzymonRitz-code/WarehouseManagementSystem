import { Component, OnInit } from '@angular/core';
import { ComponentCardComponent } from "../../../../shared/components/common/component-card/component-card.component";
import { TableComponent } from "../../../../shared/components/table/table.component";
import { WarehouseService } from '../../services/warehouse-service';
import { WarehouseList } from '../../model/warehouse';
import { Router } from '@angular/router';
import { PageBreadcrumbComponent } from "../../../../shared/components/common/page-breadcrumb/page-breadcrumb.component";
import { CommonModule } from '@angular/common';
import { catchError, finalize, Observable, of, shareReplay, startWith, Subject, switchMap } from 'rxjs';

@Component({
  selector: 'app-warehouse-list',
  standalone: true,
  imports: [CommonModule, ComponentCardComponent, TableComponent, PageBreadcrumbComponent],
  templateUrl: './warehouse-list.component.html'
})
export class WarehouseListComponent implements OnInit { 

  warehouses$!: Observable<WarehouseList[]>;
  isLoading = false;
  errorMessage = '';
  private readonly reloadWarehouses$ = new Subject<void>();

  constructor(private warehouseService: WarehouseService, private router: Router) { }

  ngOnInit(): void {
    this.warehouses$ = this.reloadWarehouses$.pipe(
      startWith(void 0),
      switchMap(() => {
        this.isLoading = true;
        this.errorMessage = '';

        return this.warehouseService.getWarehouses().pipe(
          catchError(() => {
            this.errorMessage = 'Warehouses could not be loaded. Please try again.';
            return of([]);
          }),
          finalize(() => this.isLoading = false)
        );
      }),
      shareReplay({ bufferSize: 1, refCount: true })
    );
  }

  retry(): void {
    this.loadWarehouses();
  }

  private loadWarehouses(): void {
    this.reloadWarehouses$.next();
  }

  columns = [
    { key: 'code', label: 'Code', sortable: true },
    { key: 'name', label: 'Warehouse Name', sortable: true },
    { key: 'country', label: 'Country', sortable: true },
    { key: 'address', label: 'Address', sortable: true },
    { key: 'zonesCount', label: 'Zones Count', sortable: true },
    { key: 'totalStock', label: 'Total Stock', sortable: true },
    { key: 'totalQty', label: 'Total Qty', sortable: true },
    { key: 'isActive', label: 'Is Active', sortable: true, type: 'boolean' },
    { key: 'createdAt', label: 'CreatedAt', sortable: true, type: 'date' }
  ];

  warehouseActions = [
    { label: 'Edit', action: 'edit' },
    { label: 'Details', action: 'details' },
  ];
  goToForm() {
    this.router.navigate(['/warehouses/form']);
  }
  onWarehouseAction(event: { row: WarehouseList; action: string }) {
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
  onDetails(warehouse: WarehouseList) {
    this.router.navigateByUrl(`/warehouses/detail/${warehouse.id}`)
  }
  onEdit(warehouse: WarehouseList) {
    this.router.navigateByUrl(`/warehouses/form/${warehouse.id}`)
  }
}
