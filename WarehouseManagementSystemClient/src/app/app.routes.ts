import { Routes } from '@angular/router';
import { AppLayoutComponent } from './layout/app-layout/app-layout.component';
import { HomeComponent } from './features/home/home.component';
import { ProductListComponent } from './features/products/pages/product-list/product-list.component';
import { ProductDetailComponent } from './features/products/pages/product-detail/product-detail.component';
import { WarehouseListComponent } from './features/warehouses/pages/warehouse-list/warehouse-list.component';
import { WarehouseFormComponent } from './features/warehouses/pages/warehouse-form/warehouse-form.component';
import { WarehouseDetailComponent } from './features/warehouses/pages/warehouse-detail/warehouse-detail.component';
import { ZoneListComponent } from './features/zones/pages/zone-list/zone-list.component';
import { ProductFormComponent } from './features/products/pages/product-form/product-form.component';
import { StockListComponent } from './features/stocks/pages/stock-list/stock-list.component';
import { StockAvailabilityComponent } from './features/stocks/pages/stock-availability/stock-availability.component';
import { ReservationsComponent } from './features/stocks/pages/reservations/reservations.component';
import { StockMoveComponent } from './features/stocks/pages/stock-move/stock-move.component';
import { AuditLogListComponent } from './features/audit/audit-log-list/audit-log-list.component';
import { DocumentListComponent } from './features/documents/pages/document-list/document-list.component';
import { AdjustmentListComponent } from './features/inventory-adjustments/adjustment-list/adjustment-list.component';
import { AdjustmentCreateComponent } from './features/inventory-adjustments/adjustment-form/adjustment-form.component';
import { ZoneFormComponent } from './features/zones/pages/zone-form/zone-form.component';
import { ZoneDetailComponent } from './features/zones/pages/zone-detail/zone-detail.component';
import { DocumentFormComponent } from './features/documents/pages/document-form/document-form.component';
import { DocumentDetailComponent } from './features/documents/pages/document-detail/document-detail.component';
import { DocumentItemsComponent } from './features/documents/pages/document-items/document-items-list/document-items.component';
import { UserListComponent } from './features/users/pages/user-list/user-list.component';
import { UserFormComponent } from './features/users/pages/user-form/user-form.component';
import { UserDetailComponent } from './features/users/pages/user-detail/user-detail.component';
import { ProductBatchListComponent } from './features/products/pages/product-batch/product-batch-list/product-batch-list.component';
import { ProductBatchFormComponent } from './features/products/pages/product-batch/product-batch-form/product-batch-form.component';
import { ProductBatchDetailComponent } from './features/products/pages/product-batch/product-batch-detail/product-batch-detail.component';


export const routes: Routes = [
  {
    path: '',
    component: AppLayoutComponent,
    children: [
      //Home
      { path: '', redirectTo: 'home', pathMatch: 'full' },
      { path: 'home', component: HomeComponent, title: 'WMS | Dashboard' },

      //Audit
      {
        path: 'audit',
        component: AuditLogListComponent
      },
      // Documents
      {
        path: 'documents',
        children: [
          { path: '', component: DocumentListComponent },
          { path: 'form', component: DocumentFormComponent },
          { path: 'form/:id', component: DocumentFormComponent },
          { path: 'detail/:id', component: DocumentDetailComponent },
          { path: 'items', component: DocumentItemsComponent }
        ]
      },

      // Inventory Adjustments
      {
        path: 'adjustments',
        children: [
          { path: '', component: AdjustmentListComponent },
          { path: 'form', component: AdjustmentCreateComponent }
        ]
      },

      // Products
      {
        path: 'products',
        children: [
          { path: '', component: ProductListComponent },
          { path: 'detail/:id', component: ProductDetailComponent },
          { path: 'form', component: ProductFormComponent },
          { path: 'form/:id', component: ProductFormComponent },
          {
            path: ':id/batches', children: [
              { path: '', component: ProductBatchListComponent },
              { path: 'form', component: ProductBatchFormComponent },
              { path: 'form/:batchId', component: ProductBatchFormComponent },
              { path: 'detail/:batchId', component: ProductBatchDetailComponent }
            ]
          }
        ],
      },

      // Stocks
      {
        path: 'stocks',
        children: [
          { path: '', component: StockListComponent },
          { path: 'availability', component: StockAvailabilityComponent },
          { path: 'reservations', component: ReservationsComponent },
          { path: 'move', component: StockMoveComponent }
        ],
      },

      // Users
      {
        path: 'users',
        children: [
          { path: '', component: UserListComponent },
          { path: 'form', component: UserFormComponent },
          { path: 'form/:id', component: UserFormComponent },
          { path: 'detail/:id', component: UserDetailComponent }
        ],
      },

      // Warehouses
      {
        path: 'warehouses',
        children: [
          { path: '', component: WarehouseListComponent },
          { path: 'form', component: WarehouseFormComponent },
          { path: 'form/:id', component: WarehouseFormComponent },
          { path: 'detail/:id', component: WarehouseDetailComponent },
          { path: 'zones', component: ZoneListComponent }
        ],
      },

      // Zones
      {
        path: 'zones',
        children: [
          { path: '', component: ZoneListComponent },
          { path: 'form', component: ZoneFormComponent },
          { path: 'form/:id', component: ZoneFormComponent },
          { path: 'detail/:id', component: ZoneDetailComponent }
        ],
      },

    ]
  }
];