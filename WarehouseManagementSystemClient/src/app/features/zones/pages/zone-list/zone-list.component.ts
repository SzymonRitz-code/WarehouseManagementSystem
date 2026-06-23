import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ComponentCardComponent } from "../../../../shared/components/common/component-card/component-card.component";
import { TableComponent } from "../../../../shared/components/table/table.component";
import { ZoneService } from '../../services/zone-service';
import { ZoneList } from '../../model/zone';
import { PageBreadcrumbComponent } from "../../../../shared/components/common/page-breadcrumb/page-breadcrumb.component";
import { catchError, finalize, Observable, of, shareReplay, startWith, Subject, switchMap } from 'rxjs';
import { CommonModule } from '@angular/common';
import { ButtonComponent } from "../../../../shared/ui/button/button.component";

@Component({
  selector: 'app-zones',
  standalone: true,
  imports: [CommonModule, PageBreadcrumbComponent, ComponentCardComponent, TableComponent, ButtonComponent],
  templateUrl: './zone-list.component.html'
})
export class ZoneListComponent implements OnInit {

  zones$!: Observable<ZoneList[]>;
  isLoading = false;
  errorMessage = '';
  private readonly reloadZones$ = new Subject<void>();

  columns = [
    { key: 'code', label: 'Code', sortable: true },
    { key: 'name', label: 'Name', sortable: true },
    { key: 'temperatureType', label: 'Temperature Type', sortable: true },
    { key: 'isPickingZone', label: 'is Picking Zone', sortable: true },
    { key: 'warehouseName', label: 'Warehouse Name', sortable: true },
    { key: 'stockQty', label: 'StockQty', sortable: true },
    { key: 'createdAt', label: 'Created At', sortable: true, type: 'date' }
  ];
  zoneActions = [
    { label: 'Edit', action: 'edit' },
    { label: 'Details', action: 'details' },
  ];

  constructor(private zoneService: ZoneService, private router: Router) {

  }

  ngOnInit(): void {
    this.zones$ = this.reloadZones$.pipe(
      startWith(void 0),
      switchMap(() => {
        this.isLoading = true;
        this.errorMessage = '';

        return this.zoneService.getZones().pipe(
          catchError(() => {
            this.errorMessage = 'Zones could not be loaded. Please try again.';
            return of([]);
          }),
          finalize(() => this.isLoading = false)
        );
      }),
      shareReplay({ bufferSize: 1, refCount: true })
    );
  }

  retry(): void {
    this.loadZones();
  }

  private loadZones(): void {
    this.reloadZones$.next();
  }


  goToForm() {
    this.router.navigate(['/zones/form']);
  }

  onZoneAction(event: { row: ZoneList; action: string }) {
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
  onDetails(zone: ZoneList) {
    this.router.navigateByUrl(`/zones/detail/${zone.id}`)
  }
  onEdit(zone: ZoneList) {
    this.router.navigateByUrl(`/zones/form/${zone.id}`)
  }
}
