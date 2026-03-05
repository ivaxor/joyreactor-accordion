import { Component, signal } from '@angular/core';
import { VoteCompare } from "../vote-compare/vote-compare";

@Component({
  selector: 'app-vote-root',
  imports: [VoteCompare],
  templateUrl: './vote-root.html',
  styleUrl: './vote-root.scss',
})
export class VoteRoot {
  started = signal(false);

  start(): void {
    this.started.set(true);
  }
}