import { inject, Injectable } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { SearchMediaResponse } from './search-media-response';
import { HttpClient } from '@angular/common/http';
import { SearchMediaDownloadRequest } from './search-media-download-request';
import { ConfigService } from '../config-service/config-service';
import { SearchMediaHistoryService } from '../search-media-history-service/search-media-history-service';

@Injectable({
  providedIn: 'root',
})
export class SearchMediaService {
  private configService = inject(ConfigService);
  private http = inject(HttpClient);
  private searchMediaHistoryService = inject(SearchMediaHistoryService);
  private readonly limit = 3;

  searchUpload(file: File, threshold: number): Observable<SearchMediaResponse[]> {
    const url = `${this.configService.config!.apiRoot}/search/media/upload`;
    const request = new FormData();
    request.append('media', file, file.name);
    request.append('limit', this.limit.toString());
    request.append('threshold', threshold.toString());

    return this.http.post<SearchMediaResponse[]>(url, request)
      .pipe(tap(response => {
        this.searchMediaHistoryService.addUpload(file, response);
        this.searchMediaHistoryService.setFilePreviewUrl(file);
      }));
  }

  searchDownload(mediaUrl: string, threshold: number): Observable<SearchMediaResponse[]> {
    const url = `${this.configService.config!.apiRoot}/search/media/download`;
    const request: SearchMediaDownloadRequest = {
      mediaUrl,
      limit: this.limit,
      threshold,
    };

    return this.http.post<SearchMediaResponse[]>(url, request)
      .pipe(tap(response => this.searchMediaHistoryService.addDownload(mediaUrl, response)));
  }
}