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
  historyRecordExtended!: SearchMediaHistoryRecordExtended;
  isVideo!: boolean;

  ngOnChanges(changes: SimpleChanges): void {
    if (!this.historyRecord)
      return;

    if (this.historyRecord.url) {
      const url = new URL(this.historyRecord.url);
      const isVideo = url.pathname.endsWith('webm') || url.pathname.endsWith('mp4');
      const isJoyReactor = url.hostname.endsWith('joyreactor.cc') || url.hostname.endsWith('joyreactor.com');

      if (isVideo && isJoyReactor) {
        this.historyRecordExtended = {
          ...this.historyRecord,
          url: this.historyRecord.url.replace('/picture-', '/static/picture-'),
          isVideo: false,
        };
      } else {
        this.historyRecordExtended = {
          ...this.historyRecord,
          url: this.historyRecord.url.replace('/picture-', '/static/picture-'),
          isVideo: true,
        };
      }
    } else if (this.historyRecord.fileName) {
      const isVideo = this.historyRecord.fileName.endsWith('webm') || this.historyRecord.fileName.endsWith('mp4');

      this.historyRecordExtended = {
        ...this.historyRecord,
        isVideo: isVideo,
      };
    }

    this.historyRecordExtended.results = this.historyRecordExtended.results.sort((a, b) => a.postId! - b.postId!);
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

export interface SearchMediaHistoryRecordExtended extends SearchMediaHistoryRecord {
  isVideo: boolean,
}