import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { SearchMediaHistoryRecord } from '../../../services/search-media-history-service/search-media-history-record';
import { SearchMediaResponse } from '../../../services/search-media-service/search-media-response';
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

  getPostUrl(result: SearchMediaResponse): string {
    return `https://${result.hostName}/post/${result.postId}`;
  }

  getResultImageUrl(result: SearchMediaResponse): string {
    if (this.historyRecord.fileName) {
      const extension = this.historyRecord.fileName.split('.').pop()!;
      return `https://img10.${result.hostName}/pics/post/picture-${result.postAttributeId}.${extension}`;
    } else if (this.historyRecord.url) {
      const url = new URL(this.historyRecord.url);
      const extension = url.pathname.split('.').pop()!;
      return `https://img10.${result.hostName}/pics/post/static/picture-${result.postAttributeId}.${extension}`;
    }

    return '';
  }
}