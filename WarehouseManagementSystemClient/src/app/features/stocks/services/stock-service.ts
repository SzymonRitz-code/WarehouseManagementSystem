import { Injectable } from '@angular/core';
import { Stock } from '../model/stock';
import { environment } from '../../../environments/environment';
import { HttpClient } from '@angular/common/http';
import { HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export interface StockListQuery {
  page: number;
  pageSize: number;
  search?: string;
  warehouseId?: string;
  zoneId?: string;
  availableOnly?: boolean;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
}

@Injectable({
  providedIn: 'root',
})
export class StockService {

  private readonly apiUrl: string = environment.apiUrl;

  constructor(private http: HttpClient ) { }

  getStocks(query: StockListQuery): Observable<PagedResult<Stock>> {
    let params = new HttpParams()
      .set('page', query.page)
      .set('pageSize', query.pageSize);

    if (query.search) params = params.set('search', query.search);
    if (query.warehouseId) params = params.set('warehouseId', query.warehouseId);
    if (query.zoneId) params = params.set('zoneId', query.zoneId);
    if (query.availableOnly !== undefined) params = params.set('availableOnly', query.availableOnly);
    if (query.sortBy) params = params.set('sortBy', query.sortBy);
    if (query.sortDirection) params = params.set('sortDirection', query.sortDirection);

    return this.http.get<PagedResult<Stock>>(`${this.apiUrl}/stocks`, { params });
  }
    getAvailableStocks(): Observable<Stock[]> {
    return this.http.get<Stock[]>(`${this.apiUrl}/stocks/availability`);
  }
}
