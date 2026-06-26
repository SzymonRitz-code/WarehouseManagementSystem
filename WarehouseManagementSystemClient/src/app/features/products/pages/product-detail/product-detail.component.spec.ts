import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { UnitOfMeasure } from '../../../../core/enums/unitOfMeasure';
import { Product } from '../../model/product';
import { ProductService } from '../../services/product-service';
import { ProductDetailComponent } from './product-detail.component';
import { Stock } from '../../../stocks/model/stock';

describe('ProductDetailComponent', () => {
  let component: ProductDetailComponent;
  let fixture: ComponentFixture<ProductDetailComponent>;
  let productService: {
    getProduct: ReturnType<typeof vi.fn>;
    getProductStocks: ReturnType<typeof vi.fn>;
  };
  let router: { navigateByUrl: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    productService = {
      getProduct: vi.fn().mockReturnValue(of(productFixture())),
      getProductStocks: vi.fn().mockReturnValue(of(stockFixture()))
    };
    router = { navigateByUrl: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [ProductDetailComponent],
      providers: [
        { provide: ProductService, useValue: productService },
        { provide: Router, useValue: router },
        {
          provide: ActivatedRoute,
          useValue: {
            paramMap: of(convertToParamMap({ id: 'prod-1' }))
          }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ProductDetailComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('loads product details from route id', async () => {
    const vm = await firstViewModelEmission();

    expect(productService.getProduct).toHaveBeenCalledWith('prod-1');
    expect(productService.getProductStocks).toHaveBeenCalledWith('prod-1');
    expect(component.id).toBe('prod-1');
    expect(vm).toEqual({
      product: productFixture(),
      stocks: stockFixture()
    });
  });

  it('returns undefined for the detail stream when API fails', async () => {
    productService.getProduct.mockReturnValue(throwError(() => new Error('not found')));

    fixture = TestBed.createComponent(ProductDetailComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    const vm = await firstViewModelEmission();

    expect(vm).toBeUndefined();
  });

  it('navigates back to list and to edit form', () => {
    component.id = 'prod-1';

    component.onBack();
    component.onEdit();

    expect(router.navigateByUrl).toHaveBeenCalledWith('/products');
    expect(router.navigateByUrl).toHaveBeenCalledWith('/products/form/prod-1');
  });

  function firstViewModelEmission(): Promise<{ product: Product; stocks: Stock[] } | undefined> {
    return new Promise(resolve => {
      component.vm$.subscribe(vm => resolve(vm));
    });
  }

  function productFixture(): Product {
    return {
      id: 'prod-1',
      name: 'Steel Screw',
      sku: 'SKU-001',
      description: 'Warehouse consumable',
      unit: UnitOfMeasure.Piece,
      requiresBatch: true,
      isActive: true,
      weight: 0.1,
      volume: 0.01
    };
  }

  function stockFixture(): Stock[] {
    return [
      {
        id: 'stock-1',
        productId: 'prod-1',
        productSku: 'SKU-001',
        productName: 'Steel Screw',
        warehouseId: 'warehouse-1',
        warehouseName: 'Main Warehouse',
        zoneId: 'zone-1',
        zoneName: 'Picking',
        unit: UnitOfMeasure.Piece,
        quantityTotal: 100,
        quantityReserved: 20,
        quantityAvailable: 80,
        lastUpdated: new Date('2026-01-01T00:00:00Z')
      }
    ];
  }
});
