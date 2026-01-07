import { Component } from '@angular/core';
import { SearchEmbedded } from "../search-embedded/search-embedded";
import { SearchPicture } from "../search-picture/search-picture";
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-search',
  imports: [SearchEmbedded, SearchPicture, CommonModule, FormsModule],
  templateUrl: './search.html',
  styleUrl: './search.scss',
})
export class Search {
  isPicture: boolean = true;
}