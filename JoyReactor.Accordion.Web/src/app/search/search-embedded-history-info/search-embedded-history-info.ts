import { Component, inject, Input, OnChanges, SimpleChanges } from '@angular/core';
import { SearchEmbeddedHistoryRecord } from '../../../services/search-embedded-history-service/search-embedded-history-record';
import { DatePipe } from '@angular/common';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { JoyReactorMediaMetadataService } from '../../../services/joyreactor-media-metadata-service/joyreactor-media-metadata-service';

@Component({
  selector: 'app-search-embedded-history-info',
  imports: [DatePipe],
  templateUrl: './search-embedded-history-info.html',
  styleUrl: './search-embedded-history-info.scss',
})
export class SearchEmbeddedHistoryInfo implements OnChanges {
  private domSanitizer = inject(DomSanitizer);
  joyReactorMediaMetadataService = inject(JoyReactorMediaMetadataService);

  @Input({ required: true }) historyRecord!: SearchEmbeddedHistoryRecord;

  iframeUrl: SafeResourceUrl | null = null;

  ngOnChanges(changes: SimpleChanges): void {
    if (!this.historyRecord)
      return;

    const iframeUrl = this.joyReactorMediaMetadataService.getIframeUrl(this.historyRecord);
    this.iframeUrl = this.domSanitizer.bypassSecurityTrustResourceUrl(iframeUrl);
    this.historyRecord.postIds = this.historyRecord.postIds.sort((a, b) => a - b);
  }
}