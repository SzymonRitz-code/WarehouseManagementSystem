import { Component } from '@angular/core';
import { ComponentCardComponent } from "../../../shared/components/common/component-card/component-card.component";
import { TableComponent } from "../../../shared/components/table/table.component";
import { Router } from '@angular/router';

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [ComponentCardComponent, TableComponent],
  templateUrl: './product-list.component.html'
})
export class ProductListComponent {
  constructor(private router: Router){}
  products = [
    { id: 1, name: 'Laptop Pro 15"', sku: 'LAP-001', category: 'Electronics', price: 1500, stock: 25 },
    { id: 2, name: 'Wireless Mouse', sku: 'MOU-002', category: 'Electronics', price: 45, stock: 100 },
    { id: 3, name: 'Mechanical Keyboard', sku: 'KEY-003', category: 'Electronics', price: 120, stock: 60 },
    { id: 4, name: 'HD Monitor 27"', sku: 'MON-004', category: 'Electronics', price: 320, stock: 30 },
    { id: 5, name: 'USB-C Hub', sku: 'HUB-005', category: 'Accessories', price: 35, stock: 200 },
    { id: 6, name: 'External SSD 1TB', sku: 'SSD-006', category: 'Storage', price: 180, stock: 40 },
    { id: 7, name: 'Gaming Chair', sku: 'CHA-007', category: 'Furniture', price: 250, stock: 15 },
    { id: 8, name: 'Webcam 1080p', sku: 'CAM-008', category: 'Electronics', price: 70, stock: 80 },
    { id: 9, name: 'Noise Cancelling Headphones', sku: 'HPH-009', category: 'Electronics', price: 220, stock: 50 },
    { id: 10, name: 'Smartphone X', sku: 'PHN-010', category: 'Electronics', price: 900, stock: 35 },
    { id: 11, name: 'Tablet S 11"', sku: 'TAB-011', category: 'Electronics', price: 600, stock: 45 },
    { id: 12, name: 'Portable Speaker', sku: 'SPK-012', category: 'Electronics', price: 80, stock: 70 },
    { id: 13, name: 'Fitness Tracker', sku: 'FIT-013', category: 'Wearables', price: 120, stock: 90 },
    { id: 14, name: 'Smartwatch Pro', sku: 'SW-014', category: 'Wearables', price: 250, stock: 40 },
    { id: 15, name: 'E-book Reader', sku: 'ERE-015', category: 'Electronics', price: 110, stock: 60 },
    { id: 16, name: 'Desk Lamp LED', sku: 'LMP-016', category: 'Furniture', price: 45, stock: 100 },
    { id: 17, name: 'Wireless Charger', sku: 'CHG-017', category: 'Accessories', price: 30, stock: 150 },
    { id: 18, name: 'Action Camera 4K', sku: 'CAM-018', category: 'Electronics', price: 350, stock: 25 },
    { id: 19, name: 'VR Headset', sku: 'VR-019', category: 'Electronics', price: 400, stock: 20 },
    { id: 20, name: 'Laptop Stand', sku: 'STN-020', category: 'Accessories', price: 55, stock: 80 },
    { id: 21, name: 'Wireless Earbuds', sku: 'EAR-021', category: 'Electronics', price: 130, stock: 70 },
    { id: 22, name: 'Office Chair', sku: 'CHA-022', category: 'Furniture', price: 180, stock: 25 },
    { id: 23, name: 'HDMI Cable 2m', sku: 'CAB-023', category: 'Accessories', price: 15, stock: 300 }
  ];

  columns = [
    { key: 'id', label: 'ID', sortable: true },
    { key: 'name', label: 'Name', sortable: true },
    { key: 'sku', label: 'SKU', sortable: true }
  ];
  goToForm() {
    this.router.navigate(['/products/form']);
  }
}
