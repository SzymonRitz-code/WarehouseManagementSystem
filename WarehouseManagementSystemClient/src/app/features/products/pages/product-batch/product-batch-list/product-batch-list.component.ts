import { Component, OnInit} from '@angular/core';
import { PageBreadcrumbComponent } from "../../../../../shared/components/common/page-breadcrumb/page-breadcrumb.component";
import { ComponentCardComponent } from "../../../../../shared/components/common/component-card/component-card.component";
import { TableComponent } from "../../../../../shared/components/table/table.component";
import { BatchList } from '../../../model/product-batch';
import { Observable } from 'rxjs';
import { CommonModule } from '@angular/common';
import { ProductBatchService } from '../../../services/product-batch-service';
import { ActivatedRoute, Router } from '@angular/router';
import { LabelComponent } from "../../../../../shared/components/form/label/label.component";
import { InputDetailComponent } from "../../../../../shared/components/form/input/input-detail.component";
import { Product } from '../../../model/product';
import { ProductService } from '../../../services/product-service';
import { TextAreaComponent } from "../../../../../shared/components/form/input/text-area.component";

@Component({
  selector: 'app-product-batch-list',
  standalone: true,
  imports: [
    CommonModule,
    PageBreadcrumbComponent,
    ComponentCardComponent,
    TableComponent,
    LabelComponent,
    InputDetailComponent,
    TextAreaComponent],
  templateUrl: './product-batch-list.component.html'
})
export class ProductBatchListComponent implements OnInit {
  id!: string;
  batches$!: Observable<BatchList[]>;
  product!: Product | undefined;
  productId!: string;

  columns = [
    { key: 'batchNumber', label: 'Batch Number', sortable: true },
    { key: 'productName', label: 'Product Name', sortable: true },
    { key: 'expirationDate', label: 'Expiration Date', sortable: true, type: 'date' },
    { key: 'manufacturedDate', label: 'Manufactured Date', sortable: true, type: 'date' },
    { key: 'quantity', label: 'Quantity', sortable: true },
    { key: 'availableQty', label: 'Available Quantity', sortable: true },
    { key: 'reservedQty', label: 'Reserved Quantity', sortable: true },
    { key: 'createdAt', label: 'Created At', sortable: true, type: 'date' }
  ];

  batchActions = [
    { label: 'Edit', action: 'edit' },
    { label: 'Details', action: 'details' },
  ];

  constructor(
    private router: Router,
    private activatedRoute: ActivatedRoute,
    private productBatchService: ProductBatchService,
    private productService: ProductService
  ) { }


  ngOnInit(): void {
    this.id = this.activatedRoute.snapshot.paramMap.get('id')!;
    this.productService.getProduct(this.id).subscribe({
      next: (product) => this.product = product
    }).unsubscribe();
    this.batches$ = this.productBatchService.getBatches(this.id);
  }
  onBatchAction($event: { row: BatchList; action: string; }) {
    const { row, action } = $event;
    switch (action) {
      case 'edit': this.onEdit(row); break;
      case 'details': this.onDetails(row); break;
    }

  }
  goToForm() {
    this.router.navigateByUrl(`/products/${this.id}/batches/form`);
  }
  onDetails(row: BatchList) {
    this.router.navigateByUrl(`/products/${this.id}/batches/detail/${row.id}`);
  }
  onEdit(row: BatchList) {
    this.router.navigateByUrl(`/products/${this.id}/batches/form/${row.id}`);
  }

}
