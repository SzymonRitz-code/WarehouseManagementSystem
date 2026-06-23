import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { of, Subject, throwError } from 'rxjs';
import { UnitOfMeasure } from '../../../../core/enums/unitOfMeasure';
import { ProductList } from '../../model/product';
import { PagedResult, ProductService } from '../../services/product-service';
import { ProductListComponent } from './product-list.component';

describe('ProductListComponent', () => {
  let component: ProductListComponent;
  let fixture: ComponentFixture<ProductListComponent>;
  let productService: {
    getProductsPage: ReturnType<typeof vi.fn>;
  };
  let router: {
    navigate: ReturnType<typeof vi.fn>;
    navigateByUrl: ReturnType<typeof vi.fn>;
  };

  const productRow: ProductList = {
    id: 'prod-1',
    sku: 'SKU-001',
    name: 'Steel Screw',
    unit: UnitOfMeasure.Piece,
    requiresBatch: true,
    weight: 0.1,
    volume: 0.01,
    isActive: true
  };

  beforeEach(async () => {
    productService = {
      getProductsPage: vi.fn()
    };
    router = {
      navigate: vi.fn(),
      navigateByUrl: vi.fn()
    };

    productService.getProductsPage.mockImplementation((query) =>
      of(pageResult([productRow], query.page, query.pageSize, 12000))
    );

    await TestBed.configureTestingModule({
      imports: [ProductListComponent],
      providers: [
        { provide: ProductService, useValue: productService },
        provideRouter([])
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ProductListComponent);
    component = fixture.componentInstance;

    const angularRouter = TestBed.inject(Router);
    vi.spyOn(angularRouter, 'navigate').mockImplementation((commands: readonly any[]) => {
      (router.navigate as any)(commands);
      return Promise.resolve(true);
    });
    vi.spyOn(angularRouter, 'navigateByUrl').mockImplementation((url: any) => {
      (router.navigateByUrl as any)(url);
      return Promise.resolve(true);
    });

    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('loads first page with the default server-side query', async () => {
    await firstProductsEmission();

    expect(productService.getProductsPage).toHaveBeenCalledWith({
      page: 1,
      pageSize: 10,
      search: undefined,
      unit: undefined,
      requiresBatch: undefined,
      isActive: undefined,
      sortBy: 'sku',
      sortDirection: 'asc'
    });
    expect(component.totalItems()).toBe(12000);
    expect(component.isLoading()).toBe(false);
  });

  it('sends filters to backend and resets to first page', async () => {
    await firstProductsEmission();
    productService.getProductsPage.mockReturnValue(of(pageResult([], 1, 10, 0)));

    component.page.set(5);
    component.filters.search = ' SKU-001 ';
    component.filters.unit = UnitOfMeasure.Piece;
    component.filters.requiresBatch = 'true';
    component.filters.isActive = 'false';

    component.applyFilters();
    await firstProductsEmission();

    expect(component.page()).toBe(1);
    expect(lastGetProductsQuery()).toEqual(expect.objectContaining({
      page: 1,
      search: 'SKU-001',
      unit: UnitOfMeasure.Piece,
      requiresBatch: true,
      isActive: false
    }));
  });

  it('keeps the latest query and reruns it on retry', async () => {
    await firstProductsEmission();
    component.filters.search = 'screw';
    component.applyFilters();
    await firstProductsEmission();

    component.retry();
    await firstProductsEmission();

    expect(productService.getProductsPage).toHaveBeenCalledTimes(3);
    expect(lastGetProductsQuery()).toEqual(expect.objectContaining({ search: 'screw' }));
  });

  it('uses server paging and sorting events as backend query changes', async () => {
    await firstProductsEmission();

    component.onPageSizeChange(50);
    expect(lastGetProductsQuery()).toEqual(expect.objectContaining({ page: 1, pageSize: 50 }));

    component.onPageChange(4);
    expect(lastGetProductsQuery()).toEqual(expect.objectContaining({ page: 4, pageSize: 50 }));

    component.onSortChange({ key: 'name', direction: 'desc' });
    expect(lastGetProductsQuery()).toEqual(expect.objectContaining({
      page: 1,
      sortBy: 'name',
      sortDirection: 'desc'
    }));
  });

  it('resets filters and sorting to the default server query', async () => {
    await firstProductsEmission();
    component.filters.search = 'screw';
    component.filters.unit = UnitOfMeasure.Piece;
    component.filters.requiresBatch = 'true';
    component.filters.isActive = 'false';
    component.sortBy.set('name');
    component.sortDirection.set('desc');

    component.resetFilters();

    expect(lastGetProductsQuery()).toEqual({
      page: 1,
      pageSize: 10,
      search: undefined,
      unit: undefined,
      requiresBatch: undefined,
      isActive: undefined,
      sortBy: 'sku',
      sortDirection: 'asc'
    });
  });

  it('exposes an error state and empty rows when API fails', async () => {
    await firstProductsEmission();
    productService.getProductsPage.mockReturnValue(throwError(() => new Error('timeout')));

    component.retry();

    expect(component.errorMessage()).toBe('Products could not be loaded. Please try again.');
    expect(component.totalItems()).toBe(0);
    expect(component.isLoading()).toBe(false);
  });

  it('keeps loading true while the current HTTP request is pending', () => {
    const pendingRequest$ = new Subject<PagedResult<ProductList>>();
    productService.getProductsPage.mockReturnValue(pendingRequest$);

    fixture = TestBed.createComponent(ProductListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    component.products$.subscribe();

    expect(component.isLoading()).toBe(true);

    pendingRequest$.next(pageResult([], 1, 10, 0));
    pendingRequest$.complete();

    expect(component.isLoading()).toBe(false);
  });

  it('navigates to product routes from list actions', () => {
    component.goToForm();
    component.onProductAction({ row: productRow, action: 'details' });
    component.onProductAction({ row: productRow, action: 'edit' });
    component.onProductAction({ row: productRow, action: 'manageBatches' });

    expect(router.navigate).toHaveBeenCalledWith(['/products/form']);
    expect(router.navigateByUrl).toHaveBeenCalledWith('/products/detail/prod-1');
    expect(router.navigateByUrl).toHaveBeenCalledWith('/products/form/prod-1');
    expect(router.navigateByUrl).toHaveBeenCalledWith('/products/prod-1/batches');
  });

  function firstProductsEmission(): Promise<ProductList[]> {
    return new Promise(resolve => {
      component.products$.subscribe(rows => resolve(rows));
    });
  }

  function lastGetProductsQuery() {
    const calls = productService.getProductsPage.mock.calls;
    return calls[calls.length - 1][0];
  }

  function pageResult(items: ProductList[], page: number, pageSize: number, totalItems: number): PagedResult<ProductList> {
    return {
      items,
      page,
      pageSize,
      totalItems,
      totalPages: Math.ceil(totalItems / pageSize)
    };
  }
});
