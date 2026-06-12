import { Component, OnInit } from '@angular/core';
import { PageBreadcrumbComponent } from "../../../../shared/components/common/page-breadcrumb/page-breadcrumb.component";
import { ComponentCardComponent } from "../../../../shared/components/common/component-card/component-card.component";
import { LabelComponent } from "../../../../shared/components/form/label/label.component";
import { InputFieldComponent } from "../../../../shared/components/form/input/input-field.component";
import { FormActionsComponent } from "../../../../shared/components/form/form-actions/form-actions.component";
import { Warehouse } from '../../model/warehouse';
import { CreateWarehouse } from '../../model/create-warehouse';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { WarehouseService } from '../../services/warehouse-service';
import { ActivatedRoute, isActive, Router } from '@angular/router';
import { CheckboxComponent } from "../../../../shared/components/form/input/checkbox.component";
import { setServerErrors } from '../../../../core/helpsers/vaildation-helper.helper';
import { ValidationSummaryComponent } from '../../../../shared/components/form/validation-summary/validation-summary.component';

@Component({
  selector: 'app-warehouse-form',
  standalone: true,
  imports: [
    PageBreadcrumbComponent,
    ComponentCardComponent,
    LabelComponent,
    InputFieldComponent,
    FormActionsComponent,
    CommonModule,
    ReactiveFormsModule,
    CheckboxComponent,
    ValidationSummaryComponent
  ],
  templateUrl: './warehouse-form.component.html'
})
export class WarehouseFormComponent implements OnInit {


  id!: string | null;
  warehouse!: Warehouse | CreateWarehouse;
  warehouseForm!: FormGroup;

  constructor(
    private fb: FormBuilder,
    private warehouseService: WarehouseService,
    private router: Router,
    private activatedRoute: ActivatedRoute) { }

  ngOnInit(): void {
    this.id = this.activatedRoute.snapshot.paramMap.get('id')!;

    this.warehouseForm = this.fb.nonNullable.group({
      id: [this.id || null],
      code: ['', [Validators.required, Validators.maxLength(30)]],
      name: ['', [Validators.required, Validators.maxLength(200)]],
      country: ['', [Validators.required, Validators.maxLength(100)]],
      city: ['', [Validators.required, Validators.maxLength(100)]],
      address: ['', [Validators.required, Validators.maxLength(200)]],
      isActive: [true]
    });
    if (this.id) {
      this.warehouseService.getWarehouse(this.id).subscribe({
        next: (responce) => {
          this.warehouse = responce;
          this.warehouseForm.patchValue({
            id: (this.warehouse as Warehouse).id,
            code: this.warehouse.code,
            name: this.warehouse.name,
            country: this.warehouse.country,
            city: this.warehouse.city,
            address: this.warehouse.address,
            isActive: (this.warehouse as Warehouse).isActive
          })
        },
        error: (err) => { console.error(err) }
      })
    }

  }

  onSave() {
    if (this.warehouseForm.invalid) return;

    const formValue = this.warehouseForm.getRawValue()
    const request$ = this.id
      ? this.warehouseService.updateWarehouse(this.id, {
        code: formValue.code,
        name: formValue.name,
        country: formValue.country,
        city: formValue.city,
        address: formValue.address,
        isActive: formValue.isActive
      })
      : this.warehouseService.addWarehouse({
        code: formValue.code,
        name: formValue.name,
        country: formValue.country,
        city: formValue.city,
        address: formValue.address
      });

    request$.subscribe({
      next: (responce: Warehouse) => {
        const id = responce?.id ?? this.id;
        this.router.navigateByUrl(`/warehouses/detail/${id}`);
      },
      error: (err) => {
        console.error(err);
        setServerErrors(err, this.warehouseForm);
      }
    }) 

  }
  onBack() {
    this.router.navigateByUrl('/warehouses');
  }
}
