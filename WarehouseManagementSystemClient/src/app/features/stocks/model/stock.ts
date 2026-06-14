export interface Stock {
    id: string;
    productId: string;
    productSku: string;
    productName: string;
    productBatchId?: string;
    productBatchNumber?: string;
    warehouseId: string;
    warehouseName: string;
    zoneId: string;
    zoneName: string;
    unit: string;
    quantityTotal: number;
    quantityReserved: number;
    quantityAvailable: number;
    lastUpdated: Date;
}
