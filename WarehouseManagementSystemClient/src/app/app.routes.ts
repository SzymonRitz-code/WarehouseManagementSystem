import { Routes } from '@angular/router';
import { AppLayoutComponent } from './layout/app-layout/app-layout.component';
import { SigninFormComponent } from './shared/components/auth/signin-form/signin-form.component';
import { authChildGuard, authGuard } from './core/guards/auth-guard';
import { SignoutCallbackComponent } from './shared/components/auth/signout-callback/signout-callback.component';


export const routes: Routes = [
  {
    path: '',
    component: AppLayoutComponent,
    canActivate: [authGuard],
    canActivateChild: [authChildGuard],
    children: [
      //Home
      { path: '', redirectTo: 'home', pathMatch: 'full' },
      {
        path: 'home', loadComponent: () => import('./features/home/home.component')
          .then(m => m.HomeComponent),
        title: 'WMS | Dashboard'
      },


      //Audit
      {
        path: 'audit',
        loadComponent: () => import('./features/audit/audit-log-list/audit-log-list.component')
          .then(m => m.AuditLogListComponent),
        title: 'WMS | Audit Logs'
      },
      // Documents
      {
        path: 'documents',
        children: [
          {
            path: '', loadComponent: () =>
              import('./features/documents/pages/document-list/document-list.component')
                .then(m => m.DocumentListComponent),
            title: 'WMS | Documents'
          },
          {
            path: 'pending', loadComponent: () =>
              import('./features/documents/pages/document-pending-list/document-pending-list.component')
                .then(m => m.DocumentPendingListComponent),
            title: 'WMS | Pending Documents'
          },
          {
            path: 'form', loadComponent: () =>
              import('./features/documents/pages/document-form/document-form.component')
                .then(m => m.DocumentFormComponent),
            title: 'WMS | Create Document'
          },
          {
            path: 'form/:id', loadComponent: () =>
              import('./features/documents/pages/document-form/document-form.component')
                .then(m => m.DocumentFormComponent),
            title: 'WMS | Edit Document'
          },
          {
            path: 'detail/:id', loadComponent: () =>
              import('./features/documents/pages/document-detail/document-detail.component')
                .then(m => m.DocumentDetailComponent),
            title: 'WMS | Document Detail'
          }
        ]
      },

      // Products
      {
        path: 'products',
        children: [
          {
            path: '', loadComponent: () =>
              import('./features/products/pages/product-list/product-list.component')
                .then(m => m.ProductListComponent),
            title: 'WMS | Products'
          },
          {
            path: 'detail/:id', loadComponent: () =>
              import('./features/products/pages/product-detail/product-detail.component')
                .then(m => m.ProductDetailComponent),
            title: 'WMS | Product Detail'
          },
          {
            path: 'form', loadComponent: () =>
              import('./features/products/pages/product-form/product-form.component')
                .then(m => m.ProductFormComponent),
            title: 'WMS | Create Product'
          },
          {
            path: 'form/:id', loadComponent: () =>
              import('./features/products/pages/product-form/product-form.component')
                .then(m => m.ProductFormComponent),
            title: 'WMS | Edit Product'
          },
          {
            path: ':id/batches', children: [
              {
                path: '', loadComponent: () =>
                  import('./features/products/pages/product-batch/product-batch-list/product-batch-list.component')
                    .then(m => m.ProductBatchListComponent),
                title: 'WMS | Product Batches'
              },
              {
                path: 'form', loadComponent: () =>
                  import('./features/products/pages/product-batch/product-batch-form/product-batch-form.component')
                    .then(m => m.ProductBatchFormComponent),
                title: 'WMS | Create Product Batch'
              },
              {
                path: 'form/:batchId', loadComponent: () =>
                  import('./features/products/pages/product-batch/product-batch-form/product-batch-form.component')
                    .then(m => m.ProductBatchFormComponent),
                title: 'WMS | Edit Product Batch'
              },
              {
                path: 'detail/:batchId', loadComponent: () =>
                  import('./features/products/pages/product-batch/product-batch-detail/product-batch-detail.component')
                    .then(m => m.ProductBatchDetailComponent),
                title: 'WMS | Product Batch Detail'
              }
            ]
          }
        ],
      },

      // Stocks
      {
        path: 'stocks',
        children: [
          {
            path: '', loadComponent: () =>
              import('./features/stocks/pages/stock-list/stock-list.component')
                .then(m => m.StockListComponent),
            title: 'WMS | Stocks'
          },
          {
            path: 'availability', loadComponent: () =>
              import('./features/stocks/pages/stock-availability/stock-availability.component')
                .then(m => m.StockAvailabilityComponent),
            title: 'WMS | Stock Availability'
          }
        ],
      },

      // Users
      {
        path: 'users',
        children: [
          {
            path: '', loadComponent: () =>
              import('./features/users/pages/user-list/user-list.component')
                .then(m => m.UserListComponent),
            title: 'WMS | Users'
          },
          {
            path: 'form', loadComponent: () =>
              import('./features/users/pages/user-form/user-form.component')
                .then(m => m.UserFormComponent),
            title: 'WMS | Create User'
          },
          {
            path: 'form/:id', loadComponent: () =>
              import('./features/users/pages/user-form/user-form.component')
                .then(m => m.UserFormComponent),
            title: 'WMS | Edit User'
          },
          {
            path: 'detail/:id', loadComponent: () =>
              import('./features/users/pages/user-detail/user-detail.component')
                .then(m => m.UserDetailComponent),
            title: 'WMS | User Detail'
          }
        ],
      },

      // Warehouses
      {
        path: 'warehouses',
        children: [
          {
            path: '', loadComponent: () =>
              import('./features/warehouses/pages/warehouse-list/warehouse-list.component')
                .then(m => m.WarehouseListComponent),
            title: 'WMS | Warehouses'
          },
          {
            path: 'form', loadComponent: () =>
              import('./features/warehouses/pages/warehouse-form/warehouse-form.component')
                .then(m => m.WarehouseFormComponent),
            title: 'WMS | Create Warehouse'
          },
          {
            path: 'form/:id', loadComponent: () =>
              import('./features/warehouses/pages/warehouse-form/warehouse-form.component')
                .then(m => m.WarehouseFormComponent),
            title: 'WMS | Edit Warehouse'
          },
          {
            path: 'detail/:id', loadComponent: () =>
              import('./features/warehouses/pages/warehouse-detail/warehouse-detail.component')
                .then(m => m.WarehouseDetailComponent),
            title: 'WMS | Warehouse Detail'
          },
          {
            path: 'zones', loadComponent: () =>
              import('./features/zones/pages/zone-list/zone-list.component')
                .then(m => m.ZoneListComponent),
            title: 'WMS | Warehouse Zones'
          }
        ],
      },

      // Zones
      {
        path: 'zones',
        children: [
          {
            path: '', loadComponent: () =>
              import('./features/zones/pages/zone-list/zone-list.component')
                .then(m => m.ZoneListComponent),
            title: 'WMS | Zones'
          },
          {
            path: 'form', loadComponent: () =>
              import('./features/zones/pages/zone-form/zone-form.component')
                .then(m => m.ZoneFormComponent),
            title: 'WMS | Create Zone'
          },
          {
            path: 'form/:id', loadComponent: () =>
              import('./features/zones/pages/zone-form/zone-form.component')
                .then(m => m.ZoneFormComponent),
            title: 'WMS | Edit Zone'
          },
          {
            path: 'detail/:id', loadComponent: () =>
              import('./features/zones/pages/zone-detail/zone-detail.component')
                .then(m => m.ZoneDetailComponent),
            title: 'WMS | Zone Detail'
          }
        ],
      },

    ]
  },
  {
    path: 'signin',
    loadComponent: () => SigninFormComponent,
    title: 'WMS | Sign In'
  },
  {
    path: 'signin-oidc',
    loadComponent: () => import('./shared/components/auth/signin-callback/signin-callback.component')
      .then(m => m.SigninCallbackComponent)
  },
  {
    path: 'signout-callback-oidc',
    loadComponent: () => SignoutCallbackComponent
  }
];
