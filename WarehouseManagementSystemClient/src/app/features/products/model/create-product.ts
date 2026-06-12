import { UnitOfMeasure } from "../../../core/enums/unitOfMeasure";

export interface CreateProduct {
    name: string;
    sku: string;
    description: string;
    unit: UnitOfMeasure;
    requiresBatch: boolean;
    weight: number;
    volume: number;
} 
