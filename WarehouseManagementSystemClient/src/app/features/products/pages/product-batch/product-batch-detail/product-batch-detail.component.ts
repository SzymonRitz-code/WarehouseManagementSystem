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

@Component({
  selector: 'app-product-batch-detail',
  standalone: true,
  imports: [PageBreadcrumbComponent, ComponentCardComponent, LabelComponent, InputDetailComponent, DetailActionsComponent],
  templateUrl: './product-batch-detail.component.html'
})
export class ProductBatchDetailComponent implements OnInit {
  batch!: Batch;
  productId!: string;
  batchId!: string;
  constructor(
    private activatedRoute: ActivatedRoute, 
    private router: Router, 
    private batchService: ProductBatchService, 
    private productService: ProductService) { }


  ngOnInit(): void {
    this.productId = this.activatedRoute.snapshot.paramMap.get('id')!;
    this.batchId = this.activatedRoute.snapshot.paramMap.get('batchId')!;
    this.batchService.getBatch(this.productId, this.batchId).subscribe({
      next: (batch) => {
        this.batch = batch;
        this.productService.getProduct(this.productId).subscribe({
          next: (product) => this.batch.productName = product.name,
          error: (err) => console.error("Error fetching product details", err)
        });
      },
      error: (err) => console.error("Error fetching batch details", err)
    });

  }

  onEdit() {
    this.router.navigateByUrl(`/products/${this.productId}/batches/form/${this.batchId}`);
  }
  onBack() {
    this.router.navigateByUrl(`/products/${this.productId}/batches`);
  }

}
