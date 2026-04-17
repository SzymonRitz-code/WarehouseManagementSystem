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
    CheckboxComponent
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
      code: ['', Validators.required],
      name: ['', Validators.required],
      country: ['', Validators.required],
      city: ['', Validators.required],
      address: ['', Validators.required],
      isActive: [true, Validators.required]
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
            isActive: (this.warehouse as Warehouse).isActive,
            createdAt: (this.warehouse as Warehouse).createdAt
          })
        },
        error: (err) => { console.error(err) }
      });
    }

  }

  onSave() {
    if (this.warehouseForm.invalid) return;

    const warehouse = this.warehouseForm.getRawValue()
    const request$ = this.id
      ? this.warehouseService.updateWarehouse(warehouse)
      : this.warehouseService.addWarehouse(warehouse);

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
