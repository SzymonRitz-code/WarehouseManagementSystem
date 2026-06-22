import { Component, OnInit } from '@angular/core';
import { PageBreadcrumbComponent } from "../../../../../shared/components/common/page-breadcrumb/page-breadcrumb.component";
import { ComponentCardComponent } from "../../../../../shared/components/common/component-card/component-card.component";
import { LabelComponent } from "../../../../../shared/components/form/label/label.component";
import { InputDetailComponent } from "../../../../../shared/components/form/input/input-detail.component";
import { DetailActionsComponent } from "../../../../../shared/components/form/detail-actions/detail-actions.component";
import { Batch } from '../../../model/product-batch';
import { ActivatedRoute, Router } from '@angular/router';
import { ProductBatchService } from '../../../services/product-batch-service';
import { ProductService } from '../../../services/product-service';
import { CommonModule } from '@angular/common';
import { catchError, forkJoin, map, Observable, of, shareReplay, switchMap, tap } from 'rxjs';

@Component({
  selector: 'app-product-batch-detail',
  standalone: true,
  imports: [CommonModule, PageBreadcrumbComponent, ComponentCardComponent, LabelComponent, InputDetailComponent, DetailActionsComponent],
  templateUrl: './product-batch-detail.component.html'
})
export class ProductBatchDetailComponent implements OnInit {
  batch$!: Observable<Batch | undefined>;
  productId!: string;
  batchId!: string;
  constructor(
    private activatedRoute: ActivatedRoute, 
    private router: Router, 
    private batchService: ProductBatchService, 
    private productService: ProductService) { }


  ngOnInit(): void {
    this.batch$ = this.activatedRoute.paramMap.pipe(
      map(params => ({
        productId: params.get('id')!,
        batchId: params.get('batchId')!
      })),
      tap(({ productId, batchId }) => {
        this.productId = productId;
        this.batchId = batchId;
      }),
      switchMap(({ productId, batchId }) => forkJoin({
        batch: this.batchService.getBatch(productId, batchId),
        product: this.productService.getProduct(productId).pipe(catchError(() => of(null)))
      })),
      map(({ batch, product }) => ({
        ...batch,
        productName: product?.name ?? batch.productName
      })),
      catchError(() => of(undefined)),
      shareReplay({ bufferSize: 1, refCount: true })
    );

  }

  onEdit() {
    this.router.navigateByUrl(`/products/${this.productId}/batches/form/${this.batchId}`);
  }
  onBack() {
    this.router.navigateByUrl(`/products/${this.productId}/batches`);
  }

}
