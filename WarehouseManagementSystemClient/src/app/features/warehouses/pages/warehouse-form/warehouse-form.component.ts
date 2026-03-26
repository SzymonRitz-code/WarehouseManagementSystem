import { Component, OnInit } from '@angular/core';
import { PageBreadcrumbComponent } from "../../../../shared/components/common/page-breadcrumb/page-breadcrumb.component";
import { ComponentCardComponent } from "../../../../shared/components/common/component-card/component-card.component";
import { LabelComponent } from "../../../../shared/components/form/label/label.component";
import { InputFieldComponent } from "../../../../shared/components/form/input/input-field.component";
import { FormActionsComponent } from "../../../../shared/components/form/form-actions/form-actions.component";
import { Warehouse } from '../../model/warehouse';
import { CreateWarehouse } from '../../model/create-warehouse';
import { FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { WarehouseService } from '../../../services/warehouse-service';
import { ActivatedRoute, Router } from '@angular/router';
import { RadioComponent } from "../../../../shared/components/form/input/radio.component";
import { CheckboxComponent } from "../../../../shared/components/form/input/checkbox.component";

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

  id: string | null = '';
  warehouse!: Warehouse | CreateWarehouse;
  warehouseForm!: FormGroup;

  constructor(
    private fb: FormBuilder,
    private warehouseService:
      WarehouseService,
    private router: Router,
    private activatedRoute: ActivatedRoute) { }

  ngOnInit(): void {
    this.id = this.activatedRoute.snapshot.paramMap.get('id')!;

    this.warehouseForm = this.fb.nonNullable.group({
      id:[''],
      code: ['', Validators.required],
      warehouseName: ['', Validators.required],
      country: ['', Validators.required],
      addres: ['', Validators.required],
      status: ['', Validators.required]
    });
    if (this.id) {
      this.warehouse = this.warehouseService.getWarehouse(this.id)!;
      this.warehouseForm.patchValue({
        id: (this.warehouse as Warehouse).id,
        code: this.warehouse.code,
        warehouseName: this.warehouse.warehouseName,
        country: this.warehouse.country,
        addres: this.warehouse.addres,
        status: (this.warehouse as Warehouse).status
      })
    }

  }
  onSave() {
    this.warehouse = this.warehouseForm.value
    console.log(this.warehouse)
    if(this.id === null){
    this.warehouse = this.warehouseService.addWarehouse(this.warehouse) as Warehouse;
    }
    this.router.navigateByUrl(`/warehouses/detail/${(this.warehouse as Warehouse).id}`);
  }
  onBack() {
    this.router.navigateByUrl('/warehouses');
  }
}
