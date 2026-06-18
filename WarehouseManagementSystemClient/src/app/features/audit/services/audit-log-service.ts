import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuditLog } from '../model/audtLog';

@Injectable({
  providedIn: 'root',
})
export class AuditLogService {

  private readonly apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) { }

  getAuditLogs(): Observable<AuditLog[]> {
    return this.http.get<AuditLog[]>(`${this.apiUrl}/auditlogs`);
  }
}
