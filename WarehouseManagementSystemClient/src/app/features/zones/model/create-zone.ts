import { TemperatureType } from "../../../core/enums/temperatureType"

export interface CreateZone {
    code: string;
    name: string;
    temperatureType: TemperatureType;
    isPickingZone: boolean;
    warehouseId: string;
}