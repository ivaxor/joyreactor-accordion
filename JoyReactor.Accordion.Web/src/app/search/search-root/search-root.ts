import { Component } from '@angular/core';
import { SearchEmbedded } from '../search-embedded/search-embedded';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SearchMedia } from '../search-media/search-media';
import { SearchMediaHistory } from '../search-media-history/search-media-history';
import { SearchEmbeddedHistory } from '../search-embedded-history/search-embedded-history';

@Component({
  selector: 'app-search-root',
  imports: [SearchEmbedded, CommonModule, FormsModule, SearchMedia, SearchMediaHistory, SearchEmbeddedHistory],
  templateUrl: './search-root.html',
  styleUrl: './search-root.scss',
})
export class SearchRoot {
  isMedia: boolean = true;
}