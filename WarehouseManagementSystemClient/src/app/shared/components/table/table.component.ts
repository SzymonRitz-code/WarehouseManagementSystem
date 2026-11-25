import { CommonModule } from '@angular/common';
import { Component, input, linkedSignal, output, OnChanges, OnInit, SimpleChanges } from '@angular/core';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-table',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './table.component.html',
})
export class TableComponent<T extends Record<string, any> = any> implements OnInit, OnChanges {
  readonly columns = input<{ key: string; label: string; sortable?: boolean; template?: any; type?: string }[]>([]);
  readonly rowActions = input<any[]>();
  readonly data = input<T[] | null>([]);
  readonly pageSize = input(10);
  readonly currentPage = input(1);
  readonly totalItems = input(0);
  readonly serverSide = input(false);
  readonly filterable = input(true);

  readonly pageSizeState = linkedSignal(() => Number(this.pageSize()));
  readonly currentPageState = linkedSignal(() => Number(this.currentPage()));

  readonly sortChange = output<{ key: string; direction: 'asc' | 'desc' }>();
  readonly pageChange = output<number>();
  readonly pageSizeChange = output<number>();
  readonly rowAction = output<{ row: T; action: string }>();

  sortKey: string | null = null;
  sortDir: 'asc' | 'desc' = 'asc';
  filters: Partial<Record<keyof T, string>> = {};
  filteredData: T[] = [];

  ngOnInit(): void {
    //this.filteredData = [...this.data!];
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['data']) {
      this.filteredData = [...(this.data() ?? [])];
    }
  }

  trackByFn(index: number, item: T) {
    return (item as any).id ?? index;
  }

  get paginatedData() {
    const pageSize = this.pageSizeState();

    if (this.serverSide()) return this.filteredData.slice(0, pageSize);

    const start = (this.currentPageState() - 1) * pageSize;
    return this.filteredData.slice(start, start + pageSize);
  }

  get pages(): number[] {
    return Array.from({ length: this.totalPages }, (_, i) => i + 1);
  }

  get totalPages() {
    const pageSize = this.pageSizeState();

    if (this.serverSide()) {
      return Math.ceil(this.totalItems() / pageSize);
    }

    return Math.ceil(this.filteredData.length / pageSize);
  }

  onPageSizeChange(pageSize: number) {
    this.pageSizeState.set(Number(pageSize));
    this.currentPageState.set(1);
    if (this.serverSide()) {
      this.pageSizeChange.emit(this.pageSizeState());
    }
  }

  get sortedData(): T[] {
    if (!this.sortKey) return this.data() ?? [];
    return [...(this.data() ?? [])].sort((a, b) => {
      const aVal = a[this.sortKey!];
      const bVal = b[this.sortKey!];
      if (aVal < bVal) return this.sortDir === 'asc' ? -1 : 1;
      if (aVal > bVal) return this.sortDir === 'asc' ? 1 : -1;
      return 0;
    });
  }

  applyFilters() {
    if (this.serverSide()) return;

    this.currentPageState.set(1);
    this.filteredData = this.sortedData.filter((row: T) =>
      this.columns().every(col => {
        const value = row[col.key];
        const filter = this.filters[col.key]?.toLowerCase() || '';
        return String(value).toLowerCase().includes(filter);
      })
    );
  }

  goToPageSafe(page: number | '...') {
    if (page === '...') return;
    this.goToPage(page);
  }

  goToPage(page: number) {
    if (page < 1 || page > this.totalPages) return;
    this.currentPageState.set(page);
    if (this.serverSide()) {
      this.pageChange.emit(page);
    }
  }

  toggleSort(columnKey: string) {
    if (this.sortKey === columnKey) {
      this.sortDir = this.sortDir === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortKey = columnKey;
      this.sortDir = 'asc';
    }
    this.sortChange.emit({ key: columnKey, direction: this.sortDir });
    if (this.serverSide()) return;

    this.applyFilters();
  }

  get visiblePages(): (number | '...')[] {
    const total = this.totalPages;
    const current = this.currentPageState();
    const delta = 2; // ile stron po lewej/prawej od current
    const pages: (number | '...')[] = [];

    if (total <= 7) {
      // mało stron, pokazujemy wszystkie
      for (let i = 1; i <= total; i++) pages.push(i);
    } else {
      // zawsze pokazujemy 1 i last
      pages.push(1);

      if (current - delta > 2) pages.push('...');
      for (let i = Math.max(2, current - delta); i <= Math.min(total - 1, current + delta); i++) {
        pages.push(i);
      }
      if (current + delta < total - 1) pages.push('...');

      pages.push(total);
    }

    return pages;
  }

  emitRowAction(row: T, action: string) {
    this.rowAction.emit({ row, action });
  }

  get displayedRecordCount(): number {
    return this.serverSide() ? this.totalItems() : this.filteredData.length;
  }
}
