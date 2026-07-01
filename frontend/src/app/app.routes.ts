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
  {
    path: 'spiele',
    loadComponent: () => import('./features/games/games-hub').then(m => m.GamesHub),
    title: 'LernFuchs – Spiele'
  },
  {
    path: 'spiele/quiz',
    loadComponent: () => import('./features/games/quiz').then(m => m.QuizGame),
    title: 'LernFuchs – Quiz'
  },
  {
    path: 'spiele/artikel',
    loadComponent: () => import('./features/games/artikel').then(m => m.ArtikelGame),
    title: 'LernFuchs – der/die/das'
  },
  {
    path: 'spiele/diktat',
    loadComponent: () => import('./features/games/diktat').then(m => m.DiktatGame),
    title: 'LernFuchs – Diktat'
  },
  {
    path: 'spiele/memory',
    loadComponent: () => import('./features/games/memory').then(m => m.MemoryGame),
    title: 'LernFuchs – Memory'
  },
  { path: '**', redirectTo: '' }
];
