import { Injectable } from "@angular/core";
import { CreateDocument } from "../model/create-document";
import { Document } from "../model/document";
import { DocumentList } from "../model/document";
import { DocumentStatus } from "../../../core/enums/documentStatus";
import { environment } from "../../../environments/environment";
import { HttpClient } from "@angular/common/http";
import { Observable } from "rxjs";

@Injectable({
    providedIn: 'root'
})
export class DocumentService {

    private apiUrl: string = environment.apiUrl

    constructor(private http: HttpClient) { }

    getDocument(id: string): Observable<Document> {
        return this.http.get<Document>(`${this.apiUrl}/documents/${id}`);
    }
    getDocuments(): Observable<DocumentList[]> {
        return this.http.get<DocumentList[]>(`${this.apiUrl}/documents`);
    }
    getPendingDocuments(): Observable<DocumentList[]> {
        return this.http.get<DocumentList[]>(`${this.apiUrl}/documents/pending`);
    }
    addDocument(document: CreateDocument): Observable<Document> {
        return this.http.post<Document>(`${this.apiUrl}/documents`, document);
    }
    updateDocument(id: string, document: CreateDocument): Observable<Document> {
        return this.http.put<Document>(`${this.apiUrl}/documents/${id}`, { ...document, id });
    }
    confirmDocument(document: Pick<Document, 'id'> | Pick<DocumentList, 'id'>): Observable<Document> {
        console.log(`Confirming document ${document.id}`);
        return this.http.put<Document>(`${this.apiUrl}/documents/${document.id}/confirm`, document);
    }
    transferDocument(document: Pick<Document, 'id'>): Observable<Document> {
        return this.http.put<Document>(`${this.apiUrl}/documents/${document.id}/transfer`, document);
    }
    cancelDocument(document: Pick<Document, 'id'> | Pick<DocumentList, 'id'>): Observable<Document> {
        return this.http.put<Document>(`${this.apiUrl}/documents/${document.id}/cancel`, document);
    }

}
