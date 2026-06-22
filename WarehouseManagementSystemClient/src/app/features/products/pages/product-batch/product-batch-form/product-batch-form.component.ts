import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { Batch } from '../../../model/product-batch';
import { DatePickerComponent } from "../../../../../shared/components/form/date-picker/date-picker.component";
import { LabelComponent } from "../../../../../shared/components/form/label/label.component";
import { PageBreadcrumbComponent } from "../../../../../shared/components/common/page-breadcrumb/page-breadcrumb.component";
import { ComponentCardComponent } from "../../../../../shared/components/common/component-card/component-card.component";
import { FormActionsComponent } from "../../../../../shared/components/form/form-actions/form-actions.component";
import { ProductService } from '../../../services/product-service';
import { ActivatedRoute, Router } from '@angular/router';
import { CreateBatch } from '../../../model/product-create-batch';
import { ProductBatchService } from '../../../services/product-batch-service';
import { setServerErrors } from '../../../../../core/helpsers/vaildation-helper.helper';
import { InputSelectComponent } from '../../../../../shared/components/form/input/input-select/input-select.component';
import { CommonModule } from '@angular/common';
import { InputFieldComponent } from '../../../../../shared/components/form/input/input-field.component';
import { ValidationSummaryComponent } from '../../../../../shared/components/form/validation-summary/validation-summary.component';
import { map, take } from 'rxjs';

@Component({
  selector: 'app-product-batch-form',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    DatePickerComponent,
    LabelComponent,
    PageBreadcrumbComponent,
    ComponentCardComponent,
    InputSelectComponent,
    InputFieldComponent,
    FormActionsComponent,
    ValidationSummaryComponent],
  templateUrl: './product-batch-form.component.html'
})
export class ProductBatchFormComponent implements OnInit {
  batchId!: string;
  productId!: string;
  batchForm!: FormGroup;
  batch!: Batch | CreateBatch;
  productOptions: any[] = [];
  constructor(
    private router: Router,
    private activatedRoute: ActivatedRoute,
    private fb: FormBuilder,
    private productService: ProductService,
    private batchService: ProductBatchService) { }


  ngOnInit(): void {
    this.productId = this.activatedRoute.snapshot.paramMap.get('id')!;
    this.batchId = this.activatedRoute.snapshot.paramMap.get('batchId')!;
    this.productService.getProduct(this.productId).pipe(
      take(1),
      map(product => [{ value: product.id, label: product.name }])
    ).subscribe({
      next: (options) => this.productOptions = options
    });

    this.batchForm = this.fb.group({
      id: [this.batchId || null],
      batchNumber: ['', [Validators.required, Validators.maxLength(50)]],
      productId: ['', [Validators.required]],
      expirationDate: [null],
      manufacturedDate: [null]
    });
    if (this.batchId) {
      this.batchService.getBatch(this.productId, this.batchId).pipe(take(1)).subscribe({
        next: (batch: Batch) => {
          this.batch = batch;
          this.batchForm.patchValue({
            id: (this.batch as Batch).id,
            batchNumber: this.batch.batchNumber,
            productId: this.batch.productId,
            expirationDate: this.batch.expirationDate,
            manufacturedDate: this.batch.manufacturedDate
          });
        }
      });
    }
  }
  onBack() {
    this.router.navigateByUrl(`/products/${this.productId}/batches`);
  }

  onSave() {
    const formValue = this.batchForm.getRawValue();
    const batch: Batch | CreateBatch = this.batchId
      ? {
        id: this.batchId,
        batchNumber: formValue.batchNumber,
        expirationDate: formValue.expirationDate,
        manufacturedDate: formValue.manufacturedDate
      } as Batch
      : {
        batchNumber: formValue.batchNumber,
        productId: formValue.productId,
        expirationDate: formValue.expirationDate,
        manufacturedDate: formValue.manufacturedDate
      };
    const request$ = this.batchId
      ? this.batchService.updateBatch(this.productId, this.batchId, batch)
      : this.batchService.createBatch(this.productId, batch);

    request$.pipe(take(1)).subscribe({
      next: (responce: Batch) => {
        const id = responce?.id ?? this.batchId;
        this.router.navigateByUrl(`/products/${this.productId}/batches/detail/${id}`)
      },
      error: (err) => {
        setServerErrors(err, this.batchForm);
      }
    });
  }
}
