import { Component, OnInit } from '@angular/core';
import { LabelComponent } from "../../../../shared/components/form/label/label.component";
import { ActivatedRoute, Router } from '@angular/router';
import { Product } from '../../model/product';
import { ProductService } from '../../services/product-service';
import { ComponentCardComponent } from '../../../../shared/components/common/component-card/component-card.component';
import { PageBreadcrumbComponent } from '../../../../shared/components/common/page-breadcrumb/page-breadcrumb.component';
import { InputDetailComponent } from "../../../../shared/components/form/input/input-detail.component";
import { DetailActionsComponent } from "../../../../shared/components/form/detail-actions/detail-actions.component";
import { TextAreaComponent } from "../../../../shared/components/form/input/text-area.component";
import { CommonModule } from '@angular/common';
import { catchError, map, Observable, of, shareReplay, switchMap, tap } from 'rxjs';


@Component({
  selector: 'app-product-detail',
  imports: [CommonModule, LabelComponent, ComponentCardComponent, PageBreadcrumbComponent, InputDetailComponent, DetailActionsComponent, TextAreaComponent],
  templateUrl: './product-detail.component.html'
})
export class ProductDetailComponent implements OnInit {

  constructor(private activatedRoute: ActivatedRoute, private router: Router, private productService: ProductService) { }
  id!: string;
  product$!: Observable<Product | undefined>;

  ngOnInit(): void {
    this.product$ = this.activatedRoute.paramMap.pipe(
      map(params => params.get('id')!),
      tap(id => this.id = id),
      switchMap(id => this.productService.getProduct(id).pipe(
        catchError(() => of(undefined))
      )),
      shareReplay({ bufferSize: 1, refCount: true })
    );
  }
  onBack() {
    this.router.navigateByUrl('/products');
  }
  onEdit() {
    this.router.navigateByUrl(`/products/form/${this.id}`);
  }
}
