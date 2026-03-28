import { Component, Input, OnInit } from '@angular/core';
import { PageBreadcrumbComponent } from "../../../../shared/components/common/page-breadcrumb/page-breadcrumb.component";
import { ComponentCardComponent } from "../../../../shared/components/common/component-card/component-card.component";
import { LabelComponent } from "../../../../shared/components/form/label/label.component";
import { InputFieldComponent } from "../../../../shared/components/form/input/input-field.component";
import { FormActionsComponent } from "../../../../shared/components/form/form-actions/form-actions.component";
import { ActivatedRoute, isActive, Router } from '@angular/router';
import { Product } from '../../model/product';
import { CreateProduct } from '../../model/create-product';
import { FormGroup, ReactiveFormsModule, Validators, FormBuilder } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ProductService } from '../../services/product-service';
import { InputSelectComponent } from "../../../../shared/components/form/input/input-select/input-select.component";
import { UnitOfMeasure } from '../../../../core/enums/unitOfMeasure';
import { CheckboxComponent } from "../../../../shared/components/form/input/checkbox.component";
import { TextAreaComponent } from '../../../../shared/components/form/input/text-area.component';

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
    TextAreaComponent
  ],
  templateUrl: './product-form.component.html'
})
export class ProductFormComponent implements OnInit {

  constructor(
    private activatedRoute: ActivatedRoute,
    private router: Router,
    private fb: FormBuilder,
    private productService: ProductService) { }

  id: string | null = '';
  product!: Product | CreateProduct;
  productForm!: FormGroup;
  unitOptions!: any[];

  ngOnInit(): void {
    this.id = this.activatedRoute.snapshot.paramMap.get('id');
    this.productForm = this.fb.group({
      id: [''],
      name: ['', [Validators.required, Validators.maxLength(200)]],
      sku: ['', [Validators.required, Validators.maxLength(50)]],
      description: [''],
      unit: ['', Validators.required],
      requiresBatch: [true, Validators.required],
      isActive: [true, Validators.required],
      weight: [1, [Validators.required, Validators.min(1)]],
      volume: [1, [Validators.required, Validators.min(1)]]
    })
    if (this.id) {
      this.productService.getProduct(this.id).subscribe({
        next: (res: Product) => {
          this.product = res;
          this.productForm.patchValue({
            id: (this.product as Product).id,
            name: this.product.name,
            sku: this.product.sku,
            description: this.product.description,
            unit: this.product.unit,
            requiresBatch: this.product.requiresBatch,
            isActive: this.product.isActive,
            weight: this.product.weight,
            volume: this.product.volume
          });
        },
        error: (err) => { console.error(err) }
      })

    }
    this.unitOptions = Object.values(UnitOfMeasure).map(d => ({ value: d, label: d }))
  }

  onSave() {
    if (this.productForm.invalid) return;

    const product = this.productForm.getRawValue();
    console.log(this.id)
    const request$ = this.id
      ? this.productService.updateProduct(product)
      : this.productService.addProduct(product);

    request$.subscribe({
      next: (response: Product) => {
        this.router.navigateByUrl(`/products/detail/${this.id}`);
      },
      error: (err) => {
        console.error(err);
        // tutaj docelowo:
        // this.setServerErrors(err)
      }
    });
  }
  onBack() {
    this.router.navigateByUrl('/products');
  }
}
