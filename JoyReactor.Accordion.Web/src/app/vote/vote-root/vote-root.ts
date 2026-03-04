import { Component, inject, OnInit, signal } from '@angular/core';
import { VoteService } from '../../../services/vote-service/vote-service';
import { VoteResponse } from '../../../services/vote-service/vote-response';
import { Observable, tap } from 'rxjs';
import { VoteDetails } from "../vote-details/vote-details";

@Component({
  selector: 'app-vote-root',
  imports: [VoteDetails],
  templateUrl: './vote-root.html',
  styleUrl: './vote-root.scss',
})
export class VoteRoot implements OnInit {
  private voteService = inject(VoteService);
  private votes: VoteResponse[] = [];
  currentVote = signal<VoteResponse | null>(null);

  ngOnInit(): void {
    this.loadNewVotes().subscribe();
  }

  loadNewVotes(): Observable<any> {
    const date = new Date(2000, 0, 0, 0, 0, 0, 0);
    return this.voteService.getAfter(date)
      .pipe(tap(votes => this.votes = votes));
  }

  vote(id: string, yes: boolean): Observable<any> {
    return this.voteService.vote(id, yes)
      .pipe(tap(() => this.goToNextVote()));
  }

  goToNextVote(): void {
    if (this.votes.length === 0)
      this.loadNewVotes().subscribe(() => this.goToNextVote());
    else
      this.currentVote.set(this.votes.pop()!);
  }
}