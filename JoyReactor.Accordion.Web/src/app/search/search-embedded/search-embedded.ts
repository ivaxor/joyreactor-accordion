import { CommonModule } from '@angular/common';
import { Component, computed, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { SearchEmbeddedRequest, SearchEmbeddedType } from '../../../services/search-embedded-service/search-embedded-request';
import { SearchEmbeddedService } from '../../../services/search-embedded-service/search-embedded-service';

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
    return !this.tryToParseUrl();
  }

  search(): void {
    const request = this.tryToParseUrl();
    if (!request)
      return;

    this.searchEmbeddedService.search(request)
      .subscribe(response => console.log(response));
  }

  tryToParseUrl(): SearchEmbeddedRequest | null {
    if (this.url.startsWith('https://bandcamp.com/')) {
      // TODO: Implement
    } else if (this.url.startsWith('https://coub.com/view/')) {
      const text = this.url.replace('https://coub.com/view/', '');
      return ({ type: SearchEmbeddedType.Coub, text });
    } else if (this.url.startsWith('https://soundcloud.com/')) {
      // TODO: Implement
    } else if (this.url.startsWith('https://vimeo.com/')) {
      // TODO: Implement
    } else if (this.url.startsWith('https://youtu.be/')) {
      const text = this.url.replace('https://youtu.be/', '');
      return ({ type: SearchEmbeddedType.YouTube, text });
    } else if (this.url.startsWith('https://www.youtube.com/watch') || this.url.startsWith('https://youtube.com/watch')) {
      const url = new URL(this.url);
      const text = url.searchParams.get('v');
      if (!text)
        return null;

      return ({ type: SearchEmbeddedType.YouTube, text });
    }

    return null;
  }
}