import { Injectable } from "@angular/core";
import { CreateDocument } from "../model/create-document";
import { Document } from "../model/document";
import { DocumentList } from "../model/document";
import { DocumentStatus } from "../../../core/enums/documentStatus";
import { environment } from "../../../environments/environment";
import { HttpClient } from "@angular/common/http";
import { Observable } from "rxjs";
import { HttpParams } from "@angular/common/http";

export interface PagedResult<T> {
    items: T[];
    page: number;
    pageSize: number;
    totalItems: number;
    totalPages: number;
}

export interface DocumentListQuery {
    page: number;
    pageSize: number;
    search?: string;
    type?: string;
    status?: string;
    warehouseId?: string;
    createdFrom?: string;
    createdTo?: string;
    sortBy?: string;
    sortDirection?: 'asc' | 'desc';
}

@Injectable({
    providedIn: 'root'
})
export class DocumentService {

    private apiUrl: string = environment.apiUrl

    constructor(private http: HttpClient) { }

    getDocument(id: string): Observable<Document> {
        return this.http.get<Document>(`${this.apiUrl}/documents/${id}`);
    }
    getDocuments(query: DocumentListQuery): Observable<PagedResult<DocumentList>> {
        let params = new HttpParams()
            .set('page', query.page)
            .set('pageSize', query.pageSize);

        if (query.search) params = params.set('search', query.search);
        if (query.type) params = params.set('type', query.type);
        if (query.status) params = params.set('status', query.status);
        if (query.warehouseId) params = params.set('warehouseId', query.warehouseId);
        if (query.createdFrom) params = params.set('createdFrom', query.createdFrom);
        if (query.createdTo) params = params.set('createdTo', query.createdTo);
        if (query.sortBy) params = params.set('sortBy', query.sortBy);
        if (query.sortDirection) params = params.set('sortDirection', query.sortDirection);

        return this.http.get<PagedResult<DocumentList>>(`${this.apiUrl}/documents`, { params });
    }
    getPendingDocuments(query: DocumentListQuery): Observable<PagedResult<DocumentList>> {
        let params = new HttpParams()
            .set('page', query.page)
            .set('pageSize', query.pageSize);

        if (query.search) params = params.set('search', query.search);
        if (query.type) params = params.set('type', query.type);
        if (query.warehouseId) params = params.set('warehouseId', query.warehouseId);
        if (query.createdFrom) params = params.set('createdFrom', query.createdFrom);
        if (query.createdTo) params = params.set('createdTo', query.createdTo);
        if (query.sortBy) params = params.set('sortBy', query.sortBy);
        if (query.sortDirection) params = params.set('sortDirection', query.sortDirection);

        return this.http.get<PagedResult<DocumentList>>(`${this.apiUrl}/documents/pending`, { params });
    }
    addDocument(document: CreateDocument): Observable<Document> {
        return this.http.post<Document>(`${this.apiUrl}/documents`, document);
    }
    updateDocument(id: string, document: CreateDocument): Observable<Document> {
        return this.http.put<Document>(`${this.apiUrl}/documents/${id}`, { ...document, id });
    }
    confirmDocument(document: Pick<Document, 'id'> | Pick<DocumentList, 'id'>): Observable<Document> {
        return this.http.put<Document>(`${this.apiUrl}/documents/${document.id}/confirm`, document);
    }
    cancelDocument(document: Pick<Document, 'id'> | Pick<DocumentList, 'id'>): Observable<Document> {
        return this.http.put<Document>(`${this.apiUrl}/documents/${document.id}/cancel`, document);
    }

}
