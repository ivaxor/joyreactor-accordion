import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { SearchEmbeddedHistoryInfo } from '../search-embedded-history-info/search-embedded-history-info';
import { SearchEmbeddedHistoryService } from '../../../services/search-embedded-history-service/search-embedded-history-service';
import { SearchEmbeddedHistoryRecord } from '../../../services/search-embedded-history-service/search-embedded-history-record';

@Component({
  selector: 'app-search-embedded-history',
  imports: [SearchEmbeddedHistoryInfo],
  templateUrl: './search-embedded-history.html',
  styleUrl: './search-embedded-history.scss',
})
export class SearchEmbeddedHistory implements OnInit {
  private changeDetector = inject(ChangeDetectorRef);
  private searchEmbeddedHistoryService = inject(SearchEmbeddedHistoryService);
  historyRecords: SearchEmbeddedHistoryRecord[] = [];
  offset: number = 0;
  limit: number = 10;

  ngOnInit(): void {
    this.searchEmbeddedHistoryService.records$.subscribe(historyRecords => {
      this.historyRecords = historyRecords;
      this.changeDetector.markForCheck();
    })
  }

  async onTrashClick(): Promise<void> {
    await this.searchEmbeddedHistoryService.clear();
  }
}