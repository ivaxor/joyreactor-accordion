import { Routes } from '@angular/router';
import { SearchRoot } from './search/search-root/search-root';
import { StatisticsRoot } from './statistics/statistics-root/statistics-root';
import { VoteRoot } from './vote/vote-root/vote-root';

export const routes: Routes = [
  {
    path: 'search',
    component: SearchRoot,
    title: 'Поиск - JR Accordion',
  },
  {
    path: 'statistics',
    component: StatisticsRoot,
    title: 'Статистика - JR Accordion',
  },
  {
    path: 'vote',
    component: VoteRoot,
    title: 'Найди баян - JR Accordion',
  },
  {
    path: '**',
    redirectTo: 'search',
  },
];