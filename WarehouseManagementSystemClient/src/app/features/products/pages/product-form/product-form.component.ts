import { Component, Input, OnInit } from '@angular/core';
import { PageBreadcrumbComponent } from "../../../../shared/components/common/page-breadcrumb/page-breadcrumb.component";
import { ComponentCardComponent } from "../../../../shared/components/common/component-card/component-card.component";
import { LabelComponent } from "../../../../shared/components/form/label/label.component";
import { InputFieldComponent } from "../../../../shared/components/form/input/input-field.component";
import { FormActionsComponent } from "../../../../shared/components/form/form-actions/form-actions.component";
import { Router } from '@angular/router';
import { Product } from '../../model/product';
import { CreateProduct } from '../../model/create-product';
import { FormControl, FormGroup, ReactiveFormsModule, Validators, FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';

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
    FormsModule,
    CommonModule],
  templateUrl: './product-form.component.html'
})
export class ProductFormComponent implements OnInit {
  @Input() id: string | undefined;
  product!: Product | CreateProduct;
  productForm!: FormGroup;

  ngOnInit(): void {
    this.productForm = new FormGroup({
      name: new FormControl('', Validators.required),
      sku: new FormControl('', Validators.required)
    })
    if (this.id === undefined) {
      this.product = {
        name: '',
        sku: ''
      } as CreateProduct
    } else {
      this.product = {
        id: 'id',
        name: 'Name',
        sku: 'sku'
      } as Product
    }
  }


  constructor(private router: Router) { }
  onSave() {
    console.log(`Dodano produkt: ${this.product?.name} ${this.product?.sku}`)
    this.router.navigateByUrl('/products/detail');
  }
  onBack() {
    this.router.navigateByUrl('/products');
  }
}
