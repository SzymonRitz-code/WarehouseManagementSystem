import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ComponentCardComponent } from "../../../../shared/components/common/component-card/component-card.component";
import { TableComponent } from "../../../../shared/components/table/table.component";
import { Router } from '@angular/router';
import { ProductList } from '../../model/product';
import { ProductListQuery, ProductService } from '../../services/product-service';
import { PageBreadcrumbComponent } from "../../../../shared/components/common/page-breadcrumb/page-breadcrumb.component";
import { catchError, finalize, map, Observable, of } from 'rxjs';
import { CommonModule } from '@angular/common';
import { UnitOfMeasure } from '../../../../core/enums/unitOfMeasure';


@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [CommonModule, FormsModule, PageBreadcrumbComponent, ComponentCardComponent, TableComponent],
  templateUrl: './product-list.component.html'
})
export class ProductListComponent implements OnInit {

  products$: Observable<ProductList[]> = of([]);
  isLoading = false;
  errorMessage = '';
  page = 1;
  pageSize = 10;
  totalItems = 0;
  sortBy = 'sku';
  sortDirection: 'asc' | 'desc' = 'asc';
  filters = {
    search: '',
    unit: '',
    requiresBatch: '',
    isActive: ''
  };
  readonly units = Object.values(UnitOfMeasure);

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

    this.products$ = this.productService.getProductsPage(this.buildQuery()).pipe(
      catchError(() => {
        this.errorMessage = 'Products could not be loaded. Please try again.';
        this.totalItems = 0;
        return of({ items: [], page: this.page, pageSize: this.pageSize, totalItems: 0, totalPages: 0 });
      }),
      map(result => this.setPageResult(result)),
      finalize(() => this.isLoading = false)
    );
  }

  applyFilters(): void {
    this.page = 1;
    this.loadProducts();
  }

  resetFilters(): void {
    this.filters = {
      search: '',
      unit: '',
      requiresBatch: '',
      isActive: ''
    };
    this.page = 1;
    this.sortBy = 'sku';
    this.sortDirection = 'asc';
    this.loadProducts();
  }

  onPageChange(page: number): void {
    this.page = page;
    this.loadProducts();
  }

  onPageSizeChange(pageSize: number): void {
    this.pageSize = pageSize;
    this.page = 1;
    this.loadProducts();
  }

  onSortChange(sort: { key: string; direction: 'asc' | 'desc' }): void {
    this.sortBy = sort.key;
    this.sortDirection = sort.direction;
    this.page = 1;
    this.loadProducts();
  }

  private buildQuery(): ProductListQuery {
    return {
      page: this.page,
      pageSize: this.pageSize,
      search: this.emptyToUndefined(this.filters.search),
      unit: this.emptyToUndefined(this.filters.unit),
      requiresBatch: this.toOptionalBoolean(this.filters.requiresBatch),
      isActive: this.toOptionalBoolean(this.filters.isActive),
      sortBy: this.sortBy,
      sortDirection: this.sortDirection
    };
  }

  private emptyToUndefined(value: string): string | undefined {
    const trimmed = value.trim();
    return trimmed.length > 0 ? trimmed : undefined;
  }

  private toOptionalBoolean(value: string): boolean | undefined {
    if (value === 'true') return true;
    if (value === 'false') return false;
    return undefined;
  }

  private setPageResult(result: { items: ProductList[]; page: number; pageSize: number; totalItems: number }): ProductList[] {
    this.page = result.page;
    this.pageSize = result.pageSize;
    this.totalItems = result.totalItems;

    return result.items;
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
