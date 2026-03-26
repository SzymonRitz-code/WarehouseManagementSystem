import { Injectable } from '@angular/core';
import { TemperatureType } from '../../core/enums/temperatureType';
import { Zone } from '../zones/model/zone';
import { CreateZone } from '../zones/model/create-zone';

@Injectable({
  providedIn: 'root',
})
export class ZoneService {



  zones: Zone[] = [
    {
      id: '1',
      warehouseName: 'Main Warehouse',
      stockQty: 1250,
      createdAt: '2025-01-10T08:00:00Z',
      code: 'A-01',
      name: 'Receiving Dock 1',
      temperatureType: TemperatureType.Ambient,
      isPickingZone: false,
      warehouseId: '1'
    },
    {
      id: '2',
      warehouseName: 'Main Warehouse',
      stockQty: 980,
      createdAt: '2025-01-10T08:10:00Z',
      code: 'A-02',
      name: 'Receiving Dock 2',
      temperatureType: TemperatureType.Ambient,
      isPickingZone: false,
      warehouseId: '1'
    },
    {
      id: '3',
      warehouseName: 'Main Warehouse',
      stockQty: 1500,
      createdAt: '2025-01-11T09:00:00Z',
      code: 'A-03',
      name: 'Bulk Storage A',
      temperatureType: TemperatureType.Ambient,
      isPickingZone: false,
      warehouseId: '2'
    },
    {
      id: '4',
      warehouseName: 'Main Warehouse',
      stockQty: 1320,
      createdAt: '2025-01-11T09:10:00Z',
      code: 'A-04',
      name: 'Bulk Storage B',
      temperatureType: TemperatureType.Ambient,
      isPickingZone: false,
      warehouseId: '2'
    },
    {
      id: '5',
      warehouseName: 'Main Warehouse',
      stockQty: 540,
      createdAt: '2025-01-12T10:00:00Z',
      code: 'A-05',
      name: 'Picking Zone A1',
      temperatureType: TemperatureType.Ambient,
      isPickingZone: true,
      warehouseId: '3'
    },
    {
      id: '6',
      warehouseName: 'Main Warehouse',
      stockQty: 610,
      createdAt: '2025-01-12T10:10:00Z',
      code: 'A-06',
      name: 'Picking Zone A2',
      temperatureType: TemperatureType.Ambient,
      isPickingZone: true,
      warehouseId: '3'
    },
    {
      id: '7',
      warehouseName: 'Main Warehouse',
      stockQty: 300,
      createdAt: '2025-01-13T11:00:00Z',
      code: 'A-07',
      name: 'Packing Area',
      temperatureType: TemperatureType.Ambient,
      isPickingZone: true,
      warehouseId: '4'
    },
    {
      id: '8',
      warehouseName: 'Main Warehouse',
      stockQty: 200,
      createdAt: '2025-01-13T11:30:00Z',
      code: 'A-08',
      name: 'Returns Processing',
      temperatureType: TemperatureType.Ambient,
      isPickingZone: false,
      warehouseId: '4'
    },
    {
      id: '9',
      warehouseName: 'Main Warehouse',
      stockQty: 890,
      createdAt: '2025-01-14T12:00:00Z',
      code: 'A-09',
      name: 'Overflow Storage',
      temperatureType: TemperatureType.Ambient,
      isPickingZone: false,
      warehouseId: '4'
    },

    // COLD STORAGE

    {
      id: '10',
      warehouseName: 'Cold Warehouse',
      stockQty: 620,
      createdAt: '2025-02-01T07:00:00Z',
      code: 'C-01',
      name: 'Cold Receiving',
      temperatureType: TemperatureType.Cold,
      isPickingZone: false,
      warehouseId: '2'
    },
    {
      id: '11',
      warehouseName: 'Cold Warehouse',
      stockQty: 740,
      createdAt: '2025-02-01T07:15:00Z',
      code: 'C-02',
      name: 'Cold Storage A',
      temperatureType: TemperatureType.Cold,
      isPickingZone: false,
      warehouseId: '2'
    },
    {
      id: '12',
      warehouseName: 'Cold Warehouse',
      stockQty: 680,
      createdAt: '2025-02-02T08:00:00Z',
      code: 'C-03',
      name: 'Cold Storage B',
      temperatureType: TemperatureType.Cold,
      isPickingZone: false,
      warehouseId: '3'
    },
    {
      id: '13',
      warehouseName: 'Cold Warehouse',
      stockQty: 420,
      createdAt: '2025-02-02T08:20:00Z',
      code: 'C-04',
      name: 'Cold Picking A',
      temperatureType: TemperatureType.Cold,
      isPickingZone: true,
      warehouseId: '3'
    },
    {
      id: '14',
      warehouseName: 'Cold Warehouse',
      stockQty: 390,
      createdAt: '2025-02-03T09:00:00Z',
      code: 'C-05',
      name: 'Cold Picking B',
      temperatureType: TemperatureType.Cold,
      isPickingZone: true,
      warehouseId: '1'
    },
    {
      id: '15',
      warehouseName: 'Cold Warehouse',
      stockQty: 150,
      createdAt: '2025-02-03T09:30:00Z',
      code: 'C-06',
      name: 'Cold Packing',
      temperatureType: TemperatureType.Cold,
      isPickingZone: true,
      warehouseId: '1'
    },
    {
      id: '16',
      warehouseName: 'Cold Warehouse',
      stockQty: 100,
      createdAt: '2025-02-04T10:00:00Z',
      code: 'C-07',
      name: 'Cold Returns',
      temperatureType: TemperatureType.Cold,
      isPickingZone: false,
      warehouseId: '1'
    },

    // FROZEN

    {
      id: '17',
      warehouseName: 'Frozen Warehouse',
      stockQty: 500,
      createdAt: '2025-02-10T06:00:00Z',
      code: 'F-01',
      name: 'Frozen Receiving',
      temperatureType: TemperatureType.Frozen,
      isPickingZone: false,
      warehouseId: '4'
    },
    {
      id: '18',
      warehouseName: 'Frozen Warehouse',
      stockQty: 800,
      createdAt: '2025-02-10T06:20:00Z',
      code: 'F-02',
      name: 'Frozen Bulk Storage',
      temperatureType: TemperatureType.Frozen,
      isPickingZone: false,
      warehouseId: '4'
    },
    {
      id: '19',
      warehouseName: 'Frozen Warehouse',
      stockQty: 350,
      createdAt: '2025-02-11T07:00:00Z',
      code: 'F-03',
      name: 'Frozen Picking A',
      temperatureType: TemperatureType.Frozen,
      isPickingZone: true,
      warehouseId: '4'
    },
    {
      id: '20',
      warehouseName: 'Frozen Warehouse',
      stockQty: 320,
      createdAt: '2025-02-11T07:15:00Z',
      code: 'F-04',
      name: 'Frozen Picking B',
      temperatureType: TemperatureType.Frozen,
      isPickingZone: true,
      warehouseId: '7'
    },
    {
      id: '21',
      warehouseName: 'Frozen Warehouse',
      stockQty: 180,
      createdAt: '2025-02-12T08:00:00Z',
      code: 'F-05',
      name: 'Frozen Packing',
      temperatureType: TemperatureType.Frozen,
      isPickingZone: true,
      warehouseId: '7'
    },
    {
      id: '22',
      warehouseName: 'Frozen Warehouse',
      stockQty: 90,
      createdAt: '2025-02-12T08:30:00Z',
      code: 'F-06',
      name: 'Frozen Returns',
      temperatureType: TemperatureType.Frozen,
      isPickingZone: false,
      warehouseId: '8'
    },

    // EXTRA / SPECIAL ZONES

    {
      id: '23',
      warehouseName: 'Main Warehouse',
      stockQty: 75,
      createdAt: '2025-03-01T09:00:00Z',
      code: 'A-10',
      name: 'Quality Control',
      temperatureType: TemperatureType.Ambient,
      isPickingZone: false,
      warehouseId: '8'
    },
    {
      id: '24',
      warehouseName: 'Main Warehouse',
      stockQty: 60,
      createdAt: '2025-03-01T09:30:00Z',
      code: 'A-11',
      name: 'Damaged Goods',
      temperatureType: TemperatureType.Ambient,
      isPickingZone: false,
      warehouseId: '9'
    },
    {
      id: '25',
      warehouseName: 'Cold Warehouse',
      stockQty: 55,
      createdAt: '2025-03-02T10:00:00Z',
      code: 'C-08',
      name: 'Cold QC',
      temperatureType: TemperatureType.Cold,
      isPickingZone: false,
      warehouseId: '9'
    },
    {
      id: '26',
      warehouseName: 'Frozen Warehouse',
      stockQty: 40,
      createdAt: '2025-03-02T10:30:00Z',
      code: 'F-07',
      name: 'Frozen QC',
      temperatureType: TemperatureType.Frozen,
      isPickingZone: false,
      warehouseId: '9'
    },
    {
      id: '27',
      warehouseName: 'Main Warehouse',
      stockQty: 210,
      createdAt: '2025-03-03T11:00:00Z',
      code: 'A-12',
      name: 'Cross Docking Area',
      temperatureType: TemperatureType.Ambient,
      isPickingZone: true,
      warehouseId: '9'
    }
  ];

  addZone(zone: CreateZone) {
    let newId = this.zones.length > 0
      ? Math.max(...this.zones.map(z => Number(z.id))) + 1
      : 0;
    let zoneToAdd: Zone = {
      ...zone,
      id: newId.toString()
    };
    this.zones.push(zoneToAdd)
    return zoneToAdd;
  }
  getZone(id: string): Zone {
    return this.zones.find(z => z.id === id)!;
  }
}
