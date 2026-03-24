import { Component, inject, OnInit, signal } from '@angular/core';
import { VoteService } from '../../../services/vote-service/vote-service';
import { VoteResponse } from '../../../services/vote-service/vote-response';
import { JoyReactorMediaMetadataService } from '../../../services/joyreactor-media-metadata-service/joyreactor-media-metadata-service';
import { ActivatedRoute, Router } from '@angular/router';
import { ApiKeyService } from '../../../services/api-key-service/api-key-service';

@Component({
  selector: 'app-vote-pager',
  imports: [],
  templateUrl: './vote-pager.html',
  styleUrl: './vote-pager.scss',
})
export class VotePager implements OnInit {
  private activatedRoute = inject(ActivatedRoute);
  private router = inject(Router);
  private voteService = inject(VoteService);
  private apiKeyService = inject(ApiKeyService);

  joyReactorMediaMetadataService = inject(JoyReactorMediaMetadataService);

  apiKeySet = false;
  page = signal<number>(0);
  pages = signal<number>(0);
  votes = signal<VoteResponse[] | null>(null);

  ngOnInit(): void {
    this.apiKeySet = this.apiKeyService.get() !== null;

    this.activatedRoute.queryParams
      .subscribe(queryParams => {
        const pageParam = queryParams['page'];
        if (!pageParam) {
          this.router.navigate(['/votes'], { queryParams: { page: 0 } });
          return;
        }

        const page = Number.parseInt(pageParam);
        this.page.set(page);

        this.loadVotes();
      });
  }

  goToPage(page: number): void {
    this.router.navigate(['/votes'], { queryParams: { page } });
  }

  open(vote: VoteResponse): void {
    const originalUrl = this.joyReactorMediaMetadataService.getPostUrl(vote.originalPostId);
    const duplicateUrl = this.joyReactorMediaMetadataService.getPostUrl(vote.duplicatePostId);

    window.open(duplicateUrl, "_blank");
    window.open(originalUrl, "_blank");
  }

  close(vote: VoteResponse): void {
    if (!confirm('Вы точно хотите окончить это голосование?'))
      return;

    this.voteService.close(vote)
      .subscribe(() => this.loadVotes());
  }

  closeAll(duplicatePostId: number): void {
    if (!confirm('Вы точно хотите окончить все голосования?'))
      return;

    this.voteService.closeAll(duplicatePostId)
      .subscribe(() => this.loadVotes());
  }

  private loadVotes(): void {
    this.voteService
      .getPage(this.page())
      .subscribe(paged => {
        this.votes.set(paged.values);
        this.pages.set(paged.pages);
      });
  }
}