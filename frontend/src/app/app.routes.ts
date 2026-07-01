import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./features/home/home').then(m => m.Home),
    title: 'LernFuchs – Startseite'
  },
  {
    path: 'wortschatz',
    loadComponent: () => import('./features/wortschatz/wortschatz').then(m => m.Wortschatz),
    title: 'LernFuchs – Wortschatz'
  },
  {
    path: 'lesen',
    loadComponent: () => import('./features/leseverstaendnis/leseverstaendnis').then(m => m.Leseverstaendnis),
    title: 'LernFuchs – Leseverständnis'
  },
  { path: '**', redirectTo: '' }
];
