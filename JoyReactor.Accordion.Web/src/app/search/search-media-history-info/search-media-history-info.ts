import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { SearchMediaHistoryRecord } from '../../../services/search-media-history-service/search-media-history-record';
import { SearchResponse } from '../../../services/search-service/search-response';
import { DatePipe } from '@angular/common';

@Component({
  selector: 'app-search-media-history-info',
  imports: [DatePipe],
  templateUrl: './search-media-history-info.html',
  styleUrl: './search-media-history-info.scss',
})
export class SearchMediaHistoryInfo implements OnChanges {
  @Input({ required: true }) historyRecord!: SearchMediaHistoryRecord;

  ngOnChanges(changes: SimpleChanges): void {
    if (!this.historyRecord)
      return;

    this.historyRecord.results = this.historyRecord.results.sort((a, b) => a.postId! - b.postId!);
  }

  getPostUrl(result: SearchResponse): string {
    return `https://joyreactor.cc/post/${result.postId}`;
  }

  getImageUrl(result: SearchResponse): string {
    return `https://img10.joyreactor.cc/pics/post/picture-${result.postAttributeId}.jpeg`;
  }
}