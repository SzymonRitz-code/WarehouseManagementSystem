export interface AuditLog {
    id: string;
    entityName: string;
    entityId: string;
    operation: string;
    oldValues?: string;
    newValues?: string;
    performedAt: Date;
    ipAddress?: string;
    performedById: string;
    performedByName: string;
    performedByEmail: string;
}
