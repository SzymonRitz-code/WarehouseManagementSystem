import { CreateWarehouse } from "./create-warehouse";

export interface Warehouse extends CreateWarehouse {
    id: string;
    zonesCount?: number;
    totalStock?: number;
    totalQty?: number;
    isActive: boolean;
    createdAt?: Date;

}