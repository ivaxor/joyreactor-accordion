import { CommonModule } from '@angular/common';
import { Component, EventEmitter, inject, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { BytesPipe } from '../../../pipes/bytes-pipe';
import { SearchService } from '../../../services/search-service/search-service';
import { isIP } from 'is-ip';
import { SearchMediaHistoryService } from '../../../services/search-media-history-service/search-media-history-service';
import { tap } from 'rxjs';

@Component({
  selector: 'app-search-media',
  imports: [CommonModule, FormsModule, BytesPipe],
  templateUrl: './search-media.html',
  styleUrl: './search-media.scss',
})
export class SearchMedia {
  private searchService = inject(SearchService);
  private searchMediaHistoryService = inject(SearchMediaHistoryService);
  @Output() onFileSelected = new EventEmitter<File>();

  allowedTypes: string[] = ['image/png', 'image/jpeg', 'image/gif', 'image/bmp', 'image/tiff', 'video/mp4', 'video/webm'];
  isDragging: boolean = false;
  file: File | null = null;
  url: string = '';
  searching: boolean = false;

  onFileChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.file = input.files[0];
      this.url = '';
    }
  }

  onFileDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = false;

    if (event.dataTransfer?.files && event.dataTransfer.files.length > 0) {
      const file = event.dataTransfer.files[0];
      if (this.allowedTypes.some(allowedType => allowedType === file.type)) {
        this.file = file;
        this.url = '';
      }
    }
  }

  onUrlChange(event: Event): void {
    this.file = null;
    this.url = decodeURIComponent(this.url);
  }

  isSearchDisabled(): boolean {
    if (this.searching)
      return true;

    if (this.file)
      return false;

    try {
      const url = new URL(this.url);

      if (url.protocol !== 'https:')
        return true;

      if (!url.host.includes('.'))
        return true;

      if (isIP(url.host))
        return true;

      return false;
    } catch {
      return true;
    }
  }

  search(): void {
    if (this.file) {
      this.searching = true;
      const file = this.file;
      this.searchService.searchUpload(file)
        .pipe(
          tap(results => this.searchMediaHistoryService.addUpload(file, results)),
          tap(() => this.file = null),
          tap(() => this.searching = false))
        .subscribe();
    } else if (this.url) {
      this.searching = true;
      const url = this.url;
      this.searchService.searchDownload(this.url)
        .pipe(
          tap(results => this.searchMediaHistoryService.addDownload(url, results)),
          tap(() => this.url = ''),
          tap(() => this.searching = false))
        .subscribe();
    }
  }
}