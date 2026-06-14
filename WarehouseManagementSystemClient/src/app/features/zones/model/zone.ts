import { CreateZone } from "./create-zone";

export interface ZoneList extends CreateZone {
    id: string;
    warehouseName?: string;
    stockQty: number;
    createdAt: string;
}

export interface Zone extends CreateZone {
    id: string;
    warehouseName?: string;
}
