import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { ConfigService } from '../config-service/config-service';
import { Observable } from 'rxjs';
import { VoteResponse } from './vote-response';
import { VoteRequest } from './vote-request';

@Injectable({
  providedIn: 'root',
})
export class VoteService {
  private configService = inject(ConfigService);
  private http = inject(HttpClient);

  getAfter(createdAfter: Date): Observable<VoteResponse[]> {
    const url = `${this.configService.config!.apiRoot}/vote/`;
    return this.http.get<VoteResponse[]>(url, { params: { createdAfter: createdAfter.toISOString() } });
  }

  getPage(page: number = 0): Observable<VoteResponse[]> {
    const url = `${this.configService.config!.apiRoot}/vote/pager`;
    return this.http.get<VoteResponse[]>(url, { params: { page } });
  }

  vote(id: string, yes: boolean): Observable<any> {
    const url = `${this.configService.config!.apiRoot}/vote/`;
    const request: VoteRequest = {
      id,
      yes,
    };
    return this.http.post(url, request);
  }
}