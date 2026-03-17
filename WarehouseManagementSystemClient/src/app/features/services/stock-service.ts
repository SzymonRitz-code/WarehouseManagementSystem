import { Injectable } from '@angular/core';
import { Stock } from '../stocks/model/stock';

@Injectable({
  providedIn: 'root',
})
export class StockService {


  stocks: Stock[] = [
    {
      id: '1',
      productSku: 'SKU-1001',
      productName: 'Laptop Dell Latitude',
      warehouse: 'WH-WAW-01',
      zone: 'A1',
      availableQty: 15,
      reservedQty: 3,
      totalQty: 18,
      lastUpdated: new Date('2026-03-10')
    },
    {
      id: '2',
      productSku: 'SKU-1002',
      productName: 'Logitech MX Master 3 Mouse',
      warehouse: 'WH-WAW-01',
      zone: 'A1',
      availableQty: 40,
      reservedQty: 5,
      totalQty: 45,
      lastUpdated: new Date('2026-03-11')
    },
    {
      id: '3',
      productSku: 'SKU-1003',
      productName: 'HP EliteDisplay 24 Monitor',
      warehouse: 'WH-WAW-01',
      zone: 'A2',
      availableQty: 22,
      reservedQty: 6,
      totalQty: 28,
      lastUpdated: new Date('2026-03-11')
    },
    {
      id: '4',
      productSku: 'SKU-1004',
      productName: 'USB-C Docking Station',
      warehouse: 'WH-WAW-01',
      zone: 'A2',
      availableQty: 18,
      reservedQty: 2,
      totalQty: 20,
      lastUpdated: new Date('2026-03-12')
    },
    {
      id: '5',
      productSku: 'SKU-1005',
      productName: 'Mechanical Keyboard',
      warehouse: 'WH-WAW-01',
      zone: 'A3',
      availableQty: 33,
      reservedQty: 7,
      totalQty: 40,
      lastUpdated: new Date('2026-03-12')
    },
    {
      id: '6',
      productSku: 'SKU-1006',
      productName: 'Lenovo ThinkPad Laptop',
      warehouse: 'WH-WAW-01',
      zone: 'A3',
      availableQty: 9,
      reservedQty: 1,
      totalQty: 10,
      lastUpdated: new Date('2026-03-12')
    },
    {
      id: '7',
      productSku: 'SKU-1007',
      productName: 'Samsung SSD 1TB',
      warehouse: 'WH-WAW-02',
      zone: 'B1',
      availableQty: 60,
      reservedQty: 12,
      totalQty: 72,
      lastUpdated: new Date('2026-03-13')
    },
    {
      id: '8',
      productSku: 'SKU-1008',
      productName: 'WD External HDD 2TB',
      warehouse: 'WH-WAW-02',
      zone: 'B1',
      availableQty: 25,
      reservedQty: 3,
      totalQty: 28,
      lastUpdated: new Date('2026-03-13')
    },
    {
      id: '9',
      productSku: 'SKU-1009',
      productName: 'Ethernet Switch 24 Port',
      warehouse: 'WH-WAW-02',
      zone: 'B2',
      availableQty: 14,
      reservedQty: 4,
      totalQty: 18,
      lastUpdated: new Date('2026-03-13')
    },
    {
      id: '10',
      productSku: 'SKU-1010',
      productName: 'TP-Link Router',
      warehouse: 'WH-WAW-02',
      zone: 'B2',
      availableQty: 21,
      reservedQty: 2,
      totalQty: 23,
      lastUpdated: new Date('2026-03-13')
    },
    {
      id: '11',
      productSku: 'SKU-1011',
      productName: 'HDMI Cable 2m',
      warehouse: 'WH-WAW-02',
      zone: 'B3',
      availableQty: 120,
      reservedQty: 30,
      totalQty: 150,
      lastUpdated: new Date('2026-03-14')
    },
    {
      id: '12',
      productSku: 'SKU-1012',
      productName: 'USB-C Cable',
      warehouse: 'WH-WAW-02',
      zone: 'B3',
      availableQty: 95,
      reservedQty: 10,
      totalQty: 105,
      lastUpdated: new Date('2026-03-14')
    },
    {
      id: '13',
      productSku: 'SKU-1013',
      productName: 'Office Chair',
      warehouse: 'WH-KRK-01',
      zone: 'C1',
      availableQty: 12,
      reservedQty: 4,
      totalQty: 16,
      lastUpdated: new Date('2026-03-14')
    },
    {
      id: '14',
      productSku: 'SKU-1014',
      productName: 'Standing Desk',
      warehouse: 'WH-KRK-01',
      zone: 'C1',
      availableQty: 6,
      reservedQty: 1,
      totalQty: 7,
      lastUpdated: new Date('2026-03-14')
    },
    {
      id: '15',
      productSku: 'SKU-1015',
      productName: 'LED Desk Lamp',
      warehouse: 'WH-KRK-01',
      zone: 'C2',
      availableQty: 30,
      reservedQty: 5,
      totalQty: 35,
      lastUpdated: new Date('2026-03-14')
    },
    {
      id: '16',
      productSku: 'SKU-1016',
      productName: 'Webcam Logitech C920',
      warehouse: 'WH-KRK-01',
      zone: 'C2',
      availableQty: 17,
      reservedQty: 3,
      totalQty: 20,
      lastUpdated: new Date('2026-03-14')
    },
    {
      id: '17',
      productSku: 'SKU-1017',
      productName: 'Headset Jabra',
      warehouse: 'WH-KRK-01',
      zone: 'C3',
      availableQty: 26,
      reservedQty: 6,
      totalQty: 32,
      lastUpdated: new Date('2026-03-14')
    },
    {
      id: '18',
      productSku: 'SKU-1018',
      productName: 'Portable Projector',
      warehouse: 'WH-KRK-01',
      zone: 'C3',
      availableQty: 5,
      reservedQty: 1,
      totalQty: 6,
      lastUpdated: new Date('2026-03-14')
    },
    {
      id: '19',
      productSku: 'SKU-1019',
      productName: 'Apple Magic Keyboard',
      warehouse: 'WH-GDN-01',
      zone: 'D1',
      availableQty: 19,
      reservedQty: 2,
      totalQty: 21,
      lastUpdated: new Date('2026-03-14')
    },
    {
      id: '20',
      productSku: 'SKU-1020',
      productName: 'MacBook Pro 14',
      warehouse: 'WH-GDN-01',
      zone: 'D1',
      availableQty: 4,
      reservedQty: 1,
      totalQty: 5,
      lastUpdated: new Date('2026-03-14')
    },
    {
      id: '21',
      productSku: 'SKU-1021',
      productName: 'iPad Air',
      warehouse: 'WH-GDN-01',
      zone: 'D2',
      availableQty: 11,
      reservedQty: 2,
      totalQty: 13,
      lastUpdated: new Date('2026-03-14')
    },
    {
      id: '22',
      productSku: 'SKU-1022',
      productName: 'Apple Pencil',
      warehouse: 'WH-GDN-01',
      zone: 'D2',
      availableQty: 23,
      reservedQty: 4,
      totalQty: 27,
      lastUpdated: new Date('2026-03-14')
    },
    {
      id: '23',
      productSku: 'SKU-1023',
      productName: 'Google Pixel Phone',
      warehouse: 'WH-GDN-01',
      zone: 'D3',
      availableQty: 8,
      reservedQty: 1,
      totalQty: 9,
      lastUpdated: new Date('2026-03-14')
    },
    {
      id: '24',
      productSku: 'SKU-1024',
      productName: 'Samsung Galaxy Phone',
      warehouse: 'WH-GDN-01',
      zone: 'D3',
      availableQty: 10,
      reservedQty: 2,
      totalQty: 12,
      lastUpdated: new Date('2026-03-14')
    },
    {
      id: '25',
      productSku: 'SKU-1025',
      productName: 'Bluetooth Speaker',
      warehouse: 'WH-WAW-01',
      zone: 'A4',
      availableQty: 16,
      reservedQty: 3,
      totalQty: 19,
      lastUpdated: new Date('2026-03-14')
    },
    {
      id: '26',
      productSku: 'SKU-1026',
      productName: 'Smart Watch',
      warehouse: 'WH-WAW-01',
      zone: 'A4',
      availableQty: 13,
      reservedQty: 2,
      totalQty: 15,
      lastUpdated: new Date('2026-03-14')
    },
    {
      id: '27',
      productSku: 'SKU-1027',
      productName: 'Wireless Charger',
      warehouse: 'WH-WAW-01',
      zone: 'A4',
      availableQty: 29,
      reservedQty: 6,
      totalQty: 35,
      lastUpdated: new Date('2026-03-14')
    }
  ];

  
}
