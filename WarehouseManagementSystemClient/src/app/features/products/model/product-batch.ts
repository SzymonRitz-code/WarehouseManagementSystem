import { CreateBatch } from "./product-create-batch";

export interface Batch extends CreateBatch{
    id: string;
    productName: string;
    quantity: number;
    availableQty: number;
    reservedQty: number;
    createdAt: Date;
}

