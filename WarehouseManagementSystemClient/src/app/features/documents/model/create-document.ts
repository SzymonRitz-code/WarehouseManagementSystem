import { DocumentStatus } from "../../../core/enums/documentStatus";
import { DocumentItem } from "./document-item";

export interface CreateDocument {
    number: string;
    documentDate: Date;
    type: DocumentType;
    notes?: string;
    sourceWarehouseId?: string;
    sourceWarehouseName?: string;
    targetWarehouseId?: string;
    targetWarehouseName?: string;
    documentItems: DocumentItem[];
}