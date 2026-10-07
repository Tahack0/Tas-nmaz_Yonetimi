import { Routes } from '@angular/router';
import { authGuard } from './core/auth-guard';
import { adminGuard } from './core/admin-guard';

export const routes: Routes = [
  {
     path: 'register',
    loadComponent: () =>
      import('./pages/register/register')
        .then(component => component.Register),
  },
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
  path: 'property-map',
    canActivate: [authGuard],
    loadComponent: () =>
      import(
        './pages/property-map-overview/property-map-overview'
      ).then(
        (component) => component.PropertyMapOverview,
      ),
  },
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'dashboard',
  },
  {
    path: 'spatial-analysis',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./pages/spatial-analysis/spatial-analysis')
        .then(component => component.SpatialAnalysis),
  },
  {
    path: '**',
    redirectTo: 'dashboard',
  },
];