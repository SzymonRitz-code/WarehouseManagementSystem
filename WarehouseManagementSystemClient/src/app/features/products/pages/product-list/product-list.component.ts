import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import {
  BehaviorSubject,
  catchError,
  finalize,
  map,
  merge,
  Observable,
  of,
  shareReplay,
  Subject,
  switchMap
} from 'rxjs';
import { UnitOfMeasure } from '../../../../core/enums/unitOfMeasure';
import { ComponentCardComponent } from '../../../../shared/components/common/component-card/component-card.component';
import { PageBreadcrumbComponent } from '../../../../shared/components/common/page-breadcrumb/page-breadcrumb.component';
import { TableComponent } from '../../../../shared/components/table/table.component';
import { ProductList } from '../../model/product';
import { ProductListQuery, ProductService } from '../../services/product-service';

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [CommonModule, FormsModule, PageBreadcrumbComponent, ComponentCardComponent, TableComponent],
  templateUrl: './product-list.component.html'
})
export class ProductListComponent implements OnInit {
  products$!: Observable<ProductList[]>;

  readonly isLoading = signal(false);
  readonly errorMessage = signal('');
  readonly page = signal(1);
  readonly pageSize = signal(10);
  readonly totalItems = signal(0);
  readonly sortBy = signal('sku');
  readonly sortDirection = signal<'asc' | 'desc'>('asc');

  filters = {
    search: '',
    unit: '',
    requiresBatch: '',
    isActive: ''
  };

  readonly units = Object.values(UnitOfMeasure);

  private readonly refreshProducts$ = new Subject<void>();
  private readonly queryState$ = new BehaviorSubject<ProductListQuery>(this.buildQuery());

  columns = [
    { key: 'sku', label: 'SKU', sortable: true },
    { key: 'name', label: 'Name', sortable: true },
    { key: 'unit', label: 'Unit', sortable: true },
    { key: 'requiresBatch', label: 'Requires Batch', sortable: true, type: 'boolean' },
    { key: 'weight', label: 'Weight', sortable: true },
    { key: 'volume', label: 'Volume', sortable: true },
    { key: 'isActive', label: 'Is Active', sortable: true, type: 'boolean' }
  ];

  productActions = [
    { label: 'Edit', action: 'edit' },
    { label: 'Details', action: 'details' },
    { label: 'Batches', action: 'manageBatches', visible: (row: ProductList) => row.requiresBatch === true }
  ];

  constructor(private router: Router, private productService: ProductService) {}

  ngOnInit(): void {
    this.products$ = merge(
      this.queryState$,
      this.refreshProducts$.pipe(map(() => this.queryState$.value))
    ).pipe(
      switchMap((query) => {
        this.isLoading.set(true);
        this.errorMessage.set('');

        return this.productService.getProductsPage(query).pipe(
          catchError(() => {
            this.errorMessage.set('Products could not be loaded. Please try again.');
            this.totalItems.set(0);
            return of({
              items: [],
              page: this.page(),
              pageSize: this.pageSize(),
              totalItems: 0,
              totalPages: 0
            });
          }),
          finalize(() => this.isLoading.set(false))
        );
      }),
      map(result => this.setPageResult(result)),
      shareReplay({ bufferSize: 1, refCount: true })
    );
  }

  retry(): void {
    this.refreshProducts$.next();
  }

  applyFilters(): void {
    this.page.set(1);
    this.commitQuery();
  }

  resetFilters(): void {
    this.filters = {
      search: '',
      unit: '',
      requiresBatch: '',
      isActive: ''
    };
    this.page.set(1);
    this.sortBy.set('sku');
    this.sortDirection.set('asc');
    this.commitQuery();
  }

  onPageChange(page: number): void {
    this.page.set(page);
    this.commitQuery();
  }

  onPageSizeChange(pageSize: number): void {
    this.pageSize.set(pageSize);
    this.page.set(1);
    this.commitQuery();
  }

  onSortChange(sort: { key: string; direction: 'asc' | 'desc' }): void {
    this.sortBy.set(sort.key);
    this.sortDirection.set(sort.direction);
    this.page.set(1);
    this.commitQuery();
  }

  goToForm(): void {
    this.router.navigate(['/products/form']);
  }

  onProductAction(event: { row: ProductList; action: string }): void {
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

  onDetails(row: ProductList): void {
    this.router.navigateByUrl(`/products/detail/${row.id}`);
  }

  onEdit(row: ProductList): void {
    this.router.navigateByUrl(`/products/form/${row.id}`);
  }

  onManageBatches(row: ProductList): void {
    this.router.navigateByUrl(`/products/${row.id}/batches`);
  }

  private commitQuery(): void {
    this.queryState$.next(this.buildQuery());
  }

  private buildQuery(): ProductListQuery {
    return {
      page: this.page(),
      pageSize: this.pageSize(),
      search: this.emptyToUndefined(this.filters.search),
      unit: this.emptyToUndefined(this.filters.unit),
      requiresBatch: this.toOptionalBoolean(this.filters.requiresBatch),
      isActive: this.toOptionalBoolean(this.filters.isActive),
      sortBy: this.sortBy(),
      sortDirection: this.sortDirection()
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
    this.page.set(result.page);
    this.pageSize.set(result.pageSize);
    this.totalItems.set(result.totalItems);

    return result.items;
  }
}
