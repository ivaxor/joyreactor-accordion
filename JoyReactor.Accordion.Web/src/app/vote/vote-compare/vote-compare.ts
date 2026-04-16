import { ChangeDetectorRef, Component, inject, OnInit, signal } from '@angular/core';
import { VoteResponse } from '../../../services/vote-service/vote-response';
import { JoyReactorMediaMetadataService } from '../../../services/joyreactor-media-metadata-service/joyreactor-media-metadata-service';
import { VoteService } from '../../../services/vote-service/vote-service';

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
  originalImagePostUrl!: string;

  duplicateImageLoaded = signal(false);  
  duplicateImageRetryCounter: number = 0;
  duplicateImageUrl!: string;
  duplicateImagePostUrl!: string;

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

  loadNewVotes(): void {
    this.voteService.getAfter()
      .subscribe(votes => {
        this.votes = votes.reverse();
        if (this.vote)
          this.changeDetector.markForCheck();
        else
          this.goToNextVote();
      });
  }

  submitVote(yes: boolean): void {
    this.voteService.vote(this.vote!, yes)
      .subscribe(() => this.goToNextVote());
  }

  goToNextVote(): void {
    const newVote = this.votes.pop()!;

    if (this.vote?.originalPictureAttributeId !== newVote.originalPictureAttributeId)
      this.originalImageLoaded.set(false);

    if (this.vote?.duplicatePictureAttributeId !== newVote.duplicatePictureAttributeId)
      this.duplicateImageLoaded.set(false);

    this.originalImageUrl = this.joyReactorMediaMetadataService.createImageUrl(newVote.originalPictureAttributeId);
    this.originalImagePostUrl = this.joyReactorMediaMetadataService.getPostUrl(newVote.originalPostId);

    this.duplicateImageUrl = this.joyReactorMediaMetadataService.createImageUrl(newVote.duplicatePictureAttributeId);
    this.duplicateImagePostUrl = this.joyReactorMediaMetadataService.getPostUrl(newVote.duplicatePostId);

    this.vote = newVote;

    if (this.votes.length === 0)
      this.loadNewVotes();

    this.changeDetector.markForCheck();
  }
}