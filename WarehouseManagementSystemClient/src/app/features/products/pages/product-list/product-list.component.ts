import { Component, OnInit } from '@angular/core';
import { ComponentCardComponent } from "../../../../shared/components/common/component-card/component-card.component";
import { TableComponent } from "../../../../shared/components/table/table.component";
import { Router } from '@angular/router';
import { ProductList } from '../../model/product';
import { ProductService } from '../../services/product-service';
import { PageBreadcrumbComponent } from "../../../../shared/components/common/page-breadcrumb/page-breadcrumb.component";
import { catchError, finalize, Observable, of } from 'rxjs';
import { CommonModule } from '@angular/common';


@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [CommonModule, PageBreadcrumbComponent, ComponentCardComponent, TableComponent,],
  templateUrl: './product-list.component.html'
})
export class ProductListComponent implements OnInit {

  products$: Observable<ProductList[]> = of([]);
  isLoading = false;
  errorMessage = '';
  columns = [
    { key: 'sku', label: 'SKU', sortable: true },
    { key: 'name', label: 'Name', sortable: true },
    { key: 'unit', label: 'Unit', sortable: true },
    { key: 'requiresBatch', label: 'Requires Batch', sortable: true, type: "boolean" },
    { key: 'weight', label: 'Weight', sortable: true },
    { key: 'volume', label: 'Volume', sortable: true },
    { key: 'isActive', label: 'Is Active', sortable: true, type: "boolean" }
  ];
  productActions = [
    { label: 'Edit', action: 'edit' },
    { label: 'Details', action: 'details' },
    { label: 'Batches', action: 'manageBatches', visible: (row: ProductList) => row.requiresBatch === true },
  ];

  constructor(private router: Router, private productService: ProductService) { }


  ngOnInit(): void {
    this.loadProducts();
  }

  retry(): void {
    this.loadProducts();
  }

  private loadProducts(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.products$ = this.productService.getProducts().pipe(
      catchError(() => {
        this.errorMessage = 'Products could not be loaded. Please try again.';
        return of([]);
      }),
      finalize(() => this.isLoading = false)
    );
  }

  goToForm() {
    this.router.navigate(['/products/form']);
  }
  onProductAction(event: { row: ProductList; action: string }) {
    const { row, action } = event;

    switch (action) {
      
      case 'edit':
        this.onEdit(row);
        break;

      case 'details':
        this.onDetails(row);
        break;

      case 'manageBatches':
        this.onManageBatches(row);
        break;
    }
  }
  onDetails(row: ProductList) {
    this.router.navigateByUrl(`/products/detail/${row.id}`)
  }
  onEdit(row: ProductList) {
    this.router.navigateByUrl(`/products/form/${row.id}`)
  }
  onManageBatches(row: ProductList) {
    this.router.navigateByUrl(`/products/${row.id}/batches`)
  }
}
