import { Injectable } from '@angular/core';
import { Batch, BatchList } from '../model/product-batch';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { HttpClient } from '@angular/common/http';
import { CreateBatch } from '../model/product-create-batch';

@Injectable({
  providedIn: 'root',
})
export class ProductBatchService {

  private readonly apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) { }

  getBatches(productId: string): Observable<BatchList[]> {
    return this.http.get<BatchList[]>(`${this.apiUrl}/products/${productId}/batches`);
  }
  getBatch(productId: string, batchId: string): Observable<Batch> {
    return this.http.get<Batch>(`${this.apiUrl}/products/${productId}/batches/${batchId}`);
  }
  updateBatch(productId: string, batchId: string, batch: Partial<Batch>): Observable<Batch> {
    return this.http.put<Batch>(`${this.apiUrl}/products/${productId}/batches/${batchId}`, batch);
  }
  createBatch(productId: string, batch: CreateBatch): Observable<Batch> {
    return this.http.post<Batch>(`${this.apiUrl}/products/${productId}/batches`, batch);
  }
}
