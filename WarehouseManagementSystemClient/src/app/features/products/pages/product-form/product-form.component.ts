import { Component, Input, OnInit } from '@angular/core';
import { PageBreadcrumbComponent } from "../../../../shared/components/common/page-breadcrumb/page-breadcrumb.component";
import { ComponentCardComponent } from "../../../../shared/components/common/component-card/component-card.component";
import { LabelComponent } from "../../../../shared/components/form/label/label.component";
import { InputFieldComponent } from "../../../../shared/components/form/input/input-field.component";
import { FormActionsComponent } from "../../../../shared/components/form/form-actions/form-actions.component";
import { ActivatedRoute, isActive, Router } from '@angular/router';
import { Product } from '../../model/product';
import { CreateProduct } from '../../model/create-product';
import { FormArray, FormGroup, ReactiveFormsModule, Validators, FormBuilder } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ProductService } from '../../services/product-service';
import { InputSelectComponent } from "../../../../shared/components/form/input/input-select/input-select.component";
import { UnitOfMeasure } from '../../../../core/enums/unitOfMeasure';
import { CheckboxComponent } from "../../../../shared/components/form/input/checkbox.component";
import { TextAreaComponent } from '../../../../shared/components/form/input/text-area.component';
import { setServerErrors } from '../../../../core/helpers/validation-helper.helper';
import { ValidationSummaryComponent } from '../../../../shared/components/form/validation-summary/validation-summary.component';
import { forkJoin, of, switchMap, take } from 'rxjs';
import { ProductBatchService } from '../../services/product-batch-service';
import { BatchList } from '../../model/product-batch';

@Component({
  selector: 'app-product-form',
  standalone: true,
  imports: [
    PageBreadcrumbComponent,
    ComponentCardComponent,
    LabelComponent,
    InputFieldComponent,
    FormActionsComponent,
    ReactiveFormsModule,
    CommonModule,
    InputSelectComponent,
    CheckboxComponent,
    TextAreaComponent,
    ValidationSummaryComponent
  ],
  templateUrl: './product-form.component.html'
})
export class ProductFormComponent implements OnInit {

  constructor(
    private activatedRoute: ActivatedRoute,
    private router: Router,
    private fb: FormBuilder,
    private productService: ProductService,
    private productBatchService: ProductBatchService) { }

  id: string | null = '';
  product!: Product | CreateProduct;
  productForm!: FormGroup;
  unitOptions!: any[];

  ngOnInit(): void {
    this.id = this.activatedRoute.snapshot.paramMap.get('id');

    this.productForm = this.fb.group({
      id: [this.id || null],
      name: ['', [Validators.required, Validators.maxLength(200)]],
      sku: ['', [Validators.required, Validators.maxLength(50)]],
      description: [''],
      unit: ['', Validators.required],
      requiresBatch: [true],
      isActive: [true],
      weight: [1, [Validators.required, Validators.min(0)]],
      volume: [1, [Validators.required, Validators.min(0)]],
      batches: this.fb.array([])
    })

    if (this.id) {
      this.productService.getProduct(this.id).pipe(take(1)).subscribe({
        next: (res: Product) => {
          this.product = res;
          this.productForm.patchValue({
            id: (this.product as Product).id,
            name: this.product.name,
            sku: this.product.sku,
            description: this.product.description,
            unit: this.product.unit,
            requiresBatch: this.product.requiresBatch,
            isActive: (this.product as Product).isActive,
            weight: this.product.weight,
            volume: this.product.volume
          });
        },
        error: () => this.router.navigateByUrl('/products')
      });

      this.productBatchService.getBatches(this.id).pipe(take(1)).subscribe({
        next: batches => batches.forEach(batch => this.addBatch(batch))
      });

    }
    this.unitOptions = Object.values(UnitOfMeasure).map(d => ({ value: d, label: d }))
  }

  get batches(): FormArray {
    return this.productForm.get('batches') as FormArray;
  }

  addBatch(batch?: Partial<BatchList>): void {
    this.batches.push(this.fb.group({
      id: [batch?.id ?? null],
      batchNumber: [batch?.batchNumber ?? '', [Validators.required, Validators.maxLength(100)]],
      manufacturedDate: [this.toDateInput(batch?.manufacturedDate)],
      expirationDate: [this.toDateInput(batch?.expirationDate)]
    }));
  }

  removeUnsavedBatch(index: number): void {
    if (!this.batches.at(index).get('id')?.value) {
      this.batches.removeAt(index);
    }
  }

  onSave() {
    if (this.productForm.invalid) return;

    const formValue = this.productForm.getRawValue();
    const request$ = this.id
      ? this.productService.updateProduct(this.id, {
        name: formValue.name,
        sku: formValue.sku,
        description: formValue.description,
        unit: formValue.unit,
        requiresBatch: formValue.requiresBatch,
        isActive: formValue.isActive,
        weight: formValue.weight,
        volume: formValue.volume
      })
      : this.productService.addProduct({
        name: formValue.name,
        sku: formValue.sku,
        description: formValue.description,
        unit: formValue.unit,
        requiresBatch: formValue.requiresBatch,
        weight: formValue.weight,
        volume: formValue.volume
      });

    request$.pipe(
      switchMap((response: Product) => {
        const productId = response?.id ?? this.id;
        if (!productId || !formValue.requiresBatch || this.batches.length === 0) {
          return of(response);
        }

        const batchRequests = this.batches.controls.map(control => {
          const batch = control.getRawValue();
          const payload = {
            batchNumber: batch.batchNumber,
            productId,
            manufacturedDate: batch.manufacturedDate ? new Date(batch.manufacturedDate) : null,
            expirationDate: batch.expirationDate ? new Date(batch.expirationDate) : null
          };

          return batch.id
            ? this.productBatchService.updateBatch(productId, batch.id, { ...payload, id: batch.id })
            : this.productBatchService.createBatch(productId, payload);
        });

        return forkJoin(batchRequests).pipe(switchMap(() => of(response)));
      }),
      take(1)
    ).subscribe({
      next: (response: Product) => {
        // Trzeba dodać response.id, bo w przypadku tworzenia produktu id jest generowane po stronie serwera
        // a w przypadku aktualizacji produktu id jest już dostępne w productForm.getRawValue()
        const id = response?.id ?? this.id;
        this.router.navigateByUrl(`/products/detail/${id}`);
      },
      error: (err) => {
        setServerErrors(err, this.productForm);
      }
    });
  }
  onBack() {
    this.router.navigateByUrl('/products');
  }

  private toDateInput(value?: Date): string | null {
    return value ? new Date(value).toISOString().slice(0, 10) : null;
  }


}
