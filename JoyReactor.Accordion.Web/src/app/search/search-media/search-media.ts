import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, ElementRef, EventEmitter, HostListener, inject, Output, signal, ViewChild } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { BytesPipe } from '../../../pipes/bytes-pipe';
import { SearchMediaService } from '../../../services/search-media-service/search-media-service';
import { isIP } from 'is-ip';
import { catchError, EMPTY, tap } from 'rxjs';

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
    file: [null as File | null],
    url: [''],
  });
  get file() { return this.searchForm.get('file')?.value; }
  get url() { return this.searchForm.get('url')?.value; }

  onFileChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.searchForm.patchValue({ file: input.files[0], url: '' });
      this.fileInput.nativeElement.value = '';
    }
  }

  onFileDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = false;

    if (event.dataTransfer?.files && event.dataTransfer.files.length > 0) {
      const file = event.dataTransfer.files[0];
      if (this.allowedTypes.some(allowedType => allowedType === file.type)) {
        this.searchForm.patchValue({ file, url: '' });
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

    for (let i = 0; i < event.clipboardData.items.length; i++) {
      if (!this.allowedTypes.includes(event.clipboardData.items[i].type))
        continue;

      this.searchForm.patchValue({ file: event.clipboardData.items[i].getAsFile(), url: '' });
      this.fileInput.nativeElement.value = '';
      break;
    }
  }

  onUrlChange(): void {
    const currentUrl = this.searchForm.get('url')?.value || '';
    this.searchForm.patchValue(
      { file: null, url: decodeURIComponent(currentUrl) },
      { emitEvent: false });
    this.fileInput.nativeElement.value = '';
  }

  isSearchDisabled(): boolean {
    if (this.searching)
      return true;

    if (this.file)
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
    if (this.file) {
      this.searchUpload(threshold);
    } else if (this.url) {
      this.searchDownload(threshold);
    }
  }

  private resetForm() {
    this.searchForm.reset({ file: null, url: '' });
    this.fileInput.nativeElement.value = '';
    this.searching = false;
  }

  searchUpload(threshold: number): void {
    this.errorMessage.set('');
    this.searching = true;
    const file = this.file!;
    this.searchMediaService.searchUpload(file, threshold)
      .pipe(
        catchError(error => {
          this.errorMessage.set(Object.keys(error.error).flatMap(e => error.error[e] as string[]).join('\n'));
          this.searching = false;
          this.changeDetector.markForCheck();
          return EMPTY;
        }),
        tap(response => {
          this.resetForm();
          if (response.length > 0) {
            this.isDuplicates.set(response.map((_, i) => i));
            setTimeout(() => this.isDuplicates.set([]), 2500 * response.length);
          }
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
          this.resetForm();
          if (response.length > 0) {
            this.isDuplicates.set(response.map((_, i) => i));
            setTimeout(() => this.isDuplicates.set([]), 2500 * response.length);
          }
        }))
      .subscribe();
  }
}