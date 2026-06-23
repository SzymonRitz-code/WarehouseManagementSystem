import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { TableComponent } from './table.component';

interface TestRow {
  id: number;
  name: string;
  quantity: number;
  isActive: boolean;
  createdAt: Date;
}

describe('TableComponent', () => {
  let component: TableComponent<TestRow>;
  let fixture: ComponentFixture<TableComponent<TestRow>>;

  const columns = [
    { key: 'name', label: 'Name', sortable: true },
    { key: 'quantity', label: 'Quantity', sortable: true },
    { key: 'isActive', label: 'Active', type: 'boolean' },
    { key: 'createdAt', label: 'Created At', type: 'date' }
  ];

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TableComponent]
    }).compileComponents();

    fixture = TestBed.createComponent<TableComponent<TestRow>>(TableComponent<TestRow>);
    component = fixture.componentInstance;
    component.columns = columns;
    component.pageSize = 5;
    component.currentPage = 1;
    setTableData(rows(12));
    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('renders only the configured client-side page size', () => {
    expect(component.paginatedData.map(row => row.id)).toEqual([1, 2, 3, 4, 5]);
    expect(renderedBodyRows()).toHaveLength(5);
    expect(renderedBodyText()).toContain('Item 1');
    expect(renderedBodyText()).not.toContain('Item 6');
  });

  it('uses current page when slicing client-side data', () => {
    component.goToPage(2);
    fixture.detectChanges();

    expect(component.paginatedData.map(row => row.id)).toEqual([6, 7, 8, 9, 10]);
    expect(renderedBodyText()).toContain('Item 6');
    expect(renderedBodyText()).not.toContain('Item 5');
  });

  it('marks only the current pagination button as active', () => {
    component.goToPage(2);
    fixture.detectChanges();

    const pageButtons = paginationButtons();
    const previousPageButton = pageButtons.find(button => button.textContent?.trim() === '1');
    const currentPageButton = pageButtons.find(button => button.textContent?.trim() === '2');

    expect(previousPageButton?.classList.contains('ui-button-page-active')).toBe(false);
    expect(previousPageButton?.classList.contains('ui-button-page-default')).toBe(true);
    expect(currentPageButton?.classList.contains('ui-button-page-active')).toBe(true);
    expect(currentPageButton?.classList.contains('ui-button-page-default')).toBe(false);
  });

  it('resets to first page and limits rows when page size changes on client-side table', () => {
    component.currentPage = 3;
    component.pageSize = 10;

    component.onPageSizeChange();
    fixture.detectChanges();

    expect(component.currentPage).toBe(1);
    expect(component.paginatedData.map(row => row.id)).toEqual([1, 2, 3, 4, 5, 6, 7, 8, 9, 10]);
    expect(renderedBodyRows()).toHaveLength(10);
  });

  it('does not show every provided row in server-side mode when data has more rows than page size', () => {
    component.serverSide = true;
    component.totalItems = 100;
    component.pageSize = 10;
    setTableData(rows(50));
    fixture.detectChanges();

    expect(component.paginatedData).toHaveLength(10);
    expect(renderedBodyRows()).toHaveLength(10);
    expect(renderedBodyText()).not.toContain('Item 11');
  });

  it('emits pageSizeChange with a number and resets current page in server-side mode', () => {
    const pageSizeChange = vi.fn();
    component.serverSide = true;
    component.currentPage = 4;
    component.pageSize = '20' as unknown as number;
    component.pageSizeChange.subscribe(pageSizeChange);

    component.onPageSizeChange();

    expect(component.currentPage).toBe(1);
    expect(component.pageSize).toBe(20);
    expect(pageSizeChange).toHaveBeenCalledWith(20);
  });

  it('emits pageChange only for valid server-side pages', () => {
    const pageChange = vi.fn();
    component.serverSide = true;
    component.totalItems = 30;
    component.pageSize = 10;
    component.pageChange.subscribe(pageChange);

    component.goToPage(2);
    component.goToPage(0);
    component.goToPage(4);

    expect(component.currentPage).toBe(2);
    expect(pageChange).toHaveBeenCalledTimes(1);
    expect(pageChange).toHaveBeenCalledWith(2);
  });

  it('sorts client-side data and toggles direction on the same column', () => {
    const sortChange = vi.fn();
    component.sortChange.subscribe(sortChange);

    component.toggleSort('quantity');
    expect(component.paginatedData.map(row => row.quantity)).toEqual([1, 2, 3, 4, 5]);
    expect(sortChange).toHaveBeenLastCalledWith({ key: 'quantity', direction: 'asc' });

    component.toggleSort('quantity');
    expect(component.paginatedData.map(row => row.quantity)).toEqual([12, 11, 10, 9, 8]);
    expect(sortChange).toHaveBeenLastCalledWith({ key: 'quantity', direction: 'desc' });
  });

  it('applies client-side filters and resets to first page', () => {
    component.currentPage = 2;
    component.filters.name = 'Item 12';

    component.applyFilters();
    fixture.detectChanges();

    expect(component.currentPage).toBe(1);
    expect(component.paginatedData.map(row => row.id)).toEqual([12]);
    expect(renderedBodyRows()).toHaveLength(1);
    expect(renderedBodyText()).toContain('Item 12');
  });

  it('does not apply local filters when server-side filtering is enabled', () => {
    component.serverSide = true;
    component.filters.name = 'Item 12';

    component.applyFilters();

    expect(component.filteredData).toHaveLength(12);
  });

  it('emits row action only when action is visible for the row', () => {
    const rowAction = vi.fn();
    component.rowActions = [
      { label: 'Edit', action: 'edit' },
      { label: 'Deactivate', action: 'deactivate', visible: (row: TestRow) => row.isActive }
    ];
    component.rowAction.subscribe(rowAction);
    fixture.detectChanges();

    const firstRowButtons = fixture.debugElement.queryAll(By.css('tbody tr:first-child button'));
    firstRowButtons[0].nativeElement.click();
    firstRowButtons[1].nativeElement.click();

    expect(firstRowButtons).toHaveLength(2);
    expect(rowAction).toHaveBeenCalledWith({ row: component.paginatedData[0], action: 'edit' });
    expect(rowAction).toHaveBeenCalledWith({ row: component.paginatedData[0], action: 'deactivate' });
  });

  it('renders empty state when there are no rows', () => {
    component.data = [];
    setTableData([]);
    fixture.detectChanges();

    expect(renderedBodyRows()).toHaveLength(1);
    expect(renderedBodyText()).toContain('No records to display.');
  });

  function renderedBodyRows(): HTMLTableRowElement[] {
    return Array.from(fixture.nativeElement.querySelectorAll('tbody tr'));
  }

  function renderedBodyText(): string {
    return fixture.nativeElement.querySelector('tbody').textContent;
  }

  function paginationButtons(): HTMLButtonElement[] {
    return Array.from(fixture.nativeElement.querySelectorAll('.ui-button-page'));
  }

  function setTableData(data: TestRow[]): void {
    const previousValue = component.data;
    component.data = data;
    component.ngOnChanges({
      data: {
        previousValue,
        currentValue: data,
        firstChange: previousValue === undefined,
        isFirstChange: () => previousValue === undefined
      }
    });
  }

  function rows(count: number): TestRow[] {
    return Array.from({ length: count }, (_, index) => {
      const id = index + 1;

      return {
        id,
        name: `Item ${id}`,
        quantity: count - index,
        isActive: id % 2 === 1,
        createdAt: new Date(`2026-06-${String(Math.min(id, 28)).padStart(2, '0')}T08:00:00Z`)
      };
    });
  }
});
