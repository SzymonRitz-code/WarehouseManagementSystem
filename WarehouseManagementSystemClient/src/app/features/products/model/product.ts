import { CreateProduct } from "./create-product";

export interface ProductList {
    id: string;
    sku: string;
    name: string;
    unit: string;
    requiresBatch: boolean;
    weight: number;
    volume: number;
    isActive: boolean;
}

export interface Product extends CreateProduct {
    id: string;
    isActive: boolean;
}
