import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, computed, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { SearchEmbeddedRequest, SearchEmbeddedType } from '../../../services/search-embedded-service/search-embedded-request';
import { SearchEmbeddedService } from '../../../services/search-embedded-service/search-embedded-service';
import { catchError, EMPTY, tap } from 'rxjs';

@Component({
  selector: 'app-search-embedded',
  imports: [CommonModule, FormsModule],
  templateUrl: './search-embedded.html',
  styleUrl: './search-embedded.scss',
})
export class SearchEmbedded implements OnInit, OnDestroy {
  private placeholders: string[] = [
    'Вставьте ссылку на внешний ресурс',
    'https://bandcamp.com/***',
    'https://coub.com/view/*****',
    'https://soundcloud.com/***',
    'https://vimeo.com/*********',
    'https://youtu.be/***********',
    'https://www.youtube.com/watch?v=***********'];
  private changeDetector = inject(ChangeDetectorRef);
  private searchEmbeddedService = inject(SearchEmbeddedService);

  url: string = '';

  placeholderIndex = signal(0);
  placeholderTimerId: number | null = null;
  placeholder = computed(() => this.placeholders[this.placeholderIndex()]);

  searching: boolean = false;
  isDuplicates = signal<number[]>([]);

  ngOnInit(): void {
    this.placeholderTimerId = setInterval(() => this.placeholderIndex.update(index => (index + 1) % this.placeholders.length), 3000);
  }

  ngOnDestroy(): void {
    if (this.placeholderTimerId)
      clearInterval(this.placeholderTimerId);
  }

  isSearchDisabled(): boolean {
    if (this.searching)
      return true;

    return !this.tryToParseUrl();
  }

  search(): void {
    const request = this.tryToParseUrl();
    if (!request)
      return;

    this.searching = true;

    this.searchEmbeddedService.search(request)
      .pipe(
        catchError(() => {
          this.searching = false;
          this.changeDetector.markForCheck();
          return EMPTY;
        }),
        tap(response => {
          this.url = '';
          this.searching = false;
          if (response.postIds.length > 0) {
            this.isDuplicates.set(response.postIds.map((_, i) => i));
            setTimeout(() => this.isDuplicates.set([]), 2500 * response.postIds.length);
          }
        }))
      .subscribe();
  }

  tryToParseUrl(): SearchEmbeddedRequest | null {
    try {
      const url = new URL(this.url);

      if (url.host.endsWith('bandcamp.com')) {
        if (url.pathname.startsWith('/album/') || url.pathname.startsWith('/track/') || url.pathname.startsWith('/users/') || url.pathname.startsWith('/tracks/') || url.pathname.startsWith('/playlists/')) {
          const text = url.pathname.replace('/', '');
          return ({ type: SearchEmbeddedType.BandCamp, text });
        }
      } else if (url.host === 'coub.com') {
        if (url.pathname.startsWith('/embed/')) {
          const text = url.pathname.replace('/embed/', '');
          return ({ type: SearchEmbeddedType.Coub, text });
        } else if (url.pathname.startsWith('/view/')) {
          const text = url.pathname.replace('/view/', '');
          return ({ type: SearchEmbeddedType.Coub, text });
        }
      } else if (url.host === 'soundcloud.com') {
        const text = url.pathname.replace('/', '');
        return ({ type: SearchEmbeddedType.Coub, text });
      } else if (url.host.endsWith('vimeo.com')) {
        if (url.host === 'vimeo.com') {
          const text = url.pathname.replace('/', '');
          return ({ type: SearchEmbeddedType.Vimeo, text });
        } else if (url.host === 'player.vimeo.com' && url.pathname.startsWith('/video/')) {
          const text = url.pathname.replace('/video/', '');
          return ({ type: SearchEmbeddedType.Vimeo, text });
        }
      } else if (url.host === 'youtu.be') {
        const text = url.pathname.replace('/', '');
        return ({ type: SearchEmbeddedType.YouTube, text })
      } else if (url.host === 'youtube.com' || url.host === 'www.youtube.com') {
        const v = url.searchParams.get('v');
        if (v) {
          return ({ type: SearchEmbeddedType.YouTube, text: v });
        }
      }

      return null;
    } catch {
      return null;
    }
  }
}