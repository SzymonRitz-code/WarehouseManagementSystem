import { Component, OnInit } from '@angular/core';
import { PageBreadcrumbComponent } from "../../../../shared/components/common/page-breadcrumb/page-breadcrumb.component";
import { ComponentCardComponent } from "../../../../shared/components/common/component-card/component-card.component";
import { LabelComponent } from "../../../../shared/components/form/label/label.component";
import { InputFieldComponent } from "../../../../shared/components/form/input/input-field.component";
import { FormActionsComponent } from "../../../../shared/components/form/form-actions/form-actions.component";
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Zone } from '../../model/zone';
import { CreateZone } from '../../model/create-zone';
import { ActivatedRoute, Router } from '@angular/router';
import { Warehouse } from '../../../warehouses/model/warehouse';
import { ZoneService } from '../../../services/zone-service';
import { WarehouseService } from '../../../services/warehouse-service';
import { CommonModule } from '@angular/common';
import { InputSelectComponent } from "../../../../shared/components/form/input/input-select/input-select.component";

@Component({
  selector: 'app-zone-form',
  standalone: true,
  imports: [
    PageBreadcrumbComponent,
    ComponentCardComponent,
    LabelComponent,
    InputFieldComponent,
    FormActionsComponent,
    CommonModule,
    ReactiveFormsModule,
    InputSelectComponent
  ],
  templateUrl: './zone-form.component.html'
})
export class ZoneFormComponent implements OnInit {


  id: string | null = '';
  zone!: Zone | CreateZone;
  zoneForm!: FormGroup;
  options!: any[];
  selectedValue = '';
  constructor(
    private fb: FormBuilder,
    private router: Router,
    private activatedRoute: ActivatedRoute,
    private zoneService: ZoneService,
    private warehouseService: WarehouseService) { }


  ngOnInit(): void {
    this.id = this.activatedRoute.snapshot.paramMap.get('id')!;
    this.zoneForm = this.fb.nonNullable.group({
      code: ['', Validators.required],
      name: ['', Validators.required],
      temperatureType: ['', Validators.required],
      isPickingZone: ['', Validators.required],
      warehouseId: ['', Validators.required]
    });
    this.options = this.warehouseService.warehouses.map(w => ({ value: w.id, label: w.warehouseName }));
    if (this.id) {
      this.zone = this.zoneService.getZone(this.id);
      let warehouse: Warehouse = this.warehouseService.getWarehouse(this.zone.warehouseId)!;
      this.zoneForm.patchValue({
        code: this.zone.code,
        name: this.zone.name,
        temperatureType: this.zone.temperatureType,
        isPickingZone: this.zone.isPickingZone,
        warehouseName: warehouse.warehouseName,
        warehouseId: this.zone.warehouseId,
      })
    }
  }

  onSave() {
    let zoneToAdd = this.zoneForm.value
    let zone = this.zoneService.addZone(zoneToAdd);
    console.log(`Zone aded: ${zone}`);

    this.router.navigateByUrl(`/zones/detail/${zone.id}`);
  }

  onBack() {
    this.router.navigateByUrl('/zones');
  }
  handleSelectChange(value: string) {
    this.zoneForm.patchValue({
      warehouseId: value
    })
  }
}
