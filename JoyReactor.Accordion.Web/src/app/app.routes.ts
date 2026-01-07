import { Routes } from '@angular/router';
import { Search } from './search/search';

export const routes: Routes = [
  {
    path: '',
    component: Search,
    title: 'JoyReactor Accordion',
  },
  {
    path: '**',
    redirectTo: '',
  }
];