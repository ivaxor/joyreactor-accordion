import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-search-embedded-history-info',
  imports: [],
  templateUrl: './search-embedded-history-info.html',
  styleUrl: './search-embedded-history-info.scss',
})
export class SearchEmbeddedHistoryInfo {
  @Input({ required: true }) historyRecord!: any;
}