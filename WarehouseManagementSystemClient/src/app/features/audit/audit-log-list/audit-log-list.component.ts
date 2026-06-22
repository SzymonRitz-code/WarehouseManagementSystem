import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { PageBreadcrumbComponent } from "../../../shared/components/common/page-breadcrumb/page-breadcrumb.component";
import { ComponentCardComponent } from "../../../shared/components/common/component-card/component-card.component";
import { TableComponent } from "../../../shared/components/table/table.component";
import { AuditLog } from '../model/audtLog';
import { catchError, finalize, Observable, of, shareReplay, startWith, Subject, switchMap } from 'rxjs';
import { AuditLogService } from '../services/audit-log-service';

@Component({
  selector: 'app-audit-log-list',
  imports: [CommonModule, PageBreadcrumbComponent, ComponentCardComponent, TableComponent],
  templateUrl: './audit-log-list.component.html'
})
export class AuditLogListComponent implements OnInit {

  auditLogs$!: Observable<AuditLog[]>;
  isLoading = false;
  errorMessage = '';
  private readonly reloadAuditLogs$ = new Subject<void>();

  constructor(private auditLogService: AuditLogService) { }

  ngOnInit(): void {
    this.auditLogs$ = this.reloadAuditLogs$.pipe(
      startWith(void 0),
      switchMap(() => {
        this.isLoading = true;
        this.errorMessage = '';

        return this.auditLogService.getAuditLogs().pipe(
          catchError(() => {
            this.errorMessage = 'Audit logs could not be loaded. Please try again.';
            return of([]);
          }),
          finalize(() => this.isLoading = false)
        );
      }),
      shareReplay({ bufferSize: 1, refCount: true })
    );
  }

  retry(): void {
    this.loadAuditLogs();
  }

  private loadAuditLogs(): void {
    this.reloadAuditLogs$.next();
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
