import { ChangeDetectorRef, Component, inject, OnInit, signal } from '@angular/core';
import { VoteService } from '../../../services/vote-service/vote-service';
import { VoteResponse } from '../../../services/vote-service/vote-response';
import { JoyReactorMediaMetadataService } from '../../../services/joyreactor-media-metadata-service/joyreactor-media-metadata-service';
import { ActivatedRoute, Router } from '@angular/router';

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

  joyReactorMediaMetadataService = inject(JoyReactorMediaMetadataService);

  page = signal<number>(0);
  votes = signal<VoteResponse[] | null>(null);

  ngOnInit(): void {
    this.activatedRoute.queryParams
      .subscribe(queryParams => {
        const pageParam = queryParams['page'];
        if (!pageParam) {
          this.router.navigate(['/votes'], { queryParams: { page: 0 } });
          return;
        }

        const page = Number.parseInt(pageParam);
        this.page.set(page);

        this.voteService
          .getPage(this.page())
          .subscribe(votes => this.votes.set(votes));
      });
  }

  goToPage(page: number): void {
    this.router.navigate(['/votes'], { queryParams: { page } });
  }
}