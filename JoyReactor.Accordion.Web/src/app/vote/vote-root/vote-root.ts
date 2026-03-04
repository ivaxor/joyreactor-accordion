import { Component } from '@angular/core';
import { VoteCompare } from "../vote-compare/vote-compare";

@Component({
  selector: 'app-vote-root',
  imports: [VoteCompare],
  templateUrl: './vote-root.html',
  styleUrl: './vote-root.scss',
})
export class VoteRoot { }