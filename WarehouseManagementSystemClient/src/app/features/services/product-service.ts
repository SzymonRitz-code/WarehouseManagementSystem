import { Injectable } from '@angular/core';
import { Product } from '../products/model/product';
import { CreateProduct } from '../products/model/create-product';

@Injectable({
  providedIn: 'root',
})
export class ProductService {
  products: Product[] = [
    { id: '1', name: 'Laptop Pro 15"', sku: 'LAP-001' },
    { id: '2', name: 'Wireless Mouse', sku: 'MOU-002' },
    { id: '3', name: 'Mechanical Keyboard', sku: 'KEY-003' },
    { id: '4', name: 'HD Monitor 27"', sku: 'MON-004' },
    { id: '5', name: 'USB-C Hub', sku: 'HUB-005' },
    { id: '6', name: 'External SSD 1TB', sku: 'SSD-006' },
    { id: '7', name: 'Gaming Chair', sku: 'CHA-007' },
    { id: '8', name: 'Webcam 1080p', sku: 'CAM-008' },
    { id: '9', name: 'Noise Cancelling Headphones', sku: 'HPH-009' },
    { id: '10', name: 'Smartphone X', sku: 'PHN-010' },
    { id: '11', name: 'Tablet S 11"', sku: 'TAB-011' },
    { id: '12', name: 'Portable Speaker', sku: 'SPK-012' },
    { id: '13', name: 'Fitness Tracker', sku: 'FIT-013' },
    { id: '14', name: 'Smartwatch Pro', sku: 'SW-014' },
    { id: '15', name: 'E-book Reader', sku: 'ERE-015' },
    { id: '16', name: 'Desk Lamp LED', sku: 'LMP-016' },
    { id: '17', name: 'Wireless Charger', sku: 'CHG-017' },
    { id: '18', name: 'Action Camera 4K', sku: 'CAM-018' },
    { id: '19', name: 'VR Headset', sku: 'VR-019' },
    { id: '20', name: 'Laptop Stand', sku: 'STN-020' },
    { id: '21', name: 'Wireless Earbuds', sku: 'EAR-021' },
    { id: '22', name: 'Office Chair', sku: 'CHA-022' },
    { id: '23', name: 'HDMI Cable 2m', sku: 'CAB-023' }
  ];

   addProduct(product: CreateProduct) {
    // zamiana istniejących ID na number i znalezienie max
    const maxId = this.products.length > 0
      ? Math.max(...this.products.map(p => Number(p.id)))
      : 0;

    const newProduct: Product = {
      ...product,
      id: (maxId + 1).toString()
    };

    this.products.push(newProduct);
    return newProduct;
  }
  getProduct(id: string) {
    return this.products.find(p => p.id === id)
  }
}
