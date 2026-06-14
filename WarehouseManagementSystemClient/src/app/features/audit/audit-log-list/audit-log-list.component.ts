import { Component } from '@angular/core';
import { PageBreadcrumbComponent } from "../../../shared/components/common/page-breadcrumb/page-breadcrumb.component";
import { ComponentCardComponent } from "../../../shared/components/common/component-card/component-card.component";
import { TableComponent } from "../../../shared/components/table/table.component";
import { AuditLog } from '../model/audtLog';

@Component({
  selector: 'app-audit-log-list',
  imports: [PageBreadcrumbComponent, ComponentCardComponent, TableComponent],
  templateUrl: './audit-log-list.component.html'
})
export class AuditLogListComponent {

  auditLogs!: AuditLog[];
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
