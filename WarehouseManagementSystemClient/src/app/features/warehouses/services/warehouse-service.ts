import { Injectable } from '@angular/core';
import { Warehouse } from '../model/warehouse';
import { CreateWarehouse } from '../model/create-warehouse';
import { environment } from '../../../environments/environment';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class WarehouseService {

  private readonly apiUrl: string = environment.apiUrl;
  
  constructor(private http: HttpClient) { }

  getWarehouse(id: string) {
    return this.http.get<Warehouse>(`${environment.apiUrl}/warehouses/${id}`);
  }

  getWarehouses(): Observable<Warehouse[]> {
    return this.http.get<Warehouse[]>(`${this.apiUrl}/warehouses`);
  }

  addWarehouse(warehouse: CreateWarehouse) {
    return this.http.post<Warehouse>(`${this.apiUrl}/warehouses`, warehouse);
  }

  updateWarehouse(warehouse: Warehouse) {
    return this.http.put<Warehouse>(`${this.apiUrl}/warehouses/${warehouse.id}`, warehouse);
  }

}
