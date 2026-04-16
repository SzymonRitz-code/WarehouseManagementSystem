
export interface CreateBatch {
    batchNumber: string;
    productId: string;
    expirationDate: Date | null;
    manufacturedDate: Date | null;
    quantity: number;
}
