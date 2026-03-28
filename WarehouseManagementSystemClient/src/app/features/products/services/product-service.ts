import { Injectable } from '@angular/core';
import { Product } from '../model/product';
import { CreateProduct } from '../model/create-product';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { Observable } from 'rxjs';
import { UnitOfMeasure } from '../../../core/enums/unitOfMeasure';

@Injectable({
  providedIn: 'root',
})
export class ProductService {

  private apiUrl: string = environment.apiUrl
  constructor(private http: HttpClient) { }

  products = [
    {
      id: '1',
      name: 'Laptop Dell Latitude 5540',
      sku: 'IT-LAP-001',
      description: 'Business laptop 15 inch',
      unit: UnitOfMeasure.Piece,
      isActive: true,
      weight: 1.8,
      volume: 0.003,
      CreatedAt: new Date('2026-02-15T08:12:00')
    },
    {
      id: '2',
      name: 'Wireless Mouse Logitech M185',
      sku: 'IT-MOU-002',
      description: 'Wireless optical mouse',
      unit: UnitOfMeasure.Piece,
      isActive: true,
      weight: 0.1,
      volume: 0.0005,
      CreatedAt: new Date('2026-02-16T09:20:00')
    },
    {
      id: '3',
      name: 'Office Chair Ergonomic Pro',
      sku: 'FUR-CHA-003',
      description: 'Ergonomic office chair',
      unit: UnitOfMeasure.Piece,
      isActive: true,
      weight: 12,
      volume: 0.15,
      CreatedAt: new Date('2026-02-17T10:45:00')
    },
    {
      id: '4',
      name: 'Wooden Desk 140cm',
      sku: 'FUR-DES-004',
      description: 'Office desk wooden',
      unit: UnitOfMeasure.Piece,
      isActive: true,
      weight: 25,
      volume: 0.3,
      CreatedAt: new Date('2026-02-18T11:30:00')
    },
    {
      id: '5',
      name: 'Printer Paper A4 80gsm',
      sku: 'OFF-PAP-005',
      description: 'A4 paper box 5000 sheets',
      unit: UnitOfMeasure.Box,
      isActive: true,
      weight: 12.5,
      volume: 0.04,
      CreatedAt: new Date('2026-02-19T08:15:00')
    },
    {
      id: '6',
      name: 'Industrial Pallet EUR',
      sku: 'LOG-PAL-006',
      description: 'Standard EUR pallet',
      unit: UnitOfMeasure.Pallet,
      isActive: true,
      weight: 25,
      volume: 1.2,
      CreatedAt: new Date('2026-02-20T09:10:00')
    },
    {
      id: '7',
      name: 'Steel Screws M8',
      sku: 'MAT-SCR-007',
      description: 'Steel screws pack',
      unit: UnitOfMeasure.Kilogram,
      isActive: true,
      weight: 1,
      volume: 0.001,
      CreatedAt: new Date('2026-02-21T12:00:00')
    },
    {
      id: '8',
      name: 'Copper Wire 2mm',
      sku: 'MAT-WIR-008',
      description: 'Copper cable',
      unit: UnitOfMeasure.Meter,
      isActive: true,
      weight: 0.05,
      volume: 0.0002,
      CreatedAt: new Date('2026-02-22T13:40:00')
    },
    {
      id: '9',
      name: 'Engine Oil 5W30',
      sku: 'AUT-OIL-009',
      description: 'Synthetic engine oil',
      unit: UnitOfMeasure.Liter,
      isActive: true,
      weight: 0.9,
      volume: 0.001,
      CreatedAt: new Date('2026-02-23T14:20:00')
    },
    {
      id: '10',
      name: 'Cleaning Liquid',
      sku: 'CHE-LIQ-010',
      description: 'Multi-surface cleaner',
      unit: UnitOfMeasure.Liter,
      isActive: true,
      weight: 1,
      volume: 0.001,
      CreatedAt: new Date('2026-02-24T15:30:00')
    },

    // skracam komentarze żeby było czytelniej

    {
      id: '11',
      name: 'Plastic Container 20L',
      sku: 'CON-PLA-011',
      description: 'Storage container',
      unit: UnitOfMeasure.Piece,
      isActive: true,
      weight: 1.2,
      volume: 0.02,
      CreatedAt: new Date('2026-02-25T09:00:00')
    },
    {
      id: '12',
      name: 'LED Light Bulb 12W',
      sku: 'ELE-LIG-012',
      description: 'Energy saving bulb',
      unit: UnitOfMeasure.Piece,
      isActive: true,
      weight: 0.2,
      volume: 0.0003,
      CreatedAt: new Date('2026-02-26T10:10:00')
    },
    {
      id: '13',
      name: 'Safety Gloves',
      sku: 'SAF-GLO-013',
      description: 'Protective gloves',
      unit: UnitOfMeasure.Piece,
      isActive: true,
      weight: 0.05,
      volume: 0.0002,
      CreatedAt: new Date('2026-02-27T11:20:00')
    },
    {
      id: '14',
      name: 'Steel Beam 3m',
      sku: 'MAT-BEA-014',
      description: 'Construction beam',
      unit: UnitOfMeasure.Meter,
      isActive: true,
      weight: 20,
      volume: 0.1,
      CreatedAt: new Date('2026-02-28T12:30:00')
    },
    {
      id: '15',
      name: 'Ceramic Tiles Pack',
      sku: 'MAT-TIL-015',
      description: 'Floor tiles',
      unit: UnitOfMeasure.Box,
      isActive: true,
      weight: 18,
      volume: 0.05,
      CreatedAt: new Date('2026-03-01T08:40:00')
    },

    // dociągamy do 33

    ...Array.from({ length: 18 }, (_, i) => ({
      id: (16 + i).toString(),
      name: `Generic Product ${16 + i}`,
      sku: `GEN-${100 + i}`,
      description: 'Standard warehouse item',
      unit: Object.values(UnitOfMeasure)[i % 10],
      isActive: i % 5 !== 0,
      weight: Number((Math.random() * 20 + 0.1).toFixed(2)),
      volume: Number((Math.random() * 0.5 + 0.001).toFixed(4)),
      CreatedAt: new Date(`2026-03-${(i % 10) + 1}T0${i % 10}:00:00`)
    }))
  ]; 


  getProducts(): Observable<Product[]> {
    return this.http.get<Product[]>(`${this.apiUrl}/products`);
  }
  getProduct(id: string) {
    return this.http.get<Product>(`${environment.apiUrl}/products/${id}`)
  }

  addProduct(product: CreateProduct) {
    console.log(product)
    return this.http.post<Product>(`${this.apiUrl}/products`, product)
  }

}
