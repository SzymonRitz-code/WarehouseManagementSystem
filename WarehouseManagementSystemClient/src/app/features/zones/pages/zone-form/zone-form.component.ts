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
import { ZoneService } from '../../services/zone-service';
import { WarehouseService } from '../../../warehouses/services/warehouse-service';
import { CommonModule } from '@angular/common';
import { InputSelectComponent } from "../../../../shared/components/form/input/input-select/input-select.component";
import { setServerErrors } from '../../../../core/helpsers/vaildation-helper.helper';
import { CheckboxComponent } from "../../../../shared/components/form/input/checkbox.component";
import { TemperatureType } from '../../../../core/enums/temperatureType';
import { ValidationSummaryComponent } from '../../../../shared/components/form/validation-summary/validation-summary.component';
import { map, take } from 'rxjs';

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
    CheckboxComponent,
    ValidationSummaryComponent
  ],
  templateUrl: './zone-form.component.html'
})
export class ZoneFormComponent implements OnInit {


  id: string | null = '';
  zone!: Zone | CreateZone;
  zoneForm!: FormGroup;
  warehouseOptions: any[] = [];
  temperatureTypeOptions: any[] = [];
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
      code: ['', [Validators.required, Validators.maxLength(30)]],
      name: ['', [Validators.required, Validators.maxLength(100)]],
      temperatureType: ['', [Validators.required, Validators.maxLength(20)]],
      isPickingZone: [false],
      warehouseId: ['', Validators.required]
    });
    this.warehouseService.getWarehouses().pipe(
      take(1),
      map(result => result.map(w => ({ value: w.id, label: w.name })))
    ).subscribe({
      next: (options) => this.warehouseOptions = options
    });

    if (this.id) {
      this.zoneService.getZone(this.id).pipe(take(1)).subscribe({
        next: (responce: Zone) => {
          this.zone = responce;
          this.zoneForm.patchValue({
            id: (this.zone as Zone).id,
            code: this.zone.code,
            name: this.zone.name,
            temperatureType: this.zone.temperatureType,
            isPickingZone: this.zone.isPickingZone,
            warehouseId: this.zone.warehouseId
          });
        }
      });
    }
    this.temperatureTypeOptions = Object.values(TemperatureType).map(t => ({ value: t, label: t }))
  }

  onSave() {
    if (this.zoneForm.invalid) return;

    const formValue = this.zoneForm.getRawValue();
    const request$ = this.id
      ? this.zoneService.updateZone(this.id, {
        code: formValue.code,
        name: formValue.name,
        temperatureType: formValue.temperatureType,
        isPickingZone: formValue.isPickingZone,
        warehouseId: formValue.warehouseId
      })
      : this.zoneService.addZone({
        code: formValue.code,
        name: formValue.name,
        temperatureType: formValue.temperatureType,
        isPickingZone: formValue.isPickingZone,
        warehouseId: formValue.warehouseId
      });

    request$.pipe(take(1)).subscribe({
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
