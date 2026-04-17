import { Injectable } from '@angular/core';
import { TemperatureType } from '../../../core/enums/temperatureType';
import { Zone } from '../model/zone';
import { CreateZone } from '../model/create-zone';
import { environment } from '../../../environments/environment';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class ZoneService {

  private readonly apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) { }

  addZone(zone: CreateZone): Observable<Zone> {
    return this.http.post<Zone>(`${this.apiUrl}/zones`, zone)
  }
  updateZone(zone: any): Observable<Zone> {
    const warehouseZoneId = zone.id;
    console.log('Updating zone with ID:', warehouseZoneId, 'Data:', zone);
    return this.http.put<Zone>(`${this.apiUrl}/zones/${warehouseZoneId}`, zone)
  }
  getZone(warehouseZoneId: string): Observable<Zone> {
    return this.http.get<Zone>(`${this.apiUrl}/zones/${warehouseZoneId}`);
  }
  getZones(): Observable<Zone[]> {
    return this.http.get<Zone[]>(`${this.apiUrl}/zones`);
  }
}
