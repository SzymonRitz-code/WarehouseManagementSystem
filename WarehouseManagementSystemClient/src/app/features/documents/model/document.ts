import { DocumentStatus } from "../../../core/enums/documentStatus"
import { CreateDocument } from "./create-document"

export interface Document extends CreateDocument {
    id: string;
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