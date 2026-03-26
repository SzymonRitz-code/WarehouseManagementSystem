import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ComponentCardComponent } from "../../../../shared/components/common/component-card/component-card.component";
import { TableComponent } from "../../../../shared/components/table/table.component";
import { ZoneService } from '../../../services/zone-service';
import { Zone } from '../../model/zone';
import { PageBreadcrumbComponent } from "../../../../shared/components/common/page-breadcrumb/page-breadcrumb.component";

@Component({
  selector: 'app-zones',
  standalone: true,
  imports: [PageBreadcrumbComponent, ComponentCardComponent, TableComponent],
  templateUrl: './zone-list.component.html'
})
export class ZoneListComponent implements OnInit {

  zones: Zone[] = [];

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
    this.zones = this.zoneService.zones;
  }


  goToForm() {
    this.router.navigate(['/zones/form']);
  }

  onZoneAction(event: { row: Zone; action: string }) {
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
  onDetails(zone: Zone) {
    this.router.navigateByUrl(`/zones/detail/${zone.id}`)
  }
  onEdit(zone: Zone) {
    this.router.navigateByUrl(`/zones/form/${zone.id}`)
  }
}
