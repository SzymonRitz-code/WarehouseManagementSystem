import { CreateWarehouse } from "./create-warehouse";

export interface WarehouseList {
    id: string;
    code: string;
    name: string;
    country: string;
    address: string;
    zonesCount: number;
    totalStock: number;
    totalQty: number;
    isActive: boolean;
    createdAt: Date;
}

export interface Warehouse extends CreateWarehouse {
    id: string;
    isActive: boolean;
}
