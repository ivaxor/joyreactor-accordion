import { Component } from '@angular/core';
import { SearchEmbeddedHistoryInfo } from "../search-embedded-history-info/search-embedded-history-info";

@Component({
  selector: 'app-search-embedded-history',
  imports: [SearchEmbeddedHistoryInfo],
  templateUrl: './search-embedded-history.html',
  styleUrl: './search-embedded-history.scss',
})
export class SearchEmbeddedHistory {
  historyRecords: any[] = [];

  onTrashClick(): void {

  }
}