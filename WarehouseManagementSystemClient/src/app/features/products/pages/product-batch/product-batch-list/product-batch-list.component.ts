import { Component, OnInit} from '@angular/core';
import { PageBreadcrumbComponent } from "../../../../../shared/components/common/page-breadcrumb/page-breadcrumb.component";
import { ComponentCardComponent } from "../../../../../shared/components/common/component-card/component-card.component";
import { TableComponent } from "../../../../../shared/components/table/table.component";
import { BatchList } from '../../../model/product-batch';
import { catchError, finalize, map, Observable, of, shareReplay, startWith, Subject, switchMap, tap } from 'rxjs';
import { CommonModule } from '@angular/common';
import { ProductBatchService } from '../../../services/product-batch-service';
import { ActivatedRoute, Router } from '@angular/router';
import { LabelComponent } from "../../../../../shared/components/form/label/label.component";
import { InputDetailComponent } from "../../../../../shared/components/form/input/input-detail.component";
import { Product } from '../../../model/product';
import { ProductService } from '../../../services/product-service';
import { TextAreaComponent } from "../../../../../shared/components/form/input/text-area.component";
import { ButtonComponent } from "../../../../../shared/ui/button/button.component";

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
    TextAreaComponent,
    ButtonComponent],
  templateUrl: './product-batch-list.component.html'
})
export class ProductBatchListComponent implements OnInit {
  id!: string;
  batches$!: Observable<BatchList[]>;
  product$!: Observable<Product | undefined>;
  isLoading = false;
  errorMessage = '';
  productId!: string;
  private readonly reloadBatches$ = new Subject<void>();

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
    const productId$ = this.activatedRoute.paramMap.pipe(
      map(params => params.get('id')!),
      tap(id => {
        this.id = id;
        this.productId = id;
      }),
      shareReplay({ bufferSize: 1, refCount: true })
    );

    this.product$ = productId$.pipe(
      switchMap(id => this.productService.getProduct(id).pipe(
        catchError(() => of(undefined))
      )),
      shareReplay({ bufferSize: 1, refCount: true })
    );

    this.batches$ = this.reloadBatches$.pipe(
      startWith(void 0),
      switchMap(() => productId$),
      switchMap(productId => {
        this.isLoading = true;
        this.errorMessage = '';

        return this.productBatchService.getBatches(productId).pipe(
          catchError(() => {
            this.errorMessage = 'Product batches could not be loaded. Please try again.';
            return of([]);
          }),
          finalize(() => this.isLoading = false)
        );
      }),
      shareReplay({ bufferSize: 1, refCount: true })
    );
  }

  retry(): void {
    this.loadBatches();
  }

  private loadBatches(): void {
    this.reloadBatches$.next();
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
