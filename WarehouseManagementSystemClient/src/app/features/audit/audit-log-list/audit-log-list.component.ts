import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { PageBreadcrumbComponent } from "../../../shared/components/common/page-breadcrumb/page-breadcrumb.component";
import { ComponentCardComponent } from "../../../shared/components/common/component-card/component-card.component";
import { TableComponent } from "../../../shared/components/table/table.component";
import { AuditLog } from '../model/audtLog';
import { catchError, finalize, Observable, of } from 'rxjs';
import { AuditLogService } from '../services/audit-log-service';

@Component({
  selector: 'app-audit-log-list',
  imports: [CommonModule, PageBreadcrumbComponent, ComponentCardComponent, TableComponent],
  templateUrl: './audit-log-list.component.html'
})
export class AuditLogListComponent implements OnInit {

  auditLogs$: Observable<AuditLog[]> = of([]);
  isLoading = false;
  errorMessage = '';

  constructor(private auditLogService: AuditLogService) { }

  ngOnInit(): void {
    this.loadAuditLogs();
  }

  retry(): void {
    this.loadAuditLogs();
  }

  private loadAuditLogs(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.auditLogs$ = this.auditLogService.getAuditLogs().pipe(
      catchError(() => {
        this.errorMessage = 'Audit logs could not be loaded. Please try again.';
        return of([]);
      }),
      finalize(() => this.isLoading = false)
    );
  }

  columns = [
    { key: 'id', label: 'ID' },
    { key: 'entityName', label: 'Entity' },
    { key: 'entityId', label: 'Entity ID' },
    { key: 'operation', label: 'Operation' },
    { key: 'performedByName', label: 'User' },
    { key: 'performedAt', label: 'Date', type: 'date' },
    { key: 'ipAddress', label: 'IP Address' },
  ];
}
