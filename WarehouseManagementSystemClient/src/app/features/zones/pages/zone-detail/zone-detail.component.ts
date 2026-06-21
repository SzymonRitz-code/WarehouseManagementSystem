import { Component, OnInit } from '@angular/core';
import { PageBreadcrumbComponent } from "../../../../shared/components/common/page-breadcrumb/page-breadcrumb.component";
import { ComponentCardComponent } from "../../../../shared/components/common/component-card/component-card.component";
import { LabelComponent } from "../../../../shared/components/form/label/label.component";
import { InputDetailComponent } from "../../../../shared/components/form/input/input-detail.component";
import { DetailActionsComponent } from "../../../../shared/components/form/detail-actions/detail-actions.component";
import { Zone } from '../../model/zone';
import { ActivatedRoute, Router } from '@angular/router';
import { WarehouseService } from '../../../warehouses/services/warehouse-service';
import { ZoneService } from '../../services/zone-service';
@Component({
  selector: 'app-zone-detail',
  standalone: true,
  imports: [
    PageBreadcrumbComponent, 
    ComponentCardComponent, 
    LabelComponent, 
    InputDetailComponent, 
    DetailActionsComponent],
  templateUrl: './zone-detail.component.html'
})
export class ZoneDetailComponent implements OnInit {

  id!: string;
  zone!: Zone | undefined;

  constructor(private zoneService: ZoneService, private warehouseService: WarehouseService, private activatedRoute: ActivatedRoute, private router: Router) { }
  ngOnInit(): void {
    this.id = this.activatedRoute.snapshot.paramMap.get('id')!;
    this.zoneService.getZone(this.id).subscribe({
      next: (result: Zone) => {
        this.zone = result;
        this.warehouseService.getWarehouse(this.zone.warehouseId).subscribe({
          next: (responce) => {
            this.zone!.warehouseName = responce.name
          }
        })
      }
    })

  }
  onBack() {
    this.router.navigateByUrl('/zones');
  }
  onEdit() {
    this.router.navigateByUrl(`/zones/form/${this.id}`);
  }
}
