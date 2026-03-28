export interface AuditLog {
    id: string;
    entityType: string;
    entityId: string;
    action: string;
    changedBy: string;
    timestamp: string;
    details: string;
}