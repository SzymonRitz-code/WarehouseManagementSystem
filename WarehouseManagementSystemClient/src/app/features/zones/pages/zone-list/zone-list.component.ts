import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ComponentCardComponent } from "../../../../shared/components/common/component-card/component-card.component";
import { TableComponent } from "../../../../shared/components/table/table.component";
import { ZoneService } from '../../../services/zone-service';
import { Zone  } from '../../model/zone';

@Component({
  selector: 'app-zones',
  standalone: true,
  imports: [ComponentCardComponent, TableComponent],
  templateUrl: './zone-list.component.html'
})
export class ZoneListComponent implements OnInit {

  zones: Zone[] = [];

  columns = [
    { key: 'code', label: 'Code', sortable: true },
    { key: 'name', label: 'Name', sortable: true },
    { key: 'temperatureType', label: 'Temperature Type', sortable: true },
    { key: 'isPickingZone', label: 'isPickingZone', sortable: true },
    { key: 'warehouseName', label: 'Warehouse Name', sortable: true },
    { key: 'stockQty', label: 'StockQty', sortable: true },
    { key: 'createdAt', label: 'Created At', sortable: true }
  ];

  constructor(private zoneService: ZoneService, private router: Router) {

  }

  ngOnInit(): void {
    this.zones = this.zoneService.zones;
  }


  goToForm() {
    this.router.navigate(['/zones/form']);
  }
}
