import { inject, Injectable } from '@angular/core';
import { ConfigService } from '../config-service/config-service';
import { HttpClient } from '@angular/common/http';
import { SearchEmbeddedRequest } from './search-embedded-request';
import { SearchEmbeddedResponse } from './search-embedded-response';
import { Observable, tap } from 'rxjs';
import { SearchEmbeddedHistoryService } from '../search-embedded-history-service/search-embedded-history-service';

@Injectable({
  providedIn: 'root',
})
export class SearchEmbeddedService {
  private configService = inject(ConfigService);
  private http = inject(HttpClient);
  private searchEmbeddedHistoryService = inject(SearchEmbeddedHistoryService);

  search(request: SearchEmbeddedRequest): Observable<SearchEmbeddedResponse> {
    const url = `${this.configService.config!.apiRoot}/search/embedded`;

    return this.http.post<SearchEmbeddedResponse>(url, request)
      .pipe(tap(response => this.searchEmbeddedHistoryService.add(request, response)));
  }
}