import { CommonModule } from '@angular/common';
import { Component, computed, OnDestroy, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { BehaviorSubject, Observable } from 'rxjs';

@Component({
  selector: 'app-search-embedded',
  imports: [CommonModule, FormsModule],
  templateUrl: './search-embedded.html',
  styleUrl: './search-embedded.scss',
})
export class SearchEmbedded implements OnInit, OnDestroy {
  placeholders: string[] = [
    'Вставьте ссылку на внешний ресурс',
    'https://bandcamp.com/***',
    'https://coub.com/view/*****',
    'https://soundcloud.com/***',
    'https://vimeo.com/*********',
    'https://youtu.be/***********',
    'https://www.youtube.com/watch?v=***********'];
  url: string = '';

  placeholderIndex = signal(0);
  placeholderTimerId: number | null = null;
  placeholder = computed(() => this.placeholders[this.placeholderIndex()]);

  ngOnInit(): void {
    this.placeholderTimerId = setInterval(() => {
      this.placeholderIndex.update(index => (index + 1) % this.placeholders.length);
    }, 3000);
  }

  ngOnDestroy(): void {
    if (this.placeholderTimerId)
      clearInterval(this.placeholderTimerId);
  }

  isSearchDisabled(): boolean {
    const result = this.tryToParseLink();
    return !result;
  }

  search(): void {
    const result = this.tryToParseLink();

    // TODO: Implement
  }

  tryToParseLink(): ParsedEmbeddedLink | null {
    if (this.url.startsWith('https://bandcamp.com/')) {
      // TODO: Implement
    } else if (this.url.startsWith('https://coub.com/view/')) {
      const url = this.url.replace('https://coub.com/view/', '');
      return ({ type: EmbeddedType.Coub, url: url });
    } else if (this.url.startsWith('https://soundcloud.com/')) {
      // TODO: Implement
    } else if (this.url.startsWith('https://vimeo.com/')) {
      // TODO: Implement
    } else if (this.url.startsWith('https://youtu.be/')) {
      const url = this.url.replace('https://youtu.be/', '');
      return ({ type: EmbeddedType.YouTube, url: url });
    } else if (this.url.startsWith('https://www.youtube.com/watch') || this.url.startsWith('https://youtube.com/watch')) {
      const url = new URL(this.url);
      const v = url.searchParams.get('v');
      if (!v)
        return null;

      return ({ type: EmbeddedType.YouTube, url: v });
    }

    return null;
  }
}

interface ParsedEmbeddedLink {
  type: EmbeddedType,
  url: string,
}

enum EmbeddedType {
  BandCamp,
  Coub,
  SoundCloud,
  YouTube,
  Vimeo,
}