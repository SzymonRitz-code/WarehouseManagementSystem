import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { DocumentStatus } from '../../../core/enums/documentStatus';
import { DocumentType } from '../../../core/enums/documentType';
import { environment } from '../../../environments/environment';
import { CreateDocument } from '../model/create-document';
import { DocumentService } from './document-service';

describe('DocumentService', () => {
  let service: DocumentService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        DocumentService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(DocumentService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('sends server-side list query parameters for documents', () => {
    service.getDocuments({
      page: 2,
      pageSize: 25,
      search: 'DOC/001',
      type: DocumentType.PZ,
      status: DocumentStatus.Draft,
      warehouseId: 'wh-1',
      createdFrom: '2026-06-01',
      createdTo: '2026-06-22',
      sortBy: 'documentNumber',
      sortDirection: 'asc'
    }).subscribe();

    const req = httpMock.expectOne(request =>
      request.method === 'GET' && request.url === `${environment.apiUrl}/documents`
    );

    expect(req.request.params.get('page')).toBe('2');
    expect(req.request.params.get('pageSize')).toBe('25');
    expect(req.request.params.get('search')).toBe('DOC/001');
    expect(req.request.params.get('type')).toBe(DocumentType.PZ);
    expect(req.request.params.get('status')).toBe(DocumentStatus.Draft);
    expect(req.request.params.get('warehouseId')).toBe('wh-1');
    expect(req.request.params.get('createdFrom')).toBe('2026-06-01');
    expect(req.request.params.get('createdTo')).toBe('2026-06-22');
    expect(req.request.params.get('sortBy')).toBe('documentNumber');
    expect(req.request.params.get('sortDirection')).toBe('asc');

    req.flush({ items: [], page: 2, pageSize: 25, totalItems: 0, totalPages: 0 });
  });

  it('does not send empty optional filters to documents endpoint', () => {
    service.getDocuments({
      page: 1,
      pageSize: 10,
      sortBy: 'createdAt',
      sortDirection: 'desc'
    }).subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/documents?page=1&pageSize=10&sortBy=createdAt&sortDirection=desc`);

    expect(req.request.params.has('search')).toBe(false);
    expect(req.request.params.has('type')).toBe(false);
    expect(req.request.params.has('status')).toBe(false);
    expect(req.request.params.has('warehouseId')).toBe(false);

    req.flush({ items: [], page: 1, pageSize: 10, totalItems: 0, totalPages: 0 });
  });

  it('sends pending documents query to dedicated endpoint', () => {
    service.getPendingDocuments({
      page: 3,
      pageSize: 50,
      search: 'operator',
      type: DocumentType.MM,
      warehouseId: 'wh-2'
    }).subscribe();

    const req = httpMock.expectOne(request =>
      request.method === 'GET' && request.url === `${environment.apiUrl}/documents/pending`
    );

    expect(req.request.params.get('page')).toBe('3');
    expect(req.request.params.get('pageSize')).toBe('50');
    expect(req.request.params.get('search')).toBe('operator');
    expect(req.request.params.get('type')).toBe(DocumentType.MM);
    expect(req.request.params.get('warehouseId')).toBe('wh-2');

    req.flush({ items: [], page: 3, pageSize: 50, totalItems: 0, totalPages: 0 });
  });

  it('uses workflow endpoints for confirm and cancel commands', () => {
    service.confirmDocument({ id: 'doc-1' }).subscribe();
    const confirmReq = httpMock.expectOne(`${environment.apiUrl}/documents/doc-1/confirm`);
    expect(confirmReq.request.method).toBe('PUT');
    expect(confirmReq.request.body).toEqual({ id: 'doc-1' });
    confirmReq.flush({ id: 'doc-1' });

    service.cancelDocument({ id: 'doc-1' }).subscribe();
    const cancelReq = httpMock.expectOne(`${environment.apiUrl}/documents/doc-1/cancel`);
    expect(cancelReq.request.method).toBe('PUT');
    expect(cancelReq.request.body).toEqual({ id: 'doc-1' });
    cancelReq.flush({ id: 'doc-1' });
  });

  it('posts and puts document payloads for create/update', () => {
    const payload: CreateDocument = {
      documentDate: new Date('2026-06-22T00:00:00Z'),
      type: DocumentType.PZ,
      sourceWarehouseId: 'wh-1',
      targetWarehouseId: 'wh-2',
      notes: 'delivery',
      items: [
        { productId: 'prod-1', quantity: 5, sourceZoneId: 'zone-1', targetZoneId: 'zone-2' }
      ]
    };

    service.addDocument(payload).subscribe();
    const createReq = httpMock.expectOne(`${environment.apiUrl}/documents`);
    expect(createReq.request.method).toBe('POST');
    expect(createReq.request.body).toEqual(payload);
    createReq.flush({ id: 'doc-created', ...payload });

    service.updateDocument('doc-1', payload).subscribe();
    const updateReq = httpMock.expectOne(`${environment.apiUrl}/documents/doc-1`);
    expect(updateReq.request.method).toBe('PUT');
    expect(updateReq.request.body).toEqual({ ...payload, id: 'doc-1' });
    updateReq.flush({ id: 'doc-1', ...payload });
  });
});
