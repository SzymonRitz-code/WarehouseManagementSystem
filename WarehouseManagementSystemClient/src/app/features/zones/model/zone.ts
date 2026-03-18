import { CreateZone } from "./create-zone";

export interface Zone extends CreateZone {
    id: string;
    warehouseName?: string;
    stockQty?: number;
    createdAt?: string;
}
