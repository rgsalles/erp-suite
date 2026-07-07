import { Routes } from '@angular/router';
import { LoginComponent } from './auth/login.component';
import { RegisterComponent } from './auth/register.component';
import { AuditLogsComponent } from './audit-logs/audit-logs.component';
import { CatalogComponent } from './catalog/catalog.component';
import { authGuard, guestGuard } from './core/auth.guard';
import { DashboardComponent } from './dashboard/dashboard.component';
import { FinanceComponent } from './finance/finance.component';
import { InventoryComponent } from './inventory/inventory.component';
import { ShellComponent } from './layout/shell.component';
import { MaterialsComponent } from './materials/materials.component';
import { OrganizationComponent } from './organization/organization.component';
import { OrdersComponent } from './orders/orders.component';
import { PartnersComponent } from './partners/partners.component';
import { UsersComponent } from './users/users.component';

export const routes: Routes = [
  { path: 'login', component: LoginComponent, canActivate: [guestGuard] },
  { path: 'register', component: RegisterComponent, canActivate: [guestGuard] },
  {
    path: '',
    component: ShellComponent,
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      { path: 'dashboard', component: DashboardComponent },
      { path: 'materials', component: MaterialsComponent },
      { path: 'organization', component: OrganizationComponent },
      { path: 'catalog', component: CatalogComponent },
      { path: 'inventory', component: InventoryComponent },
      { path: 'partners', component: PartnersComponent },
      { path: 'purchasing', component: OrdersComponent, data: { mode: 'purchasing' } },
      { path: 'sales', component: OrdersComponent, data: { mode: 'sales' } },
      { path: 'finance', component: FinanceComponent },
      { path: 'users', component: UsersComponent },
      { path: 'audit-logs', component: AuditLogsComponent }
    ]
  },
  { path: '**', redirectTo: 'dashboard' }
];
