import { Component } from '@angular/core';
import { ComponentCardComponent } from "../../../../shared/components/common/component-card/component-card.component";
import { TableComponent } from "../../../../shared/components/table/table.component";
import { PageBreadcrumbComponent } from "../../../../shared/components/common/page-breadcrumb/page-breadcrumb.component";

@Component({
  selector: 'app-stock-availability',
  standalone: true,
  imports: [ComponentCardComponent, TableComponent, PageBreadcrumbComponent],
  templateUrl: './stock-availability.component.html'
})
export class StockAvailabilityComponent {


  columns = [
    { key: 'productId', label: 'Product ID', sortable: true },
    { key: 'productName', label: 'Product Name', sortable: true },
    { key: 'warehouseName', label: 'Warehouse', sortable: true },
    { key: 'zoneName', label: 'Zone', sortable: true },
    { key: 'availableQty', label: 'Available Quantity', sortable: true },
    { key: 'reservedQty', label: 'Reserved Quantity', sortable: true },
    { key: 'totalQty', label: 'Total Quantity', sortable: true },
    { key: 'unit', label: 'Unit', sortable: true },
    { key: 'temperatureType', label: 'Temperature Type', sortable: true },
    { key: 'status', label: 'Status', sortable: true },
    { key: 'lastUpdated', label: 'Last Updated', sortable: true, type: 'date' }
  ];

  stockAvailabilities = [
    { productId: 'P001', productName: 'Red Apple', warehouseName: 'Central Warehouse', zoneName: 'Zone A', availableQty: 120, reservedQty: 30, totalQty: 150, unit: 'pcs', temperatureType: 'Ambient', status: 'In Stock', lastUpdated: new Date('2026-03-18') },
    { productId: 'P002', productName: 'Banana Cavendish', warehouseName: 'Central Warehouse', zoneName: 'Zone B', availableQty: 50, reservedQty: 10, totalQty: 60, unit: 'pcs', temperatureType: 'Cold', status: 'Low Stock', lastUpdated: new Date('2026-03-17') },
    { productId: 'P003', productName: 'Valencia Orange', warehouseName: 'East Warehouse', zoneName: 'Zone A', availableQty: 0, reservedQty: 0, totalQty: 0, unit: 'pcs', temperatureType: 'Ambient', status: 'Out of Stock', lastUpdated: new Date('2026-03-16') },
    { productId: 'P004', productName: 'Cherry Tomato', warehouseName: 'East Warehouse', zoneName: 'Zone C', availableQty: 200, reservedQty: 20, totalQty: 220, unit: 'kg', temperatureType: 'Cold', status: 'In Stock', lastUpdated: new Date('2026-03-15') },
    { productId: 'P005', productName: 'English Cucumber', warehouseName: 'West Warehouse', zoneName: 'Zone B', availableQty: 15, reservedQty: 5, totalQty: 20, unit: 'kg', temperatureType: 'Cold', status: 'Low Stock', lastUpdated: new Date('2026-03-14') },
    { productId: 'P006', productName: 'Iceberg Lettuce', warehouseName: 'West Warehouse', zoneName: 'Zone A', availableQty: 80, reservedQty: 10, totalQty: 90, unit: 'pcs', temperatureType: 'Cold', status: 'In Stock', lastUpdated: new Date('2026-03-13') },
    { productId: 'P007', productName: 'Russet Potato', warehouseName: 'Central Warehouse', zoneName: 'Zone C', availableQty: 300, reservedQty: 50, totalQty: 350, unit: 'kg', temperatureType: 'Ambient', status: 'In Stock', lastUpdated: new Date('2026-03-12') },
    { productId: 'P008', productName: 'Organic Carrot', warehouseName: 'East Warehouse', zoneName: 'Zone B', availableQty: 0, reservedQty: 0, totalQty: 0, unit: 'kg', temperatureType: 'Cold', status: 'Out of Stock', lastUpdated: new Date('2026-03-11') },
    { productId: 'P009', productName: 'Yellow Onion', warehouseName: 'West Warehouse', zoneName: 'Zone C', availableQty: 120, reservedQty: 10, totalQty: 130, unit: 'kg', temperatureType: 'Ambient', status: 'In Stock', lastUpdated: new Date('2026-03-10') },
    { productId: 'P010', productName: 'Garlic Bulb', warehouseName: 'Central Warehouse', zoneName: 'Zone A', availableQty: 5, reservedQty: 0, totalQty: 5, unit: 'kg', temperatureType: 'Ambient', status: 'Low Stock', lastUpdated: new Date('2026-03-09') },
    { productId: 'P011', productName: 'Strawberry Pack', warehouseName: 'East Warehouse', zoneName: 'Zone A', availableQty: 40, reservedQty: 5, totalQty: 45, unit: 'kg', temperatureType: 'Cold', status: 'In Stock', lastUpdated: new Date('2026-03-08') },
    { productId: 'P012', productName: 'Blueberry Box', warehouseName: 'West Warehouse', zoneName: 'Zone B', availableQty: 0, reservedQty: 0, totalQty: 0, unit: 'kg', temperatureType: 'Cold', status: 'Out of Stock', lastUpdated: new Date('2026-03-07') },
    { productId: 'P013', productName: 'Mango Ataulfo', warehouseName: 'Central Warehouse', zoneName: 'Zone B', availableQty: 100, reservedQty: 20, totalQty: 120, unit: 'pcs', temperatureType: 'Ambient', status: 'In Stock', lastUpdated: new Date('2026-03-06') },
    { productId: 'P014', productName: 'Pineapple Smoothie', warehouseName: 'East Warehouse', zoneName: 'Zone C', availableQty: 10, reservedQty: 5, totalQty: 15, unit: 'pcs', temperatureType: 'Ambient', status: 'Low Stock', lastUpdated: new Date('2026-03-05') },
    { productId: 'P015', productName: 'Yellow Peach', warehouseName: 'West Warehouse', zoneName: 'Zone A', availableQty: 0, reservedQty: 0, totalQty: 0, unit: 'pcs', temperatureType: 'Ambient', status: 'Out of Stock', lastUpdated: new Date('2026-03-04') },
    { productId: 'P016', productName: 'Bartlett Pear', warehouseName: 'Central Warehouse', zoneName: 'Zone C', availableQty: 75, reservedQty: 10, totalQty: 85, unit: 'pcs', temperatureType: 'Ambient', status: 'In Stock', lastUpdated: new Date('2026-03-03') },
    { productId: 'P017', productName: 'Seedless Grapes', warehouseName: 'East Warehouse', zoneName: 'Zone B', availableQty: 20, reservedQty: 5, totalQty: 25, unit: 'kg', temperatureType: 'Cold', status: 'Low Stock', lastUpdated: new Date('2026-03-02') },
    { productId: 'P018', productName: 'Watermelon Seedless', warehouseName: 'West Warehouse', zoneName: 'Zone C', availableQty: 0, reservedQty: 0, totalQty: 0, unit: 'pcs', temperatureType: 'Ambient', status: 'Out of Stock', lastUpdated: new Date('2026-03-01') },
    { productId: 'P019', productName: 'Cantaloupe Melon', warehouseName: 'Central Warehouse', zoneName: 'Zone A', availableQty: 60, reservedQty: 10, totalQty: 70, unit: 'pcs', temperatureType: 'Ambient', status: 'In Stock', lastUpdated: new Date('2026-02-28') },
    { productId: 'P020', productName: 'Kiwi Green', warehouseName: 'East Warehouse', zoneName: 'Zone C', availableQty: 0, reservedQty: 0, totalQty: 0, unit: 'pcs', temperatureType: 'Cold', status: 'Out of Stock', lastUpdated: new Date('2026-02-27') },
    { productId: 'P021', productName: 'Lemon Eureka', warehouseName: 'West Warehouse', zoneName: 'Zone B', availableQty: 90, reservedQty: 5, totalQty: 95, unit: 'pcs', temperatureType: 'Ambient', status: 'In Stock', lastUpdated: new Date('2026-02-26') },
    { productId: 'P022', productName: 'Lime Persian', warehouseName: 'Central Warehouse', zoneName: 'Zone B', availableQty: 0, reservedQty: 0, totalQty: 0, unit: 'pcs', temperatureType: 'Ambient', status: 'Out of Stock', lastUpdated: new Date('2026-02-25') },
    { productId: 'P023', productName: 'Cherry Bing', warehouseName: 'East Warehouse', zoneName: 'Zone A', availableQty: 35, reservedQty: 5, totalQty: 40, unit: 'kg', temperatureType: 'Cold', status: 'In Stock', lastUpdated: new Date('2026-02-24') },
    { productId: 'P024', productName: 'Plum Black', warehouseName: 'West Warehouse', zoneName: 'Zone C', availableQty: 0, reservedQty: 0, totalQty: 0, unit: 'kg', temperatureType: 'Ambient', status: 'Out of Stock', lastUpdated: new Date('2026-02-23') },
    { productId: 'P025', productName: 'Apricot Royal', warehouseName: 'Central Warehouse', zoneName: 'Zone C', availableQty: 70, reservedQty: 10, totalQty: 80, unit: 'pcs', temperatureType: 'Ambient', status: 'In Stock', lastUpdated: new Date('2026-02-22') },
    { productId: 'P026', productName: 'Fig Brown Turkey', warehouseName: 'East Warehouse', zoneName: 'Zone B', availableQty: 0, reservedQty: 0, totalQty: 0, unit: 'pcs', temperatureType: 'Cold', status: 'Out of Stock', lastUpdated: new Date('2026-02-21') },
    { productId: 'P027', productName: 'Pomegranate Wonderful', warehouseName: 'West Warehouse', zoneName: 'Zone A', availableQty: 45, reservedQty: 5, totalQty: 50, unit: 'pcs', temperatureType: 'Ambient', status: 'In Stock', lastUpdated: new Date('2026-02-20') },
    { productId: 'P028', productName: 'Avocado Hass', warehouseName: 'Central Warehouse', zoneName: 'Zone A', availableQty: 5, reservedQty: 0, totalQty: 5, unit: 'pcs', temperatureType: 'Cold', status: 'Low Stock', lastUpdated: new Date('2026-02-19') },
    { productId: 'P029', productName: 'Green Cabbage', warehouseName: 'East Warehouse', zoneName: 'Zone C', availableQty: 100, reservedQty: 10, totalQty: 110, unit: 'kg', temperatureType: 'Cold', status: 'In Stock', lastUpdated: new Date('2026-02-18') },
    { productId: 'P030', productName: 'Baby Spinach', warehouseName: 'West Warehouse', zoneName: 'Zone B', availableQty: 0, reservedQty: 0, totalQty: 0, unit: 'kg', temperatureType: 'Cold', status: 'Out of Stock', lastUpdated: new Date('2026-02-17') },
    { productId: 'P031', productName: 'Broccoli Crown', warehouseName: 'Central Warehouse', zoneName: 'Zone B', availableQty: 50, reservedQty: 5, totalQty: 55, unit: 'kg', temperatureType: 'Cold', status: 'In Stock', lastUpdated: new Date('2026-02-16') },
    { productId: 'P032', productName: 'Cauliflower White', warehouseName: 'East Warehouse', zoneName: 'Zone A', availableQty: 0, reservedQty: 0, totalQty: 0, unit: 'kg', temperatureType: 'Cold', status: 'Out of Stock', lastUpdated: new Date('2026-02-15') },
    { productId: 'P033', productName: 'Button Mushroom', warehouseName: 'West Warehouse', zoneName: 'Zone C', availableQty: 80, reservedQty: 10, totalQty: 90, unit: 'kg', temperatureType: 'Cold', status: 'In Stock', lastUpdated: new Date('2026-02-14') },
  ];
}
