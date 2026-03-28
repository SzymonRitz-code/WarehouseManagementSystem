import { Injectable } from '@angular/core';
import { Warehouse } from '../model/warehouse';
import { CreateWarehouse } from '../model/create-warehouse';
import { environment } from '../../../environments/environment';
import { HttpClient } from '@angular/common/http';

@Injectable({
  providedIn: 'root',
})
export class WarehouseService {
  private apiUrl: string = environment.apiUrl
  constructor(private http: HttpClient) { }

  warehouses: Warehouse[] = [
    {
      id: "1",
      code: "WH-001",
      warehouseName: "Central Warehouse Warsaw",
      country: "Poland",
      addres: "ul. Logistyczna 12, Warsaw",
      zonesCount: 12,
      totalStock: 540,
      totalQty: 18200,
      status: true,
      createdAt: new Date("2024-01-12")
    },
    {
      id: "2",
      code: "WH-002",
      warehouseName: "North Distribution Center",
      country: "Poland",
      addres: "ul. Portowa 8, Gdansk",
      zonesCount: 8,
      totalStock: 320,
      totalQty: 10400,
      status: true,
      createdAt: new Date("2024-02-03")
    },
    {
      id: "3",
      code: "WH-003",
      warehouseName: "South Logistics Hub",
      country: "Poland",
      addres: "ul. Przemyslowa 44, Krakow",
      zonesCount: 10,
      totalStock: 410,
      totalQty: 13300,
      status: true,
      createdAt: new Date("2024-02-18")
    },
    {
      id: "4",
      code: "WH-004",
      warehouseName: "Poznan Storage Facility",
      country: "Poland",
      addres: "ul. Magazynowa 5, Poznan",
      zonesCount: 6,
      totalStock: 210,
      totalQty: 7600,
      status: true,
      createdAt: new Date("2024-03-01")
    },
    {
      id: "5",
      code: "WH-005",
      warehouseName: "Silesia Industrial Warehouse",
      country: "Poland",
      addres: "ul. Hutnicza 19, Katowice",
      zonesCount: 14,
      totalStock: 680,
      totalQty: 22100,
      status: true,
      createdAt: new Date("2024-03-10")
    },
    {
      id: "6",
      code: "WH-006",
      warehouseName: "Berlin Logistics Center",
      country: "Germany",
      addres: "Industriestrasse 15, Berlin",
      zonesCount: 11,
      totalStock: 470,
      totalQty: 15800,
      status: true,
      createdAt: new Date("2024-03-22")
    },
    {
      id: "7",
      code: "WH-007",
      warehouseName: "Munich Distribution Hub",
      country: "Germany",
      addres: "Lagerstrasse 3, Munich",
      zonesCount: 9,
      totalStock: 360,
      totalQty: 11800,
      status: true,
      createdAt: new Date("2024-04-02")
    },
    {
      id: "8",
      code: "WH-008",
      warehouseName: "Hamburg Port Warehouse",
      country: "Germany",
      addres: "Dockstrasse 22, Hamburg",
      zonesCount: 13,
      totalStock: 590,
      totalQty: 19900,
      status: true,
      createdAt: new Date("2024-04-15")
    },
    {
      id: "9",
      code: "WH-009",
      warehouseName: "Prague Storage Center",
      country: "Czech Republic",
      addres: "Logisticka 11, Prague",
      zonesCount: 7,
      totalStock: 240,
      totalQty: 8200,
      status: true,
      createdAt: new Date("2024-04-28")
    },
    {
      id: "10",
      code: "WH-010",
      warehouseName: "Brno Logistics Depot",
      country: "Czech Republic",
      addres: "Prumyslova 6, Brno",
      zonesCount: 5,
      totalStock: 170,
      totalQty: 5400,
      status: false,
      createdAt: new Date("2024-05-09")
    },
    {
      id: "11",
      code: "WH-011",
      warehouseName: "Vienna Central Warehouse",
      country: "Austria",
      addres: "Industriestrasse 4, Vienna",
      zonesCount: 12,
      totalStock: 520,
      totalQty: 17600,
      status: true,
      createdAt: new Date("2024-05-21")
    },
    {
      id: "12",
      code: "WH-012",
      warehouseName: "Salzburg Storage Hub",
      country: "Austria",
      addres: "Lagerweg 9, Salzburg",
      zonesCount: 6,
      totalStock: 200,
      totalQty: 6700,
      status: true,
      createdAt: new Date("2024-06-03")
    },
    {
      id: "13",
      code: "WH-013",
      warehouseName: "Paris Distribution Center",
      country: "France",
      addres: "Rue Logistique 18, Paris",
      zonesCount: 15,
      totalStock: 720,
      totalQty: 24000,
      status: true,
      createdAt: new Date("2024-06-14")
    },
    {
      id: "14",
      code: "WH-014",
      warehouseName: "Lyon Industrial Warehouse",
      country: "France",
      addres: "Zone Industrielle 2, Lyon",
      zonesCount: 10,
      totalStock: 390,
      totalQty: 12900,
      status: true,
      createdAt: new Date("2024-06-27")
    },
    {
      id: "15",
      code: "WH-015",
      warehouseName: "Madrid Logistics Hub",
      country: "Spain",
      addres: "Calle Almacen 7, Madrid",
      zonesCount: 11,
      totalStock: 450,
      totalQty: 15000,
      status: true,
      createdAt: new Date("2024-07-10")
    },
    {
      id: "16",
      code: "WH-016",
      warehouseName: "Barcelona Storage Center",
      country: "Spain",
      addres: "Zona Logistica 13, Barcelona",
      zonesCount: 9,
      totalStock: 340,
      totalQty: 11300,
      status: true,
      createdAt: new Date("2024-07-22")
    },
    {
      id: "17",
      code: "WH-017",
      warehouseName: "Milan Distribution Depot",
      country: "Italy",
      addres: "Via Industria 21, Milan",
      zonesCount: 12,
      totalStock: 510,
      totalQty: 17000,
      status: true,
      createdAt: new Date("2024-08-04")
    },
    {
      id: "18",
      code: "WH-018",
      warehouseName: "Rome Storage Facility",
      country: "Italy",
      addres: "Via Magazzino 10, Rome",
      zonesCount: 8,
      totalStock: 290,
      totalQty: 9600,
      status: true,
      createdAt: new Date("2024-08-16")
    },
    {
      id: "19",
      code: "WH-019",
      warehouseName: "Amsterdam Logistics Center",
      country: "Netherlands",
      addres: "Warehouse Park 3, Amsterdam",
      zonesCount: 10,
      totalStock: 400,
      totalQty: 13200,
      status: true,
      createdAt: new Date("2024-08-28")
    },
    {
      id: "20",
      code: "WH-020",
      warehouseName: "Rotterdam Port Warehouse",
      country: "Netherlands",
      addres: "Dock Area 9, Rotterdam",
      zonesCount: 14,
      totalStock: 610,
      totalQty: 20500,
      status: true,
      createdAt: new Date("2024-09-09")
    },
    {
      id: "21",
      code: "WH-021",
      warehouseName: "Stockholm Storage Hub",
      country: "Sweden",
      addres: "Logistikgatan 5, Stockholm",
      zonesCount: 7,
      totalStock: 260,
      totalQty: 8400,
      status: true,
      createdAt: new Date("2024-09-20")
    },
    {
      id: "22",
      code: "WH-022",
      warehouseName: "Copenhagen Distribution Center",
      country: "Denmark",
      addres: "Warehouse Road 6, Copenhagen",
      zonesCount: 9,
      totalStock: 330,
      totalQty: 11000,
      status: true,
      createdAt: new Date("2024-10-01")
    },
    {
      id: "23",
      code: "WH-023",
      warehouseName: "Helsinki Storage Depot",
      country: "Finland",
      addres: "Varasto 4, Helsinki",
      zonesCount: 6,
      totalStock: 190,
      totalQty: 6200,
      status: false,
      createdAt: new Date("2024-10-12")
    },
    {
      id: "24",
      code: "WH-024",
      warehouseName: "Oslo Logistics Facility",
      country: "Norway",
      addres: "Lagerveien 8, Oslo",
      zonesCount: 8,
      totalStock: 280,
      totalQty: 9100,
      status: true,
      createdAt: new Date("2024-10-25")
    },
    {
      id: "25",
      code: "WH-025",
      warehouseName: "Tallinn Warehouse Center",
      country: "Estonia",
      addres: "Warehouse Street 14, Tallinn",
      zonesCount: 5,
      totalStock: 150,
      totalQty: 4700,
      status: true,
      createdAt: new Date("2024-11-05")
    }
  ];

  addWarehouse(warehouse: CreateWarehouse) {
    let newid = this.warehouses.length > 0
      ? Math.max(...this.warehouses.map(w => Number(w.id))) + 1
      : 0;
    let warehouseToAdd: Warehouse = {
      ...warehouse,
      id: newid.toString(),
      status: true
    }
    console.log(`Warehouse added: ${warehouseToAdd}`)
    this.warehouses.push(warehouseToAdd);
    return warehouseToAdd;
  }

  getWarehouse(id: string) {
    return this.warehouses.find(w => w.id === id)
  }
}
