import { DocumentStatus } from "../../../core/enums/documentStatus";

export interface CreateDocument {
    number: string;
    documentDate: Date;
    type: DocumentType;
    notes?: string;
    sourceWarehouseId?: string;
    sourceWarehouseName?: string;
    targetWarehouseId?: string;
    targetWarehouseName?: string;
}