import { Routes } from '@angular/router';

export const routes: Routes = [
	{ path: '', redirectTo: 'login', pathMatch: 'full' },
	{ path: 'login', loadComponent: () => import('./features/auth/login/login').then(m => m.LoginComponent) },
	{ path: 'register', loadComponent: () => import('./features/auth/register/register').then(m => m.RegisterComponent) },
	{ path: 'dashboard', loadComponent: () => import('./features/dashboard/dashboard').then(m => m.DashboardComponent) },
	{ path: 'history', loadComponent: () => import('./features/history/history.component').then(m => m.HistoryComponent) },
	{ path: 'profile', loadComponent: () => import('./features/profile/profile.component').then(m => m.ProfileComponent) },
	{ path: '**', redirectTo: 'login' }
];
