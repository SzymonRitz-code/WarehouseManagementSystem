import { Component, OnInit } from '@angular/core';
import { LabelComponent } from "../../../../shared/components/form/label/label.component";
import { ActivatedRoute, Router } from '@angular/router';
import { Product } from '../../model/product';
import { ComponentCardComponent } from '../../../../shared/components/common/component-card/component-card.component';
import { PageBreadcrumbComponent } from '../../../../shared/components/common/page-breadcrumb/page-breadcrumb.component';
import { ProductService } from '../../../services/product-service';
import { InputDetailComponent } from "../../../../shared/components/form/input/input-detail.component";
import { DetailActionsComponent } from "../../../../shared/components/form/detail-actions/detail-actions.component";

@Component({
  selector: 'app-product-detail',
  imports: [LabelComponent, ComponentCardComponent, PageBreadcrumbComponent, InputDetailComponent, DetailActionsComponent],
  templateUrl: './product-detail.component.html'
})
export class ProductDetailComponent implements OnInit {

  constructor(private activatedRoute: ActivatedRoute, private router: Router, private productService: ProductService) { }
  id!: string;
  product!: Product | undefined;
  ngOnInit(): void {
    this.id = this.activatedRoute.snapshot.paramMap.get('id')!;
    this.product = this.productService.getProduct(this.id) as Product;
    console.log(this.product)
  }
  onBack() {
    this.router.navigateByUrl('/products');
  }
  onEdit() {
    this.router.navigateByUrl(`/products/form/${this.id}`);
  }
}
