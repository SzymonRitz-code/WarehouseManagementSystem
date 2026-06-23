import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { WarehouseService } from '../../../warehouses/services/warehouse-service';
import { ZoneService } from '../../../zones/services/zone-service';
import { Stock } from '../../model/stock';
import { PagedResult, StockService } from '../../services/stock-service';
import { StockListComponent } from './stock-list.component';

describe('StockListComponent', () => {
  let component: StockListComponent;
  let fixture: ComponentFixture<StockListComponent>;
  let stockService: {
    getStocks: ReturnType<typeof vi.fn>;
  };
  let warehouseService: {
    getWarehouses: ReturnType<typeof vi.fn>;
  };
  let zoneService: {
    getZones: ReturnType<typeof vi.fn>;
  };

  const stockRow: Stock = {
    id: 'stock-1',
    productId: 'prod-1',
    productSku: 'SKU-001',
    productName: 'Steel Screw',
    warehouseId: 'wh-1',
    warehouseName: 'Main Warehouse',
    zoneId: 'zone-1',
    zoneName: 'A-01',
    unit: 'Piece',
    quantityTotal: 100,
    quantityReserved: 15,
    quantityAvailable: 85,
    lastUpdated: new Date('2026-06-22T08:00:00Z')
  };

  beforeEach(async () => {
    stockService = {
      getStocks: vi.fn()
    };
    warehouseService = {
      getWarehouses: vi.fn().mockReturnValue(of([
        { id: 'wh-1', code: 'MAIN', name: 'Main Warehouse' }
      ]))
    };
    zoneService = {
      getZones: vi.fn().mockReturnValue(of([
        { id: 'zone-1', code: 'A-01', name: 'A-01', warehouseId: 'wh-1' }
      ]))
    };

    stockService.getStocks.mockImplementation((query) =>
      of(pageResult([stockRow], query.page, query.pageSize, 2500))
    );

    await TestBed.configureTestingModule({
      imports: [StockListComponent],
      providers: [
        { provide: StockService, useValue: stockService },
        { provide: WarehouseService, useValue: warehouseService },
        { provide: ZoneService, useValue: zoneService }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(StockListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('loads first page with the default server-side query', async () => {
    await firstStocksEmission();

    expect(stockService.getStocks).toHaveBeenCalledWith({
      page: 1,
      pageSize: 10,
      search: undefined,
      warehouseId: undefined,
      zoneId: undefined,
      availableOnly: undefined,
      sortBy: 'lastUpdated',
      sortDirection: 'desc'
    });
    expect(component.totalItems()).toBe(2500);
    expect(component.isLoading()).toBe(false);
  });

  it('loads warehouse and zone filter options defensively', async () => {
    await firstStocksEmission();

    const warehouses = await firstEmission(component.warehouses$);
    const zones = await firstEmission(component.zones$);

    expect(warehouseService.getWarehouses).toHaveBeenCalledTimes(1);
    expect(zoneService.getZones).toHaveBeenCalledTimes(1);
    expect(warehouses).toEqual([{ id: 'wh-1', code: 'MAIN', name: 'Main Warehouse' }]);
    expect(zones).toEqual([{ id: 'zone-1', code: 'A-01', name: 'A-01', warehouseId: 'wh-1' }]);
  });

  it('sends filters to backend and resets to first page', async () => {
    await firstStocksEmission();
    stockService.getStocks.mockReturnValue(of(pageResult([], 1, 10, 0)));

    component.page.set(7);
    component.filters.search = ' SKU-001 ';
    component.filters.warehouseId = 'wh-1';
    component.filters.zoneId = 'zone-1';
    component.filters.availableOnly = true;

    component.applyFilters();
    await firstStocksEmission();

    expect(component.page()).toBe(1);
    expect(lastGetStocksQuery()).toEqual(expect.objectContaining({
      page: 1,
      search: 'SKU-001',
      warehouseId: 'wh-1',
      zoneId: 'zone-1',
      availableOnly: true
    }));
  });

  it('keeps the latest query and reruns it on retry', async () => {
    await firstStocksEmission();
    component.filters.search = 'screw';
    component.applyFilters();
    await firstStocksEmission();

    component.retry();
    await firstStocksEmission();

    expect(stockService.getStocks).toHaveBeenCalledTimes(3);
    expect(lastGetStocksQuery()).toEqual(expect.objectContaining({ search: 'screw' }));
  });

  it('uses server paging and sorting events as backend query changes', async () => {
    await firstStocksEmission();

    component.onPageSizeChange(100);
    expect(lastGetStocksQuery()).toEqual(expect.objectContaining({ page: 1, pageSize: 100 }));

    component.onPageChange(3);
    expect(lastGetStocksQuery()).toEqual(expect.objectContaining({ page: 3, pageSize: 100 }));

    component.onSortChange({ key: 'quantityAvailable', direction: 'asc' });
    expect(lastGetStocksQuery()).toEqual(expect.objectContaining({
      page: 1,
      sortBy: 'quantityAvailable',
      sortDirection: 'asc'
    }));
  });

  it('resets filters and sorting to the default server query', async () => {
    await firstStocksEmission();
    component.filters.search = 'screw';
    component.filters.warehouseId = 'wh-1';
    component.filters.zoneId = 'zone-1';
    component.filters.availableOnly = true;
    component.sortBy.set('quantityTotal');
    component.sortDirection.set('asc');

    component.resetFilters();

    expect(lastGetStocksQuery()).toEqual({
      page: 1,
      pageSize: 10,
      search: undefined,
      warehouseId: undefined,
      zoneId: undefined,
      availableOnly: undefined,
      sortBy: 'lastUpdated',
      sortDirection: 'desc'
    });
  });

  it('exposes an error state and empty rows when API fails', async () => {
    await firstStocksEmission();
    stockService.getStocks.mockReturnValue(throwError(() => new Error('timeout')));

    component.retry();

    expect(component.errorMessage()).toBe('Stocks could not be loaded. Please try again.');
    expect(component.totalItems()).toBe(0);
    expect(component.isLoading()).toBe(false);
  });

  it('keeps loading true while the current HTTP request is pending', () => {
    const pendingRequest$ = new Subject<PagedResult<Stock>>();
    stockService.getStocks.mockReturnValue(pendingRequest$);

    fixture = TestBed.createComponent(StockListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    component.stocks$.subscribe();

    expect(component.isLoading()).toBe(true);

    pendingRequest$.next(pageResult([], 1, 10, 0));
    pendingRequest$.complete();

    expect(component.isLoading()).toBe(false);
  });

  it('uses empty option lists when warehouses or zones cannot be loaded', async () => {
    warehouseService.getWarehouses.mockReturnValue(throwError(() => new Error('warehouse timeout')));
    zoneService.getZones.mockReturnValue(throwError(() => new Error('zone timeout')));

    fixture = TestBed.createComponent(StockListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    await expect(firstEmission(component.warehouses$)).resolves.toEqual([]);
    await expect(firstEmission(component.zones$)).resolves.toEqual([]);
  });

  function firstStocksEmission(): Promise<Stock[]> {
    return firstEmission(component.stocks$);
  }

  function firstEmission<T>(source$: { subscribe: (next: (value: T) => void) => unknown }): Promise<T> {
    return new Promise(resolve => {
      source$.subscribe(value => resolve(value));
    });
  }

  function lastGetStocksQuery() {
    const calls = stockService.getStocks.mock.calls;
    return calls[calls.length - 1][0];
  }

  function pageResult(items: Stock[], page: number, pageSize: number, totalItems: number): PagedResult<Stock> {
    return {
      items,
      page,
      pageSize,
      totalItems,
      totalPages: Math.ceil(totalItems / pageSize)
    };
  }
});
