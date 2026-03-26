import { CommonModule } from '@angular/common';
import { Component, Input, Output, EventEmitter, OnInit, } from '@angular/core';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-table',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './table.component.html',
})
export class TableComponent<T extends Record<string, any> = any> implements OnInit {

  @Input() columns: { key: string; label: string; sortable?: boolean; template?: any; type?: string }[] = [];
  @Input() rowActions!: any[];
  @Input() data: T[] = [];
  @Input() pageSize = 10;

  @Output() sortChange = new EventEmitter<{ key: string; direction: 'asc' | 'desc' }>();
  @Output() rowAction = new EventEmitter<{ row: T; action: string }>();

  currentPage = 1;
  sortKey: string | null = null;
  sortDir: 'asc' | 'desc' = 'asc';
  filters: Partial<Record<keyof T, string>> = {};
  filteredData: T[] = [];

  ngOnInit(): void {
    this.filteredData = [...this.data];
  }

  get paginatedData() {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.filteredData.slice(start, start + this.pageSize);
  }
  get pages(): number[] {
    return Array.from({ length: this.totalPages }, (_, i) => i + 1);
  }

  get totalPages() {
    return Math.ceil(this.filteredData.length / this.pageSize);
  }
  onPageSizeChange() {
    this.currentPage = 1; // reset paginacji
  }

  get sortedData(): T[] {
    if (!this.sortKey) return this.data;
    return [...this.data].sort((a, b) => {
      const aVal = a[this.sortKey!];
      const bVal = b[this.sortKey!];
      if (aVal < bVal) return this.sortDir === 'asc' ? -1 : 1;
      if (aVal > bVal) return this.sortDir === 'asc' ? 1 : -1;
      return 0;
    });
  }
  applyFilters() {
    this.currentPage = 1;
    this.filteredData = this.sortedData.filter((row: T) =>
      this.columns.every(col => {
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
    this.currentPage = page;
  }

  toggleSort(columnKey: string) {
    if (this.sortKey === columnKey) {
      this.sortDir = this.sortDir === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortKey = columnKey;
      this.sortDir = 'asc';
    }
    this.sortChange.emit({ key: columnKey, direction: this.sortDir });
    this.applyFilters();
  }
  get visiblePages(): (number | '...')[] {
    const total = this.totalPages;
    const current = this.currentPage;
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
}