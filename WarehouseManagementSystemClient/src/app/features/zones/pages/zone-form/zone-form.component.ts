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
import { ZoneService } from '../../services/zone-service';
import { WarehouseService } from '../../../warehouses/services/warehouse-service';
import { CommonModule } from '@angular/common';
import { InputSelectComponent } from "../../../../shared/components/form/input/input-select/input-select.component";
import { setServerErrors } from '../../../../core/helpsers/vaildation-helper.helper';
import { CheckboxComponent } from "../../../../shared/components/form/input/checkbox.component";
import { TemperatureType } from '../../../../core/enums/temperatureType';

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
    InputSelectComponent,
    CheckboxComponent
  ],
  templateUrl: './zone-form.component.html'
})
export class ZoneFormComponent implements OnInit {


  id: string | null = '';
  zone!: Zone | CreateZone;
  zoneForm!: FormGroup;
  options!: any[];
  warehouseOptions!: any[];
  temperatureTypeOptions!: any[];
  constructor(
    private fb: FormBuilder,
    private router: Router,
    private activatedRoute: ActivatedRoute,
    private zoneService: ZoneService,
    private warehouseService: WarehouseService) { }


  ngOnInit(): void {
    this.id = this.activatedRoute.snapshot.paramMap.get('id')!;
    this.zoneForm = this.fb.group({
      id: [this.id || null],
      code: ['', Validators.required],
      name: ['', Validators.required],
      temperatureType: ['', Validators.required],
      isPickingZone: ['', Validators.required],
      warehouseId: ['', Validators.required]
    });
    this.warehouseService.getWarehouses().subscribe({
      next: (responce) => {
        this.options = responce.map(w => ({ value: w.id, label: w.name }));
      }
    })

    if (this.id) {
      this.zoneService.getZone(this.id).subscribe({
        next: (responce: Zone) => {
          this.zone = responce;
          let warehouse: Warehouse;

          this.warehouseService.getWarehouse(this.zone.warehouseId).subscribe({
            next: (responce) => {
              warehouse = responce
              this.zoneForm.patchValue({
                id: (this.zone as Zone).id,
                code: this.zone.code,
                name: this.zone.name,
                temperatureType: this.zone.temperatureType,
                isPickingZone: this.zone.isPickingZone,
                warehouseName: warehouse.name,
                warehouseId: this.zone.warehouseId,
                createdAt: (this.zone as Zone).createdAt
              })
            }
          });
        }
      });


    }
    this.warehouseService.getWarehouses().subscribe({
      next: (result) => {
        this.warehouseOptions = result.map(w => ({ value: w.id, label: w.name }))
      }
    });
    this.temperatureTypeOptions = Object.values(TemperatureType).map(t => ({ value: t, label: t }))
  }

  onSave() {
    if (this.zoneForm.invalid) return;

    const zone = this.zoneForm.getRawValue();

    const request$ = this.id
      ? this.zoneService.updateZone(zone)
      : this.zoneService.addZone(zone);

    request$.subscribe({
      next: (responce: Zone) => {
        const id = responce?.id ?? this.id; // Użyj ID z odpowiedzi lub istniejącego ID
        this.router.navigateByUrl(`/zones/detail/${id}`);
      },
      error: (err) => {
        console.error(err);
        setServerErrors(err, this.zoneForm);
      }
    })

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
