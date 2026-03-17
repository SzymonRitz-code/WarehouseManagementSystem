export interface Stock{
    id: string,
    productSku: string,
    productName: string,
    warehouse: string,
    zone: string,
    availableQty: number,
    reservedQty: number,
    totalQty: number,
    lastUpdated: Date
}