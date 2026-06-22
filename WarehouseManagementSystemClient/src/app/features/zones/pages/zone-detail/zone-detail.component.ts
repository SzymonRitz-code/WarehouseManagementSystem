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
import { CommonModule } from '@angular/common';
import { catchError, map, Observable, of, shareReplay, switchMap, tap } from 'rxjs';
@Component({
  selector: 'app-zone-detail',
  standalone: true,
  imports: [
    CommonModule,
    PageBreadcrumbComponent, 
    ComponentCardComponent, 
    LabelComponent, 
    InputDetailComponent, 
    DetailActionsComponent],
  templateUrl: './zone-detail.component.html'
})
export class ZoneDetailComponent implements OnInit {

  id!: string;
  zone$!: Observable<Zone | undefined>;

  constructor(private zoneService: ZoneService, private warehouseService: WarehouseService, private activatedRoute: ActivatedRoute, private router: Router) { }
  ngOnInit(): void {
    this.zone$ = this.activatedRoute.paramMap.pipe(
      map(params => params.get('id')!),
      tap(id => this.id = id),
      switchMap(id => this.zoneService.getZone(id)),
      switchMap(zone => this.warehouseService.getWarehouse(zone.warehouseId).pipe(
        map(warehouse => ({ ...zone, warehouseName: warehouse.name })),
        catchError(() => of(zone))
      )),
      catchError(() => of(undefined)),
      shareReplay({ bufferSize: 1, refCount: true })
    );

  }
  onBack() {
    this.router.navigateByUrl('/zones');
  }
  onEdit() {
    this.router.navigateByUrl(`/zones/form/${this.id}`);
  }
}
