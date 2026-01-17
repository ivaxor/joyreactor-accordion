import { Component } from '@angular/core';
import { SearchEmbedded } from '../search-embedded/search-embedded';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SearchMedia } from "../search-media/search-media";

@Component({
  selector: 'app-search-root',
  imports: [SearchEmbedded, CommonModule, FormsModule, SearchMedia],
  templateUrl: './search-root.html',
  styleUrl: './search-root.scss',
})
export class SearchRoot {
  isMedia: boolean = true;
}