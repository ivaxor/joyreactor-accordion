import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, ElementRef, EventEmitter, HostListener, inject, Output, signal, ViewChild } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { BytesPipe } from '../../../pipes/bytes-pipe';
import { SearchMediaService } from '../../../services/search-media-service/search-media-service';
import { isIP } from 'is-ip';
import { catchError, concatMap, EMPTY, finalize, from, map, of, switchMap, tap, throwError, timer } from 'rxjs';

@Component({
  selector: 'app-search-media',
  imports: [CommonModule, ReactiveFormsModule, BytesPipe],
  templateUrl: './search-media.html',
  styleUrl: './search-media.scss',
})
export class SearchMedia {
  private formBuilder = inject(FormBuilder);
  private changeDetector = inject(ChangeDetectorRef);
  private searchMediaService = inject(SearchMediaService);

  @Output() onFileSelected = new EventEmitter<File>();
  @ViewChild("fileInput", { read: ElementRef }) fileInput!: ElementRef<HTMLInputElement>;

  allowedTypes: string[] = ['image/png', 'image/jpeg', 'image/gif', 'image/bmp', 'image/tiff', 'video/mp4', 'video/webm', 'image/webp'];
  isDragging: boolean = false;
  searching: boolean = false;
  isDuplicates = signal<number[]>([]);
  errorMessage = signal<string>('');

  searchForm = this.formBuilder.group({
    files: [[] as File[]],
    url: [''],
  });
  get files() { return this.searchForm.get('files')?.value || []; }
  get url() { return this.searchForm.get('url')?.value; }

  onFileChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.searchForm.patchValue({ files: Array.from(input.files), url: '' });
      this.fileInput.nativeElement.value = '';
    }
  }

  onFileDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = false;

    if (event.dataTransfer?.files && event.dataTransfer.files.length > 0) {
      const files = Array.from(event.dataTransfer.files).filter(file => this.allowedTypes.includes(file.type));
      if (files.length > 0) {
        this.searchForm.patchValue({ files, url: '' });
        this.fileInput.nativeElement.value = '';
      }
    }
  }

  @HostListener('window:paste', ['$event'])
  onPaste(event: ClipboardEvent): void {
    if (this.searching)
      return;

    if (!event.clipboardData?.items)
      return;

    const files = Array.from(event.clipboardData.items).filter(item => this.allowedTypes.includes(item.type)).map(item => item.getAsFile()!);
    if (files.length > 0) {
      this.searchForm.patchValue({ files, url: '' });
      this.fileInput.nativeElement.value = '';
    }
  }

  onUrlChange(): void {
    const currentUrl = this.searchForm.get('url')?.value || '';
    this.searchForm.patchValue(
      { files: [], url: decodeURIComponent(currentUrl) },
      { emitEvent: false });
    this.fileInput.nativeElement.value = '';
  }

  isSearchDisabled(): boolean {
    if (this.searching)
      return true;

    if (this.files.length > 0)
      return false;

    try {
      const url = new URL(this.url!);

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

  search(threshold: number): void {
    if (this.files.length > 0) {
      this.searchUpload(threshold);
    } else if (this.url) {
      this.searchDownload(threshold);
    }
  }

  private resetForm() {
    this.searchForm.reset({ files: [], url: '' });
    this.fileInput.nativeElement.value = '';
    this.searching = false;
  }

  searchUpload(threshold: number): void {
    this.errorMessage.set('');
    this.searching = true;

    from(this.files).pipe(
      concatMap((file) => {
        this.isDuplicates.set([]);
        return this.searchMediaService.searchUpload(file, threshold).pipe(
          switchMap(response => {
            gtag('event', 'search_media_upload', { threshold, results: response.length });

            if (response.length === 0)
              return of(response);

            this.isDuplicates.set([...response.map((_, i) => i)]);
            return timer(2500 * response.length).pipe(map(() => response));
          }),
          catchError(error => {
            this.errorMessage.set(Object.keys(error.error).flatMap(e => error.error[e] as string[]).join('\n'));
            return throwError(() => error);
          }))
      }),
      finalize(() => {
        this.resetForm();
        this.searching = false;
        this.isDuplicates.set([]);
        this.changeDetector.markForCheck();
      }))
      .subscribe();
  }

  searchDownload(threshold: number): void {
    this.errorMessage.set('');
    this.searching = true;
    this.searchMediaService.searchDownload(this.url!, threshold)
      .pipe(
        catchError(error => {
          this.errorMessage.set(Object.keys(error.error).flatMap(e => error.error[e] as string[]).join('\n'));
          this.searching = false;
          this.changeDetector.markForCheck();
          return EMPTY;
        }),
        tap(response => {
          gtag('event', 'search_media_download', { threshold, results: response.length });

          this.resetForm();
          if (response.length > 0) {
            this.isDuplicates.set(response.map((_, i) => i));
            setTimeout(() => this.isDuplicates.set([]), 2500 * response.length);
          }
        }))
      .subscribe();
  }
}