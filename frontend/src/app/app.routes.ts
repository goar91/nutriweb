import { Routes } from '@angular/router';
import { authGuard } from './guards/auth.guard';

export const routes: Routes = [
  {
    path: '',
    redirectTo: '/login',
    pathMatch: 'full'
  },
  {
    path: 'login',
    loadComponent: () => import('./components/login/login.component').then(m => m.LoginComponent)
  },
  {
    path: 'dashboard',
    loadComponent: () => import('./components/dashboard/dashboard.component').then(m => m.DashboardComponent),
    canActivate: [authGuard]
  },
  {
    path: 'history',
    loadComponent: () => import('./app').then(m => m.App),
    canActivate: [authGuard]
  },
  {
    path: 'pacientes',
    loadComponent: () => import('./components/pacientes/pacientes.component').then(m => m.PacientesComponent),
    canActivate: [authGuard]
  },
  {
    path: 'pacientes/:id',
    loadComponent: () => import('./components/paciente-detalle/paciente-detalle.component').then(m => m.PacienteDetalleComponent),
    canActivate: [authGuard]
  },
  {
    path: 'reportes',
    loadComponent: () => import('./components/reportes/reportes.component').then(m => m.ReportesComponent),
    canActivate: [authGuard]
  },
  {
    path: '**',
    redirectTo: '/login'
  }
];
