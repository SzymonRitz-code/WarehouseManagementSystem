import { DocumentType } from "../../../core/enums/documentType";
export interface CreateDocument {
    documentDate: Date;
    type: DocumentType;
    notes?: string;
    sourceWarehouseId: string;
    targetWarehouseId?: string;
    items: CreateDocumentItem[];
}

export interface CreateDocumentItem {
    productId: string;
    quantity: number;
    productBatchId?: string;
    sourceZoneId?: string;
    targetZoneId?: string;
}
