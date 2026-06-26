import { Injectable } from '@angular/core';
import { Product } from '../model/product';
import { ProductList } from '../model/product';
import { CreateProduct } from '../model/create-product';
import { HttpClient } from '@angular/common/http';
import { HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { Observable } from 'rxjs';
import { Stock } from '../../stocks/model/stock';

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export interface ProductListQuery {
  page: number;
  pageSize: number;
  search?: string;
  unit?: string;
  requiresBatch?: boolean;
  isActive?: boolean;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
}

@Injectable({
  providedIn: 'root',
})
export class ProductService {

  private readonly apiUrl: string = environment.apiUrl;

  constructor(private http: HttpClient) { }

  getProducts(): Observable<ProductList[]> {
    return this.http.get<ProductList[]>(`${this.apiUrl}/products`);
  }

  getProductsPage(query: ProductListQuery): Observable<PagedResult<ProductList>> {
    let params = new HttpParams()
      .set('page', query.page)
      .set('pageSize', query.pageSize);

    if (query.search) params = params.set('search', query.search);
    if (query.unit) params = params.set('unit', query.unit);
    if (query.requiresBatch !== undefined) params = params.set('requiresBatch', query.requiresBatch);
    if (query.isActive !== undefined) params = params.set('isActive', query.isActive);
    if (query.sortBy) params = params.set('sortBy', query.sortBy);
    if (query.sortDirection) params = params.set('sortDirection', query.sortDirection);

    return this.http.get<PagedResult<ProductList>>(`${this.apiUrl}/products/paged`, { params });
  }

  getProduct(id: string) {
    return this.http.get<Product>(`${this.apiUrl}/products/${id}`);
  }

  getProductStocks(id: string): Observable<Stock[]> {
    return this.http.get<Stock[]>(`${this.apiUrl}/products/${id}/stocks`);
  }

  addProduct(product: CreateProduct) {
    return this.http.post<Product>(`${this.apiUrl}/products`, product);
  }

  updateProduct(id: string, product: Partial<Product>) {
    return this.http.put<Product>(`${this.apiUrl}/products/${id}`, { ...product, id });
  }
}
