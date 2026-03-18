import { Component, Input, OnInit } from '@angular/core';
import { PageBreadcrumbComponent } from "../../../../shared/components/common/page-breadcrumb/page-breadcrumb.component";
import { ComponentCardComponent } from "../../../../shared/components/common/component-card/component-card.component";
import { LabelComponent } from "../../../../shared/components/form/label/label.component";
import { InputFieldComponent } from "../../../../shared/components/form/input/input-field.component";
import { FormActionsComponent } from "../../../../shared/components/form/form-actions/form-actions.component";
import { ActivatedRoute, Router } from '@angular/router';
import { Product } from '../../model/product';
import { CreateProduct } from '../../model/create-product';
import { FormGroup, ReactiveFormsModule, Validators, FormBuilder } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ProductService } from '../../../services/product-service';

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
    CommonModule],
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

  ngOnInit(): void {
    this.id = this.activatedRoute.snapshot.paramMap.get('id');
    this.productForm = this.fb.nonNullable.group({
      name: ['', Validators.required],
      sku: ['', Validators.required]
    })
    if (this.id) {
      this.product = this.productService.getProduct(this.id)!;
      this.productForm.patchValue({
        name: this.product.name,
        sku: this.product.sku
      });
    }
  }

  onSave() {
    this.product = this.productForm.value
    this.product = this.productService.addProduct(this.product) as Product;
    this.router.navigateByUrl(`/products/detail/${(this.product as Product).id}`);
  }
  onBack() {
    this.router.navigateByUrl('/products');
  }
}
