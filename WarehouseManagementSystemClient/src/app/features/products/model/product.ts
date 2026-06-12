import { CreateProduct } from "./create-product";

export interface Product extends CreateProduct {
    id: string;
    isActive: boolean;
    createdAt: Date;
}
