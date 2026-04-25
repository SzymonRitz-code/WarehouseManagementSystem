import { DocumentStatus } from "../../../core/enums/documentStatus";
import { DocumentItem } from "./document-item";
import { DocumentType } from "../../../core/enums/documentType";
export interface CreateDocument {
    number: string;
    documentDate: Date;
    type: DocumentType;
    notes?: string;
    sourceWarehouseId?: string;
    sourceWarehouseName?: string;
    targetWarehouseId?: string;
    targetWarehouseName?: string;
    items: DocumentItem[];
}