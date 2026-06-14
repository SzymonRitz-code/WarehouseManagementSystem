import { CreateBatch } from "./product-create-batch";

export interface Batch extends CreateBatch{
    id: string;
    productName: string;
}

export interface BatchList {
    id: string;
    batchNumber: string;
    productName: string;
    manufacturedDate?: Date;
    expirationDate?: Date;
    quantity: number;
    availableQty: number;
    reservedQty: number;
    createdAt: Date;
}

