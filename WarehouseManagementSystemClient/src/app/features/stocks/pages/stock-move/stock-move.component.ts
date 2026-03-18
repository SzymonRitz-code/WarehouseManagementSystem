import { Component } from '@angular/core';
import { ComponentCardComponent } from "../../../../shared/components/common/component-card/component-card.component";
import { TableComponent } from "../../../../shared/components/table/table.component";
import { PageBreadcrumbComponent } from "../../../../shared/components/common/page-breadcrumb/page-breadcrumb.component";

@Component({
  selector: 'app-stock-move',
  standalone: true,
  imports: [ComponentCardComponent, TableComponent, PageBreadcrumbComponent],
  templateUrl: './stock-move.component.html'
})
export class StockMoveComponent {

  columns = [
    { key: 'id', label: 'Move ID', sortable: true },               // unikalny identyfikator ruchu
    { key: 'productId', label: 'Product ID', sortable: true },    // identyfikator produktu
    { key: 'productName', label: 'Product Name', sortable: true },
    { key: 'fromWarehouse', label: 'From Warehouse', sortable: true }, // magazyn źródłowy
    { key: 'fromZone', label: 'From Zone', sortable: true },           // strefa źródłowa
    { key: 'toWarehouse', label: 'To Warehouse', sortable: true },     // magazyn docelowy
    { key: 'toZone', label: 'To Zone', sortable: true },               // strefa docelowa
    { key: 'quantity', label: 'Quantity', sortable: true },           // ilość przenoszona
    { key: 'unit', label: 'Unit', sortable: true },                   // jednostka miary
    { key: 'moveType', label: 'Move Type', sortable: true },          // typ ruchu (In / Out / Transfer)
    { key: 'status', label: 'Status', sortable: true },               // status operacji (Completed / Pending / Cancelled)
    { key: 'movedBy', label: 'Moved By', sortable: true },           // użytkownik/operator wykonujący ruch
    { key: 'movedAt', label: 'Moved At', sortable: true },           // data i czas ruchu
    { key: 'reference', label: 'Reference', sortable: true }         // powiązany dokument (zamówienie, przyjęcie, itp.)
  ];
  stockMoves = [
    { id: 'M001', productId: 'P001', productName: 'Red Apple', fromWarehouse: 'Central Warehouse', fromZone: 'Zone A', toWarehouse: 'East Warehouse', toZone: 'Zone B', quantity: 50, unit: 'pcs', moveType: 'Transfer', status: 'Completed', movedBy: 'John Smith', movedAt: new Date('2026-03-18'), reference: 'TR-1001' },
    { id: 'M002', productId: 'P002', productName: 'Banana Cavendish', fromWarehouse: 'Central Warehouse', fromZone: 'Zone B', toWarehouse: 'West Warehouse', toZone: 'Zone C', quantity: 20, unit: 'pcs', moveType: 'Transfer', status: 'Pending', movedBy: 'Anna Kowalska', movedAt: new Date('2026-03-17'), reference: 'TR-1002' },
    { id: 'M003', productId: 'P003', productName: 'Valencia Orange', fromWarehouse: '', fromZone: '', toWarehouse: 'East Warehouse', toZone: 'Zone A', quantity: 100, unit: 'pcs', moveType: 'In', status: 'Completed', movedBy: 'Piotr Nowak', movedAt: new Date('2026-03-16'), reference: 'GRN-2001' },
    { id: 'M004', productId: 'P004', productName: 'Cherry Tomato', fromWarehouse: 'East Warehouse', fromZone: 'Zone C', toWarehouse: '', toZone: '', quantity: 80, unit: 'kg', moveType: 'Out', status: 'Completed', movedBy: 'Maria Wiśniewska', movedAt: new Date('2026-03-15'), reference: 'SO-3001' },
    { id: 'M005', productId: 'P005', productName: 'English Cucumber', fromWarehouse: 'West Warehouse', fromZone: 'Zone B', toWarehouse: 'Central Warehouse', toZone: 'Zone A', quantity: 15, unit: 'kg', moveType: 'Transfer', status: 'Completed', movedBy: 'Tomasz Zieliński', movedAt: new Date('2026-03-14'), reference: 'TR-1003' },
    { id: 'M006', productId: 'P006', productName: 'Iceberg Lettuce', fromWarehouse: '', fromZone: '', toWarehouse: 'West Warehouse', toZone: 'Zone A', quantity: 60, unit: 'pcs', moveType: 'In', status: 'Completed', movedBy: 'Ewa Lewandowska', movedAt: new Date('2026-03-13'), reference: 'GRN-2002' },
    { id: 'M007', productId: 'P007', productName: 'Russet Potato', fromWarehouse: 'Central Warehouse', fromZone: 'Zone C', toWarehouse: 'East Warehouse', toZone: 'Zone B', quantity: 200, unit: 'kg', moveType: 'Transfer', status: 'Completed', movedBy: 'Krzysztof Mazur', movedAt: new Date('2026-03-12'), reference: 'TR-1004' },
    { id: 'M008', productId: 'P008', productName: 'Organic Carrot', fromWarehouse: 'East Warehouse', fromZone: 'Zone B', toWarehouse: '', toZone: '', quantity: 40, unit: 'kg', moveType: 'Out', status: 'Cancelled', movedBy: 'Agnieszka Kaczmarek', movedAt: new Date('2026-03-11'), reference: 'SO-3002' },
    { id: 'M009', productId: 'P009', productName: 'Yellow Onion', fromWarehouse: '', fromZone: '', toWarehouse: 'West Warehouse', toZone: 'Zone C', quantity: 120, unit: 'kg', moveType: 'In', status: 'Completed', movedBy: 'Michał Piotrowski', movedAt: new Date('2026-03-10'), reference: 'GRN-2003' },
    { id: 'M010', productId: 'P010', productName: 'Garlic Bulb', fromWarehouse: 'Central Warehouse', fromZone: 'Zone A', toWarehouse: '', toZone: '', quantity: 5, unit: 'kg', moveType: 'Out', status: 'Completed', movedBy: 'Paweł Dąbrowski', movedAt: new Date('2026-03-09'), reference: 'SO-3003' },
    { id: 'M011', productId: 'P011', productName: 'Strawberry Pack', fromWarehouse: 'East Warehouse', fromZone: 'Zone A', toWarehouse: 'Central Warehouse', toZone: 'Zone B', quantity: 25, unit: 'kg', moveType: 'Transfer', status: 'Completed', movedBy: 'Katarzyna Piątek', movedAt: new Date('2026-03-08'), reference: 'TR-1005' },
    { id: 'M012', productId: 'P012', productName: 'Blueberry Box', fromWarehouse: '', fromZone: '', toWarehouse: 'West Warehouse', toZone: 'Zone B', quantity: 30, unit: 'kg', moveType: 'In', status: 'Pending', movedBy: 'Łukasz Adamski', movedAt: new Date('2026-03-07'), reference: 'GRN-2004' },
    { id: 'M013', productId: 'P013', productName: 'Mango Ataulfo', fromWarehouse: 'Central Warehouse', fromZone: 'Zone B', toWarehouse: 'East Warehouse', toZone: 'Zone C', quantity: 40, unit: 'pcs', moveType: 'Transfer', status: 'Completed', movedBy: 'Daniel Król', movedAt: new Date('2026-03-06'), reference: 'TR-1006' },
    { id: 'M014', productId: 'P014', productName: 'Pineapple Smoothie', fromWarehouse: 'East Warehouse', fromZone: 'Zone C', toWarehouse: '', toZone: '', quantity: 10, unit: 'pcs', moveType: 'Out', status: 'Pending', movedBy: 'Natalia Baran', movedAt: new Date('2026-03-05'), reference: 'SO-3004' },
    { id: 'M015', productId: 'P015', productName: 'Yellow Peach', fromWarehouse: '', fromZone: '', toWarehouse: 'West Warehouse', toZone: 'Zone A', quantity: 50, unit: 'pcs', moveType: 'In', status: 'Completed', movedBy: 'Sebastian Wójcik', movedAt: new Date('2026-03-04'), reference: 'GRN-2005' },
    { id: 'M016', productId: 'P016', productName: 'Bartlett Pear', fromWarehouse: 'Central Warehouse', fromZone: 'Zone C', toWarehouse: 'West Warehouse', toZone: 'Zone C', quantity: 60, unit: 'pcs', moveType: 'Transfer', status: 'Completed', movedBy: 'Monika Lis', movedAt: new Date('2026-03-03'), reference: 'TR-1007' },
    { id: 'M017', productId: 'P017', productName: 'Seedless Grapes', fromWarehouse: 'East Warehouse', fromZone: 'Zone B', toWarehouse: '', toZone: '', quantity: 25, unit: 'kg', moveType: 'Out', status: 'Completed', movedBy: 'Adam Sikora', movedAt: new Date('2026-03-02'), reference: 'SO-3005' },
    { id: 'M018', productId: 'P018', productName: 'Watermelon Seedless', fromWarehouse: '', fromZone: '', toWarehouse: 'Central Warehouse', toZone: 'Zone A', quantity: 30, unit: 'pcs', moveType: 'In', status: 'Completed', movedBy: 'Karolina Ostrowska', movedAt: new Date('2026-03-01'), reference: 'GRN-2006' },
    { id: 'M019', productId: 'P019', productName: 'Cantaloupe Melon', fromWarehouse: 'Central Warehouse', fromZone: 'Zone A', toWarehouse: 'East Warehouse', toZone: 'Zone A', quantity: 35, unit: 'pcs', moveType: 'Transfer', status: 'Completed', movedBy: 'Mateusz Górski', movedAt: new Date('2026-02-28'), reference: 'TR-1008' },
    { id: 'M020', productId: 'P020', productName: 'Kiwi Green', fromWarehouse: 'East Warehouse', fromZone: 'Zone C', toWarehouse: '', toZone: '', quantity: 20, unit: 'pcs', moveType: 'Out', status: 'Pending', movedBy: 'Julia Pawlak', movedAt: new Date('2026-02-27'), reference: 'SO-3006' }
  ];
}
