import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { SearchEmbeddedHistoryRecord } from '../../../services/search-embedded-history-service/search-embedded-history-record';
import { SearchEmbeddedType } from '../../../services/search-embedded-service/search-embedded-request';

@Component({
  selector: 'app-search-embedded-history-info',
  imports: [],
  templateUrl: './search-embedded-history-info.html',
  styleUrl: './search-embedded-history-info.scss',
})
export class SearchEmbeddedHistoryInfo implements OnChanges {
  @Input({ required: true }) historyRecord!: SearchEmbeddedHistoryRecord;

  ngOnChanges(changes: SimpleChanges): void {
    if (!this.historyRecord)
      return;

    this.historyRecord.postIds = this.historyRecord.postIds.sort((a, b) => a - b);
  }

  getIFrameUrl(): string {
    switch (this.historyRecord.type) {
      case SearchEmbeddedType.BandCamp:
        return ``;

      case SearchEmbeddedType.Coub:
        // <iframe src="//coub.com/embed/***?muted=false&autostart=false&originalSize=false&startWithHD=false" allowfullscreen frameborder="0" width="600" height="480" allow="autoplay"></iframe>
        return `https://coub.com/embed/${this.historyRecord.text}`;

      case SearchEmbeddedType.SoundCloud:
        return ``;

      case SearchEmbeddedType.Vimeo:
        return ``;

      case SearchEmbeddedType.YouTube:
        // <iframe width="560" height="315" src="https://www.youtube.com/embed/***" title="YouTube video player" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share" referrerpolicy="strict-origin-when-cross-origin" allowfullscreen></iframe>
        return `https://www.youtube.com/embed/${this.historyRecord.text}`;

      default:
        return '';
    }
  }
}