import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, EventEmitter, HostListener, inject, Output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { BytesPipe } from '../../../pipes/bytes-pipe';
import { SearchMediaService } from '../../../services/search-media-service/search-media-service';
import { isIP } from 'is-ip';
import { SearchMediaHistoryService } from '../../../services/search-media-history-service/search-media-history-service';
import { catchError, EMPTY, tap } from 'rxjs';

@Component({
  selector: 'app-search-media',
  imports: [CommonModule, FormsModule, BytesPipe],
  templateUrl: './search-media.html',
  styleUrl: './search-media.scss',
})
export class SearchMedia {
  private changeDetector = inject(ChangeDetectorRef);
  private searchMediaService = inject(SearchMediaService);
  private searchMediaHistoryService = inject(SearchMediaHistoryService);
  @Output() onFileSelected = new EventEmitter<File>();

  allowedTypes: string[] = ['image/png', 'image/jpeg', 'image/gif', 'image/bmp', 'image/tiff', 'video/mp4', 'video/webm'];
  isDragging: boolean = false;
  file: File | null = null;
  url: string = '';
  searching: boolean = false;
  isDuplicates = signal<number[]>([]);

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

  @HostListener('window:paste', ['$event'])
  onPaste(event: ClipboardEvent): void {
    if (this.searching)
      return;

    if (!event.clipboardData?.items)
      return;

    for (let i = 0; i < event.clipboardData.items.length; i++) {
      if (!this.allowedTypes.includes(event.clipboardData.items[i].type))
        continue;

      this.file = event.clipboardData.items[i].getAsFile();
      this.url = '';
      break;
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
      this.searchMediaService.searchUpload(file)
        .pipe(
          catchError(() => {
            this.searching = false;
            this.changeDetector.markForCheck();
            return EMPTY;
          }),
          tap(results => {
            this.searchMediaHistoryService.addUpload(file, results);
            this.file = null;
            this.searching = false;
            if (results.length > 0) {
              this.isDuplicates.set(results.map((_, i) => i));
              setTimeout(() => this.isDuplicates.set([]), 3000);
            }
          }))
        .subscribe();
    } else if (this.url) {
      this.searching = true;
      const url = this.url;
      this.searchMediaService.searchDownload(this.url)
        .pipe(
          catchError(() => {
            this.searching = false;
            this.changeDetector.markForCheck();
            return EMPTY;
          }),
          tap(results => {
            this.searchMediaHistoryService.addDownload(url, results);
            this.url = '';
            this.searching = false;
            if (results.length > 0) {
              this.isDuplicates.set(results.map((_, i) => i));
              setTimeout(() => this.isDuplicates.set([]), 3000);
            }
          }))
        .subscribe();
    }
  }
}