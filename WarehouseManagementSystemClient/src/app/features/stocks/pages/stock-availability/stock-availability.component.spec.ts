import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { Stock } from '../../model/stock';
import { StockService } from '../../services/stock-service';
import { StockAvailabilityComponent } from './stock-availability.component';

describe('StockAvailabilityComponent', () => {
  let component: StockAvailabilityComponent;
  let fixture: ComponentFixture<StockAvailabilityComponent>;
  let stockService: {
    getAvailableStocks: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    stockService = {
      getAvailableStocks: vi.fn().mockReturnValue(of([
        stockFixture('stock-1', 25),
        stockFixture('stock-2', 10),
        stockFixture('stock-3', 0)
      ]))
    };

    await TestBed.configureTestingModule({
      imports: [StockAvailabilityComponent],
      providers: [
        { provide: StockService, useValue: stockService }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(StockAvailabilityComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('loads stock availability and maps frontend availability statuses', async () => {
    const rows = await firstAvailabilityEmission();

    expect(stockService.getAvailableStocks).toHaveBeenCalledTimes(1);
    expect(rows.map(row => row.status)).toEqual(['In Stock', 'Low Stock', 'Out of Stock']);
    expect(component.isLoading).toBe(false);
    expect(component.errorMessage).toBe('');
  });

  it('reloads availability when retry is triggered', async () => {
    await firstAvailabilityEmission();
    stockService.getAvailableStocks.mockReturnValue(of([stockFixture('stock-4', 4)]));

    component.retry();
    const rows = await firstAvailabilityEmission();

    expect(stockService.getAvailableStocks).toHaveBeenCalledTimes(2);
    expect(rows).toEqual([expect.objectContaining({ id: 'stock-4', status: 'Low Stock' })]);
  });

  it('exposes an error state and empty rows when API fails', async () => {
    await firstAvailabilityEmission();
    stockService.getAvailableStocks.mockReturnValue(throwError(() => new Error('timeout')));

    component.retry();
    const rows = await firstAvailabilityEmission();

    expect(rows).toEqual([]);
    expect(component.errorMessage).toBe('Stock availability could not be loaded. Please try again.');
    expect(component.isLoading).toBe(false);
  });

  it('keeps loading true while the current availability request is pending', () => {
    const pendingRequest$ = new Subject<Stock[]>();
    stockService.getAvailableStocks.mockReturnValue(pendingRequest$);

    fixture = TestBed.createComponent(StockAvailabilityComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    component.stockAvailabilities$.subscribe();

    expect(component.isLoading).toBe(true);

    pendingRequest$.next([]);
    pendingRequest$.complete();

    expect(component.isLoading).toBe(false);
  });

  function firstAvailabilityEmission(): Promise<Array<Stock & { status: string }>> {
    return new Promise(resolve => {
      component.stockAvailabilities$.subscribe(rows => resolve(rows));
    });
  }

  function stockFixture(id: string, quantityAvailable: number): Stock {
    return {
      id,
      productId: 'prod-1',
      productSku: 'SKU-001',
      productName: 'Steel Screw',
      warehouseId: 'wh-1',
      warehouseName: 'Main Warehouse',
      zoneId: 'zone-1',
      zoneName: 'A-01',
      unit: 'Piece',
      quantityTotal: 100,
      quantityReserved: 100 - quantityAvailable,
      quantityAvailable,
      lastUpdated: new Date('2026-06-22T08:00:00Z')
    };
  }
});
