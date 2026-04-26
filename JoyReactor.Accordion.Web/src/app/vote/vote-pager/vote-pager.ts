import { Component, inject, OnInit, signal } from '@angular/core';
import { VoteService } from '../../../services/vote-service/vote-service';
import { VoteResponse } from '../../../services/vote-service/vote-response';
import { JoyReactorMediaMetadataService } from '../../../services/joyreactor-media-metadata-service/joyreactor-media-metadata-service';
import { ActivatedRoute, Router } from '@angular/router';
import { ApiKeyService } from '../../../services/api-key-service/api-key-service';
import { DatePipe } from '@angular/common';

@Component({
  selector: 'app-vote-pager',
  imports: [DatePipe],
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
  openedTabs = new Map<string, Window[]>();

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

    const originalTab = window.open(duplicateUrl, "_blank");
    const duplicateTab = window.open(originalUrl, "_blank");

    const tabs: Window[] = [];
    if (originalTab) tabs.push(originalTab);
    if (duplicateTab) tabs.push(duplicateTab);

    this.openedTabs.set(vote.id, tabs);
  }

  close(vote: VoteResponse): void {
    if (!confirm('Вы точно хотите окончить это голосование?'))
      return;

    this.voteService.close(vote)
      .subscribe(() => {
        this.closeTabsForVote(vote.id);
        this.loadVotes();
      });
  }

  closeAll(duplicatePostId: number): void {
    if (!confirm('Вы точно хотите окончить все голосования?'))
      return;

    this.voteService.closeAll(duplicatePostId)
      .subscribe(() => {
        this.votes()?.filter(v => v.duplicatePostId === duplicatePostId).forEach(v => this.closeTabsForVote(v.id));
        this.loadVotes();
      });
  }

  private closeTabsForVote(voteId: string): void {
    const tabs = this.openedTabs.get(voteId);
    if (tabs) {
      tabs.filter(t => !t.closed).forEach(t => t.close());
      this.openedTabs.delete(voteId);
    }
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