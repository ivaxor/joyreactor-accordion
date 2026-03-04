import { Injectable } from '@angular/core';
import { SearchMediaResponse } from '../search-media-service/search-media-response';

@Injectable({
  providedIn: 'root',
})
export class JoyreactorMediaMetadataService {
  isVideo(fileNameOrPathname: string): boolean {
    return fileNameOrPathname.endsWith('webm') || fileNameOrPathname.endsWith('mp4');
  }

  isJoyReactor(url: URL): boolean {
    return url.hostname.endsWith('joyreactor.cc') || url.hostname.endsWith('joyreactor.com');
  }

  getImagePreviewUrl(url: URL, isVideo: boolean, isJoyReactor: boolean): string {
    if(isVideo && isJoyReactor) {
        if (url.pathname.includes('/picture-'))
          return url.toString().replace('/picture-', '/static/picture-');
        else if (url.pathname.includes('/webm/') && url.pathname.endsWith('.webm'))
          return url.toString().replace('/webm/', '/static/').replace('.webm', '.jpeg');
        else if (url.pathname.includes('/mp4/') && url.pathname.endsWith('.mp4'))
          return url.toString().replace('/mp4/', '/static/').replace('.mp4', '.jpeg');
    }

    return url.toString();
  }
}