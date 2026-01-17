import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { SearchResponse } from './search-response';
import { HttpClient } from '@angular/common/http';
import { SearchDownloadRequest } from './search-download-request';
import { ConfigService } from '../config-service/config-service';

@Injectable({
  providedIn: 'root',
})
export class SearchService {
  private configService = inject(ConfigService);
  private http = inject(HttpClient);

  searchUpload(file: File): Observable<SearchResponse[]> {
    const url = `${this.configService.config!.apiRoot}/search/media/upload`;
    const request = new FormData();
    request.append('media', file, file.name);
    return this.http.post<SearchResponse[]>(url, request);
  }

  searchDownload(mediaUrl: string): Observable<SearchResponse[]> {
    const url = `${this.configService.config!.apiRoot}/search/media/download`;
    const request: SearchDownloadRequest = { mediaUrl };
    return this.http.post<SearchResponse[]>(url, request);
  }
}