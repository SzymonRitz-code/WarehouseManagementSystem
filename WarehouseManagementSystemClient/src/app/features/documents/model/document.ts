import { DocumentStatus } from "../../../core/enums/documentStatus"
import { CreateDocument } from "./create-document"
import { DocumentItem } from "./document-item";

export interface DocumentList {
    id: string;
    documentNumber?: string;
    type: string;
    status: DocumentStatus;
    sourceWarehouse: string;
    destinationWarehouse?: string;
    createdBy: string;
    approvedBy?: string;
    createdAt: Date;
    approvedAt?: Date;
    itemCount: number;
    totalQuantity: number;
}

export interface Document extends CreateDocument {
    id: string;
    number?: string;
    sourceWarehouseName?: string;
    targetWarehouseName?: string;
    items: DocumentItem[];
    confirmedAt?: Date;
    transferStartedAt?: Date;
    status: DocumentStatus;
    createdAt: Date;
    createdById?: string;
    createdByName?: string;
    confirmedById?: string;
    confirmedByName?: string;
    transferStartedById?: string;
    transferStartedByName?: string;
}
