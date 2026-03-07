import { HttpClient, HttpHeaders } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { ConfigService } from '../config-service/config-service';
import { catchError, EMPTY, Observable, of, tap, throwError } from 'rxjs';
import { VoteResponse } from './vote-response';
import { VoteRequest } from './vote-request';
import { ApiKeyService } from '../api-key-service/api-key-service';

@Injectable({
  providedIn: 'root',
})
export class VoteService {
  private configService = inject(ConfigService);
  private apiKeyService = inject(ApiKeyService);
  private http = inject(HttpClient);

  getAfter(): Observable<VoteResponse[]> {
    const createdAfter = this.getAfterDate();
    const url = `${this.configService.config!.apiRoot}/vote`;
    return this.http.get<VoteResponse[]>(url, { params: { createdAfter } });
  }

  getPage(page: number = 0): Observable<VoteResponse[]> {
    const url = `${this.configService.config!.apiRoot}/vote/pager`;
    return this.http.get<VoteResponse[]>(url, { params: { page } });
  }

  vote(vote: VoteResponse, yes: boolean): Observable<any> {
    const url = `${this.configService.config!.apiRoot}/vote`;
    const request: VoteRequest = {
      id: vote.id,
      yes,
    };
    return this.http.post(url, request)
      .pipe(
        catchError((error) => {
          if (error?.status === 409)
            return of(null);

          return throwError(() => error);
        }),
        tap(() => this.setAfterDate(vote.createdAt)));
  }

  close(vote: VoteResponse): Observable<any> {
    const apiKey = this.apiKeyService.get();
    if (apiKey === null)
      return EMPTY;

    const url = `${this.configService.config!.apiRoot}/vote/${vote.id}`;

    let headers = new HttpHeaders();
    headers = headers.append('X-API-Key', apiKey);

    return this.http.delete(url, { headers });
  }

  private getAfterDate(): string {
    const voteAfterDate = localStorage.getItem('voteAfterDate');
    if (voteAfterDate)
      return voteAfterDate;
    else
      return new Date(2026, 0, 1, 1, 0, 0, 0).toISOString();
  }

  private setAfterDate(date: string): void {
    const oldDate = new Date(this.getAfterDate());
    const newDate = new Date(date);
    if (newDate <= oldDate)
      return;

    localStorage.setItem('voteAfterDate', date);
  }
}