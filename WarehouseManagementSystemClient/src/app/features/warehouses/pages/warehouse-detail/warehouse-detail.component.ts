import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PageBreadcrumbComponent } from "../../../../shared/components/common/page-breadcrumb/page-breadcrumb.component";
import { ComponentCardComponent } from "../../../../shared/components/common/component-card/component-card.component";
import { InputDetailComponent } from "../../../../shared/components/form/input/input-detail.component";
import { LabelComponent } from "../../../../shared/components/form/label/label.component";
import { DetailActionsComponent } from "../../../../shared/components/form/detail-actions/detail-actions.component";
import { Warehouse } from '../../model/warehouse';
import { ActivatedRoute, Router } from '@angular/router';
import { WarehouseService } from '../../services/warehouse-service';
import { catchError, map, Observable, of, shareReplay, switchMap, tap } from 'rxjs';

@Component({
  selector: 'app-warehouse-detail',
  standalone: true,
  imports: [CommonModule, PageBreadcrumbComponent, ComponentCardComponent, InputDetailComponent, LabelComponent, DetailActionsComponent],
  templateUrl: './warehouse-detail.component.html'
})
export class WarehouseDetailComponent implements OnInit {

  id!: string;
  warehouse$!: Observable<Warehouse | undefined>;
  constructor(private router: Router, private activatedRoute: ActivatedRoute, private warehouseService: WarehouseService) { }

  ngOnInit(): void {
    this.warehouse$ = this.activatedRoute.paramMap.pipe(
      map(params => params.get('id')!),
      tap(id => this.id = id),
      switchMap(id => this.warehouseService.getWarehouse(id).pipe(
        catchError(() => of(undefined))
      )),
      shareReplay({ bufferSize: 1, refCount: true })
    );
  }
  onEdit() {
    this.router.navigateByUrl(`/warehouses/form/${this.id}`)
  }
  onBack() {
    this.router.navigateByUrl('/warehouses')
  }


}
