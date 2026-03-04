import { Component, Input } from '@angular/core';
import { VoteResponse } from '../../../services/vote-service/vote-response';

@Component({
  selector: 'app-vote-details',
  imports: [],
  templateUrl: './vote-details.html',
  styleUrl: './vote-details.scss',
})
export class VoteDetails {
  @Input({ required: true })
  vote!: VoteResponse;

  
}
