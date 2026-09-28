import { Routes } from '@angular/router';
import { authGuard } from './core/auth-guard';
import { adminGuard } from './core/admin-guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./pages/login/login')
        .then((component) => component.Login),
  },
  {
    path: 'dashboard',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./pages/dashboard/dashboard')
        .then((component) => component.Dashboard),
  },
  {
    path: 'properties',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./pages/properties/properties')
        .then((component) => component.Properties),
  },
  {
  path: 'users',
  canActivate: [authGuard, adminGuard],
  loadComponent: () =>
    import('./pages/users/users')
      .then((component) => component.Users),
  },
  {
    path: 'audit-logs',
    canActivate: [authGuard, adminGuard],
    loadComponent: () =>
      import('./pages/audit-logs/audit-logs')
        .then(
          (component) => component.AuditLogs,
        ),
  },
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'dashboard',
  },
  {
    path: '**',
    redirectTo: 'dashboard',
  },
];