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
import { catchError, forkJoin, map, Observable, of, shareReplay, switchMap, tap } from 'rxjs';
import { TableComponent } from '../../../../shared/components/table/table.component';
import { Stock } from '../../../stocks/model/stock';

interface ProductDetailViewModel {
  product: Product;
  stocks: Stock[];
}


@Component({
  selector: 'app-product-detail',
  imports: [CommonModule, LabelComponent, ComponentCardComponent, PageBreadcrumbComponent, InputDetailComponent, DetailActionsComponent, TextAreaComponent, TableComponent],
  templateUrl: './product-detail.component.html'
})
export class ProductDetailComponent implements OnInit {

  constructor(private activatedRoute: ActivatedRoute, private router: Router, private productService: ProductService) { }
  id!: string;
  vm$!: Observable<ProductDetailViewModel | undefined>;
  readonly stockColumns = [
    { key: 'warehouseName', label: 'Warehouse', sortable: true },
    { key: 'zoneName', label: 'Zone', sortable: true },
    { key: 'productBatchNumber', label: 'Batch', sortable: true },
    { key: 'quantityAvailable', label: 'Available Qty', sortable: true },
    { key: 'quantityReserved', label: 'Reserved Qty', sortable: true },
    { key: 'quantityTotal', label: 'Total Qty', sortable: true },
    { key: 'unit', label: 'Unit', sortable: true },
    { key: 'lastUpdated', label: 'Last Updated', sortable: true, type: 'date' }
  ];

  ngOnInit(): void {
    this.vm$ = this.activatedRoute.paramMap.pipe(
      map(params => params.get('id')!),
      tap(id => this.id = id),
      switchMap(id =>
        forkJoin({
          product: this.productService.getProduct(id),
          stocks: this.productService.getProductStocks(id)
        }).pipe(
          catchError(() => of(undefined))
        )
      ),
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
