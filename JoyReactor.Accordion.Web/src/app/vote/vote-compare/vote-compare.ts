import { ChangeDetectorRef, Component, inject, OnInit, signal } from '@angular/core';
import { VoteResponse } from '../../../services/vote-service/vote-response';
import { JoyReactorMediaMetadataService } from '../../../services/joyreactor-media-metadata-service/joyreactor-media-metadata-service';
import { VoteService } from '../../../services/vote-service/vote-service';
import { catchError, Observable, of, tap, throwError } from 'rxjs';
import { Subscription } from 'dexie';

@Component({
  selector: 'app-vote-compare',
  imports: [],
  templateUrl: './vote-compare.html',
  styleUrl: './vote-compare.scss',
})
export class VoteCompare implements OnInit {
  private changeDetector = inject(ChangeDetectorRef);
  private voteService = inject(VoteService);
  private joyReactorMediaMetadataService = inject(JoyReactorMediaMetadataService);

  votes!: VoteResponse[];
  vote: VoteResponse | null = null;

  originalImageLoaded = signal(false);
  originalImageRetryCounter: number = 0;
  originalImageUrl!: string;

  duplicateImageLoaded = signal(false);
  duplicateImageUrl!: string;
  duplicateImageRetryCounter: number = 0;

  ngOnInit(): void {
    this.loadNewVotes();
  }

  originalImageFailed(): void {
    setTimeout(() => {
      const url = new URL(this.joyReactorMediaMetadataService.createImageUrl(this.vote!.originalPictureAttributeId));
      this.originalImageRetryCounter++;
      url.searchParams.append('retry', this.originalImageRetryCounter.toString());

      this.originalImageUrl = url.toString();
    }, 1000);
  }

  duplicateImageFailed(): void {
    setTimeout(() => {
      const url = new URL(this.joyReactorMediaMetadataService.createImageUrl(this.vote!.duplicatePictureAttributeId));
      this.duplicateImageRetryCounter++;
      url.searchParams.append('retry', this.duplicateImageRetryCounter.toString());

      this.duplicateImageUrl = url.toString();
    }, 1000);
  }

  getAfterDate(): string {
    const voteAfterDate = localStorage.getItem('voteAfterDate');
    if (voteAfterDate)
      return voteAfterDate;
    else
      return new Date(2026, 0, 1, 1, 0, 0, 0).toISOString();
  }

  setAfterDate(date: string): void {
    localStorage.setItem('voteAfterDate', date);
  }

  loadNewVotes(): void {
    const afterDate = this.getAfterDate();
    this.voteService.getAfter(afterDate)
      .subscribe(votes => {
        this.votes = votes.reverse();
        if (this.vote)
          this.changeDetector.markForCheck();
        else
          this.goToNextVote();
      });
  }

  submitVote(yes: boolean): void {
    this.voteService.vote(this.vote!.id, yes)
      .pipe(
        catchError((error) => {
          if (error?.status === 409)
            return of(null);

          return throwError(() => error);
        }),
        tap(() => this.setAfterDate(this.vote!.createdAt)))
      .subscribe(() => this.goToNextVote());
  }

  goToNextVote(): void {
    const newVote = this.votes.pop()!;

    if (this.vote?.originalPictureAttributeId !== newVote.originalPictureAttributeId)
      this.originalImageLoaded.set(false);

    if (this.vote?.duplicatePictureAttributeId !== newVote.duplicatePictureAttributeId)
      this.duplicateImageLoaded.set(false);

    this.originalImageUrl = this.joyReactorMediaMetadataService.createImageUrl(newVote.originalPictureAttributeId);
    this.duplicateImageUrl = this.joyReactorMediaMetadataService.createImageUrl(newVote.duplicatePictureAttributeId);
    this.vote = newVote;

    if (this.votes.length === 0)
      this.loadNewVotes();

    this.changeDetector.markForCheck();
  }
}