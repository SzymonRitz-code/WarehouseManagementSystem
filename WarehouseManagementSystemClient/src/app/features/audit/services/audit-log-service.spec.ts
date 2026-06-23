import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../environments/environment';
import { AuditLogService } from './audit-log-service';

describe('AuditLogService', () => {
  let service: AuditLogService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        AuditLogService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(AuditLogService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('loads audit logs from the API endpoint', () => {
    const response = [
      {
        id: 'audit-1',
        userId: 'user-1',
        action: 'Created',
        entityName: 'Document',
        entityId: 'doc-1',
        changes: 'Document created',
        createdAt: new Date('2026-06-22T10:00:00Z')
      }
    ];

    service.getAuditLogs().subscribe(result => {
      expect(result).toEqual(response);
    });

    const req = httpMock.expectOne(`${environment.apiUrl}/auditlogs`);
    expect(req.request.method).toBe('GET');

    req.flush(response);
  });
});
