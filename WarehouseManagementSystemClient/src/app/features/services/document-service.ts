import { Injectable } from "@angular/core";
import { CreateDocument } from "../documents/model/create-document";
import { Document } from "../documents/model/document";
import { DocumentStatus } from "../../core/enums/documentStatus";

@Injectable({
    providedIn: 'root'
})
export class DocumentService {




    documents = [
        { id: '1', documentNumber: "DOC-1001", type: "Transfer", status: "Completed", sourceWarehouse: "Regional DC Kraków", destinationWarehouse: "North Hub Gdańsk", createdBy: "Tom Brown", approvedBy: "Anna Kowalska", createdAt: new Date("2026-03-16T15:36:50"), approvedAt: new Date("2026-03-18T13:36:50"), itemCount: 20, totalQuantity: 538, actions: "" },
        { id: '2', documentNumber: "DOC-1002", type: "Transfer", status: "Completed", sourceWarehouse: "Central Warehouse Warsaw", destinationWarehouse: "East DC Lublin", createdBy: "Tom Brown", approvedBy: "Anna Kowalska", createdAt: new Date("2026-03-05T15:49:36"), approvedAt: new Date("2026-03-07T00:49:36"), itemCount: 19, totalQuantity: 509, actions: "" },
        { id: '3', documentNumber: "DOC-1003", type: "Transfer", status: "Cancelled", sourceWarehouse: "North Hub Gdańsk", destinationWarehouse: "North Hub Gdańsk", createdBy: "Sara Lee", approvedBy: "", createdAt: new Date("2026-03-16T01:40:54"), approvedAt: null, itemCount: 5, totalQuantity: 114, actions: "" },
        { id: '4', documentNumber: "DOC-1004", type: "Adjustment", status: "Cancelled", sourceWarehouse: "", destinationWarehouse: "", createdBy: "Tom Brown", approvedBy: "", createdAt: new Date("2026-02-28T01:25:56"), approvedAt: null, itemCount: 7, totalQuantity: 199, actions: "" },
        { id: '5', documentNumber: "DOC-1005", type: "Issue", status: "Completed", sourceWarehouse: "South Hub Wrocław", destinationWarehouse: "", createdBy: "Michael Johnson", approvedBy: "Sara Lee", createdAt: new Date("2026-02-18T09:51:55"), approvedAt: new Date("2026-02-19T15:51:55"), itemCount: 5, totalQuantity: 149, actions: "" },
        { id: '6', documentNumber: "DOC-1006", type: "Issue", status: "Completed", sourceWarehouse: "East DC Lublin", destinationWarehouse: "", createdBy: "Anna Kowalska", approvedBy: "John Smith", createdAt: new Date("2026-02-23T00:36:56"), approvedAt: new Date("2026-02-24T10:36:56"), itemCount: 5, totalQuantity: 141, actions: "" },
        { id: '7', documentNumber: "DOC-1007", type: "Transfer", status: "Completed", sourceWarehouse: "Regional DC Kraków", destinationWarehouse: "North Hub Gdańsk", createdBy: "John Smith", approvedBy: "Tom Brown", createdAt: new Date("2026-03-13T05:59:25"), approvedAt: new Date("2026-03-13T06:59:25"), itemCount: 18, totalQuantity: 403, actions: "" },
        { id: '8', documentNumber: "DOC-1008", type: "Transfer", status: "Draft", sourceWarehouse: "North Hub Gdańsk", destinationWarehouse: "North Hub Gdańsk", createdBy: "Michael Johnson", approvedBy: "", createdAt: new Date("2026-03-08T10:28:19"), approvedAt: null, itemCount: 5, totalQuantity: 114, actions: "" },
        { id: '9', documentNumber: "DOC-1009", type: "Receipt", status: "Confirmed", sourceWarehouse: "", destinationWarehouse: "Central Warehouse Warsaw", createdBy: "John Smith", approvedBy: "Sara Lee", createdAt: new Date("2026-02-23T20:25:19"), approvedAt: new Date("2026-02-24T11:25:19"), itemCount: 20, totalQuantity: 390, actions: "" },
        { id: '10', documentNumber: "DOC-1010", type: "Issue", status: "Confirmed", sourceWarehouse: "East DC Lublin", destinationWarehouse: "", createdBy: "John Smith", approvedBy: "Michael Johnson", createdAt: new Date("2026-03-10T03:12:12"), approvedAt: new Date("2026-03-11T19:12:12"), itemCount: 9, totalQuantity: 237, actions: "" },
        { id: '11', documentNumber: "DOC-1011", type: "Adjustment", status: "Completed", sourceWarehouse: "", destinationWarehouse: "", createdBy: "Sara Lee", approvedBy: "Michael Johnson", createdAt: new Date("2026-03-17T06:11:02"), approvedAt: new Date("2026-03-17T20:11:02"), itemCount: 1, totalQuantity: 45, actions: "" },
        { id: '12', documentNumber: "DOC-1012", type: "Issue", status: "Cancelled", sourceWarehouse: "North Hub Gdańsk", destinationWarehouse: "", createdBy: "Michael Johnson", approvedBy: "", createdAt: new Date("2026-03-09T17:21:04"), approvedAt: null, itemCount: 16, totalQuantity: 380, actions: "" },
        { id: '13', documentNumber: "DOC-1013", type: "Receipt", status: "Completed", sourceWarehouse: "", destinationWarehouse: "East DC Lublin", createdBy: "Sara Lee", approvedBy: "Sara Lee", createdAt: new Date("2026-03-17T11:26:50"), approvedAt: new Date("2026-03-18T05:26:50"), itemCount: 4, totalQuantity: 144, actions: "" },
        { id: '14', documentNumber: "DOC-1014", type: "Issue", status: "Confirmed", sourceWarehouse: "South Hub Wrocław", destinationWarehouse: "", createdBy: "Anna Kowalska", approvedBy: "Michael Johnson", createdAt: new Date("2026-03-17T14:32:16"), approvedAt: new Date("2026-03-17T18:32:16"), itemCount: 10, totalQuantity: 280, actions: "" },
        { id: '15', documentNumber: "DOC-1015", type: "Adjustment", status: "Draft", sourceWarehouse: "", destinationWarehouse: "", createdBy: "Sara Lee", approvedBy: "", createdAt: new Date("2026-03-03T15:50:49"), approvedAt: null, itemCount: 5, totalQuantity: 124, actions: "" },
        { id: '16', documentNumber: "DOC-1016", type: "Transfer", status: "Confirmed", sourceWarehouse: "Regional DC Kraków", destinationWarehouse: "North Hub Gdańsk", createdBy: "Tom Brown", approvedBy: "Anna Kowalska", createdAt: new Date("2026-03-09T22:58:06"), approvedAt: new Date("2026-03-10T08:58:06"), itemCount: 9, totalQuantity: 218, actions: "" },
        { id: '17', documentNumber: "DOC-1017", type: "Receipt", status: "Draft", sourceWarehouse: "", destinationWarehouse: "North Hub Gdańsk", createdBy: "Anna Kowalska", approvedBy: "", createdAt: new Date("2026-02-18T12:59:48"), approvedAt: null, itemCount: 4, totalQuantity: 109, actions: "" },
        { id: '18', documentNumber: "DOC-1018", type: "Transfer", status: "Completed", sourceWarehouse: "North Hub Gdańsk", destinationWarehouse: "Central Warehouse Warsaw", createdBy: "Sara Lee", approvedBy: "John Smith", createdAt: new Date("2026-03-06T14:47:12"), approvedAt: new Date("2026-03-06T21:47:12"), itemCount: 4, totalQuantity: 159, actions: "" },
        { id: '19', documentNumber: "DOC-1019", type: "Transfer", status: "Draft", sourceWarehouse: "Central Warehouse Warsaw", destinationWarehouse: "Regional DC Kraków", createdBy: "John Smith", approvedBy: "", createdAt: new Date("2026-02-18T05:49:36"), approvedAt: null, itemCount: 19, totalQuantity: 449, actions: "" },
        { id: '20', documentNumber: "DOC-1020", type: "Adjustment", status: "Cancelled", sourceWarehouse: "", destinationWarehouse: "", createdBy: "Anna Kowalska", approvedBy: "", createdAt: new Date("2026-03-19T01:12:02"), approvedAt: null, itemCount: 6, totalQuantity: 125, actions: "" },
        { id: '21', documentNumber: "DOC-1021", type: "Adjustment", status: "Cancelled", sourceWarehouse: "", destinationWarehouse: "", createdBy: "Anna Kowalska", approvedBy: "", createdAt: new Date("2026-03-17T15:08:04"), approvedAt: null, itemCount: 5, totalQuantity: 138, actions: "" },
        { id: '22', documentNumber: "DOC-1022", type: "Receipt", status: "Completed", sourceWarehouse: "", destinationWarehouse: "North Hub Gdańsk", createdBy: "Sara Lee", approvedBy: "Tom Brown", createdAt: new Date("2026-03-11T15:05:25"), approvedAt: new Date("2026-03-13T10:05:25"), itemCount: 8, totalQuantity: 190, actions: "" },
        { id: '23', documentNumber: "DOC-1023", type: "Issue", status: "Cancelled", sourceWarehouse: "Regional DC Kraków", destinationWarehouse: "", createdBy: "Anna Kowalska", approvedBy: "", createdAt: new Date("2026-03-17T11:54:34"), approvedAt: null, itemCount: 12, totalQuantity: 401, actions: "" },
        { id: '24', documentNumber: "DOC-1024", type: "Issue", status: "Draft", sourceWarehouse: "East DC Lublin", destinationWarehouse: "", createdBy: "Tom Brown", approvedBy: "", createdAt: new Date("2026-03-02T20:23:17"), approvedAt: null, itemCount: 20, totalQuantity: 474, actions: "" },
        { id: '25', documentNumber: "DOC-1025", type: "Transfer", status: "Completed", sourceWarehouse: "Central Warehouse Warsaw", destinationWarehouse: "South Hub Wrocław", createdBy: "Sara Lee", approvedBy: "Sara Lee", createdAt: new Date("2026-03-01T20:42:46"), approvedAt: new Date("2026-03-02T03:42:46"), itemCount: 6, totalQuantity: 196, actions: "" },
        { id: '26', documentNumber: "DOC-1026", type: "Receipt", status: "Confirmed", sourceWarehouse: "", destinationWarehouse: "East DC Lublin", createdBy: "John Smith", approvedBy: "Sara Lee", createdAt: new Date("2026-03-08T06:51:28"), approvedAt: new Date("2026-03-09T21:51:28"), itemCount: 14, totalQuantity: 374, actions: "" },
        { id: '27', documentNumber: "DOC-1027", type: "Adjustment", status: "Completed", sourceWarehouse: "", destinationWarehouse: "", createdBy: "Sara Lee", approvedBy: "Tom Brown", createdAt: new Date("2026-03-14T21:27:56"), approvedAt: new Date("2026-03-15T02:27:56"), itemCount: 14, totalQuantity: 458, actions: "" },
        { id: '28', documentNumber: "DOC-1028", type: "Issue", status: "Draft", sourceWarehouse: "South Hub Wrocław", destinationWarehouse: "", createdBy: "Michael Johnson", approvedBy: "", createdAt: new Date("2026-02-22T12:10:38"), approvedAt: null, itemCount: 1, totalQuantity: 33, actions: "" },
        { id: '29', documentNumber: "DOC-1029", type: "Receipt", status: "Completed", sourceWarehouse: "", destinationWarehouse: "Central Warehouse Warsaw", createdBy: "Tom Brown", approvedBy: "Michael Johnson", createdAt: new Date("2026-03-13T02:58:20"), approvedAt: new Date("2026-03-14T14:58:20"), itemCount: 19, totalQuantity: 411, actions: "" },
        { id: '30', documentNumber: "DOC-1030", type: "Transfer", status: "Draft", sourceWarehouse: "North Hub Gdańsk", destinationWarehouse: "Regional DC Kraków", createdBy: "Anna Kowalska", approvedBy: "", createdAt: new Date("2026-03-05T00:06:34"), approvedAt: null, itemCount: 15, totalQuantity: 329, actions: "" },
        { id: '31', documentNumber: "DOC-1031", type: "Transfer", status: "Draft", sourceWarehouse: "East DC Lublin", destinationWarehouse: "Central Warehouse Warsaw", createdBy: "Anna Kowalska", approvedBy: "", createdAt: new Date("2026-03-10T21:57:56"), approvedAt: null, itemCount: 4, totalQuantity: 149, actions: "" },
        { id: '32', documentNumber: "DOC-1032", type: "Receipt", status: "Draft", sourceWarehouse: "", destinationWarehouse: "North Hub Gdańsk", createdBy: "John Smith", approvedBy: "", createdAt: new Date("2026-03-11T13:52:09"), approvedAt: null, itemCount: 20, totalQuantity: 572, actions: "" },
        { id: '33', documentNumber: "DOC-1033", type: "Receipt", status: "Confirmed", sourceWarehouse: "", destinationWarehouse: "East DC Lublin", createdBy: "Michael Johnson", approvedBy: "Anna Kowalska", createdAt: new Date("2026-02-22T01:59:05"), approvedAt: new Date("2026-02-22T17:59:05"), itemCount: 2, totalQuantity: 51, actions: "" }
    ];

    addDocument(document: CreateDocument) {
        let newId = this.documents.length > 0
            ? Math.max(...this.documents.map(d => Number(d.id))) + 1
            : 0;
        let newDocument: Document = {
            ...document,
            id: (newId).toString(),
            createdAt: new Date(),
            status: DocumentStatus.Draft
        }
        return newDocument;
    }
    getDocument(id: string): Document {
      return this.documents.find(d => d.id === id) as unknown as Document;
    }
}