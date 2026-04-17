import { Injectable } from '@angular/core';
import { Stock } from '../model/stock';
import { environment } from '../../../environments/environment';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class StockService {

  private readonly apiUrl: string = environment.apiUrl;

  constructor(private http: HttpClient ) { }

  getStocks(): Observable<Stock[]> {
    return this.http.get<Stock[]>(`${this.apiUrl}/stocks`);
  }
    getAvailableStocks(): Observable<Stock[]> {
    return this.http.get<Stock[]>(`${this.apiUrl}/stocks/availability`);
  }
}
