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

  ngOnChanges(changes: SimpleChanges): void {
    if (!this.historyRecord)
      return;

    if (this.historyRecord.fileName) {
      const isVideo = this.historyRecord.fileName.endsWith('webm') || this.historyRecord.fileName.endsWith('mp4');

      this.historyRecordExtended = {
        ...this.historyRecord,
        isVideo: isVideo,
      };
    } else if (this.historyRecord.url) {
      const url = new URL(this.historyRecord.url);
      const isVideo = url.pathname.endsWith('webm') || url.pathname.endsWith('mp4');
      const isJoyReactor = url.hostname.endsWith('joyreactor.cc') || url.hostname.endsWith('joyreactor.com');

      if (isVideo && isJoyReactor) {
        let newUrl = this.historyRecord.url;
        if (url.pathname.includes('/picture-'))
          newUrl = newUrl.replace('/picture-', '/static/picture-');
        else if (url.pathname.includes('/webm/') && url.pathname.endsWith('.webm'))
          newUrl = newUrl.replace('/webm/', '/static/').replace('.webm', '.jpeg');
        else if (url.pathname.includes('/mp4/') && url.pathname.endsWith('.mp4'))
          newUrl = newUrl.replace('/mp4/', '/static/').replace('.mp4', '.jpeg');

        this.historyRecordExtended = {
          ...this.historyRecord,
          url: newUrl,
          isVideo: false,
        };
      } else {
        this.historyRecordExtended = {
          ...this.historyRecord,
          isVideo: isVideo,
        };
      }
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