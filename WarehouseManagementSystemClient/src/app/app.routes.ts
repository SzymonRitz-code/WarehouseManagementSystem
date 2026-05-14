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
import { UserListComponent } from './features/users/pages/user-list/user-list.component';
import { UserFormComponent } from './features/users/pages/user-form/user-form.component';
import { UserDetailComponent } from './features/users/pages/user-detail/user-detail.component';
import { ProductBatchListComponent } from './features/products/pages/product-batch/product-batch-list/product-batch-list.component';
import { ProductBatchFormComponent } from './features/products/pages/product-batch/product-batch-form/product-batch-form.component';
import { ProductBatchDetailComponent } from './features/products/pages/product-batch/product-batch-detail/product-batch-detail.component';
import { DocumentPendingListComponent } from './features/documents/pages/document-pending-list/document-pending-list.component';
import { SigninFormComponent } from './shared/components/auth/signin-form/signin-form.component';
import { authGuard } from './core/guards/auth-guard';
import { SignoutCallbackComponent } from './shared/components/auth/signout-callback/signout-callback.component';


export const routes: Routes = [
  {
    path: '',
    component: AppLayoutComponent,
    canActivate: [authGuard],
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
          { path: '', component: DocumentListComponent, title: 'WMS | Documents' },
          { path: 'pending', component: DocumentPendingListComponent, title: 'WMS | Pending Documents' },
          { path: 'form', component: DocumentFormComponent, title: 'WMS | Create Document' },
          { path: 'form/:id', component: DocumentFormComponent, title: 'WMS | Edit Document' },
          { path: 'detail/:id', component: DocumentDetailComponent, title: 'WMS | Document Detail' }
        ]
      },

      // Inventory Adjustments
      {
        path: 'adjustments',
        children: [
          { path: '', component: AdjustmentListComponent, title: 'WMS | Adjustments' },
          { path: 'form', component: AdjustmentCreateComponent, title: 'WMS | Create Adjustment' }
        ]
      },

      // Products
      {
        path: 'products',
        children: [
          { path: '', component: ProductListComponent, title: 'WMS | Products' },
          { path: 'detail/:id', component: ProductDetailComponent, title: 'WMS | Product Detail' },
          { path: 'form', component: ProductFormComponent, title: 'WMS | Create Product' },
          { path: 'form/:id', component: ProductFormComponent, title: 'WMS | Edit Product' },
          {
            path: ':id/batches', children: [
              { path: '', component: ProductBatchListComponent, title: 'WMS | Product Batches' },
              { path: 'form', component: ProductBatchFormComponent, title: 'WMS | Create Product Batch' },
              { path: 'form/:batchId', component: ProductBatchFormComponent, title: 'WMS | Edit Product Batch' },
              { path: 'detail/:batchId', component: ProductBatchDetailComponent, title: 'WMS | Product Batch Detail' }
            ]
          }
        ],
      },

      // Stocks
      {
        path: 'stocks',
        children: [
          { path: '', component: StockListComponent, title: 'WMS | Stocks' },
          { path: 'availability', component: StockAvailabilityComponent, title: 'WMS | Stock Availability' },
          { path: 'reservations', component: ReservationsComponent, title: 'WMS | Reservations' },
          { path: 'move', component: StockMoveComponent, title: 'WMS | Move Stock' }
        ],
      },

      // Users
      {
        path: 'users',
        children: [
          { path: '', component: UserListComponent, title: 'WMS | Users' },
          { path: 'form', component: UserFormComponent, title: 'WMS | Create User' },
          { path: 'form/:id', component: UserFormComponent, title: 'WMS | Edit User' },
          { path: 'detail/:id', component: UserDetailComponent, title: 'WMS | User Detail' }
        ],
      },

      // Warehouses
      {
        path: 'warehouses',
        children: [
          { path: '', component: WarehouseListComponent, title: 'WMS | Warehouses' },
          { path: 'form', component: WarehouseFormComponent, title: 'WMS | Create Warehouse' },
          { path: 'form/:id', component: WarehouseFormComponent, title: 'WMS | Edit Warehouse' },
          { path: 'detail/:id', component: WarehouseDetailComponent, title: 'WMS | Warehouse Detail' },
          { path: 'zones', component: ZoneListComponent, title: 'WMS | Warehouse Zones' }
        ],
      },

      // Zones
      {
        path: 'zones',
        children: [
          { path: '', component: ZoneListComponent, title: 'WMS | Zones' },
          { path: 'form', component: ZoneFormComponent, title: 'WMS | Create Zone' },
          { path: 'form/:id', component: ZoneFormComponent, title: 'WMS | Edit Zone' },
          { path: 'detail/:id', component: ZoneDetailComponent, title: 'WMS | Zone Detail' }
        ],
      },

    ]
  },
  {
    path: 'signin',
    component: SigninFormComponent,
    title: 'WMS | Sign In'
  },
  {
    path: 'signout-callback-oidc',
    component: SignoutCallbackComponent
  }
];