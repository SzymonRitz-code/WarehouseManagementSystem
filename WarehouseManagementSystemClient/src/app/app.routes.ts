import { Routes } from '@angular/router';
import { AppLayoutComponent } from './layout/app-layout/app-layout.component';
import { DashboardComponent } from './pages/dashboard/dashboard.component';

export const routes: Routes = [
    {
        path: '',
        component: AppLayoutComponent,
        children: [
            {
                path: '',
                component: DashboardComponent,
                pathMatch: 'full',
                title:
                    'WMS | Admin Dashboard',
            }
        ]
    }
];
