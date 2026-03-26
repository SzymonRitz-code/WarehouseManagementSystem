import { Component, OnInit } from '@angular/core';
import { PageBreadcrumbComponent } from "../../../../shared/components/common/page-breadcrumb/page-breadcrumb.component";
import { ComponentCardComponent } from "../../../../shared/components/common/component-card/component-card.component";
import { InputDetailComponent } from "../../../../shared/components/form/input/input-detail.component";
import { LabelComponent } from "../../../../shared/components/form/label/label.component";
import { DetailActionsComponent } from "../../../../shared/components/form/detail-actions/detail-actions.component";
import { Warehouse } from '../../model/warehouse';
import { ActivatedRoute, Router } from '@angular/router';
import { WarehouseService } from '../../../services/warehouse-service';

@Component({
  selector: 'app-warehouse-detail',
  standalone: true,
  imports: [PageBreadcrumbComponent, ComponentCardComponent, InputDetailComponent, LabelComponent, DetailActionsComponent],
  templateUrl: './warehouse-detail.component.html'
})
export class WarehouseDetailComponent implements OnInit {

  id!: string;
  warehouse!: Warehouse;
  constructor(private router: Router, private activatedRoute: ActivatedRoute, private warehouseService: WarehouseService) { }
  ngOnInit(): void {
    this.id = this.activatedRoute.snapshot.paramMap.get('id')!;
    this.warehouse = this.warehouseService.getWarehouse(this.id) as Warehouse
  }
  onEdit() {
    this.router.navigateByUrl(`/warehouses/form/${(this.warehouse as Warehouse).id}`)
  }
  onBack() {
    this.router.navigateByUrl('/warehouses')
  }


}
