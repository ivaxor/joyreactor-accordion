import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { SearchMediaResponse } from './search-media-response';
import { HttpClient } from '@angular/common/http';
import { SearchMediaDownloadRequest } from './search-media-download-request';
import { ConfigService } from '../config-service/config-service';

@Injectable({
  providedIn: 'root',
})
export class SearchMediaService {
  private configService = inject(ConfigService);
  private http = inject(HttpClient);

  searchUpload(file: File): Observable<SearchMediaResponse[]> {
    const url = `${this.configService.config!.apiRoot}/search/media/upload`;
    const request = new FormData();
    request.append('media', file, file.name);
    return this.http.post<SearchMediaResponse[]>(url, request);
  }

  searchDownload(mediaUrl: string): Observable<SearchMediaResponse[]> {
    const url = `${this.configService.config!.apiRoot}/search/media/download`;
    const request: SearchMediaDownloadRequest = { mediaUrl };
    return this.http.post<SearchMediaResponse[]>(url, request);
  }
}