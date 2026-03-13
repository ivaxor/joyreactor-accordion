import { Injectable } from '@angular/core';
import { SearchEmbeddedHistoryRecord } from '../search-embedded-history-service/search-embedded-history-record';
import { SearchEmbeddedType } from '../search-embedded-service/search-embedded-request';

@Injectable({
  providedIn: 'root',
})
export class JoyReactorMediaMetadataService {
  isVideo(fileNameOrPathname: string): boolean {
    return fileNameOrPathname.endsWith('webm') || fileNameOrPathname.endsWith('mp4');
  }

  isJoyReactor(url: URL): boolean {
    return url.hostname.endsWith('joyreactor.cc') || url.hostname.endsWith('joyreactor.com');
  }

  getImagePreviewUrl(url: URL): string {
    const isVideo = this.isVideo(url.pathname);
    const isJoyReactor = this.isJoyReactor(url);

    if (isVideo && isJoyReactor) {
      if (url.pathname.includes('/picture-'))
        return url.toString().replace('/picture-', '/static/picture-');
      else if (url.pathname.includes('/webm/') && url.pathname.endsWith('.webm'))
        return url.toString().replace('/webm/', '/static/').replace('.webm', '.jpeg');
      else if (url.pathname.includes('/mp4/') && url.pathname.endsWith('.mp4'))
        return url.toString().replace('/mp4/', '/static/').replace('.mp4', '.jpeg');
    }

    return url.toString();
  }

  createImageUrl(postAttributeId: number): string {
    // TODO: Add hostname ${hostName} instead of hardcoding joyreactor.cc
    return `https://img10.joyreactor.cc/pics/post/picture-${postAttributeId}.`;
  }

  getPostUrl(postId: number): string {
    return `https://joyreactor.cc/${postId}`;
  }

  getIframeUrl(historyRecord: SearchEmbeddedHistoryRecord): string {
    switch (historyRecord.type) {
      case SearchEmbeddedType.BandCamp:
        return ``;

      case SearchEmbeddedType.Coub:
        // <iframe src="//coub.com/embed/***?muted=false&autostart=false&originalSize=false&startWithHD=false" allowfullscreen frameborder="0" width="600" height="480" allow="autoplay"></iframe>
        return `https://coub.com/embed/${historyRecord.text}`;

      case SearchEmbeddedType.SoundCloud:
        return ``;
        
      case SearchEmbeddedType.Vimeo:
        // <iframe src="https://player.vimeo.com/video/***?badge=0&amp;autopause=0&amp;player_id=0&amp;app_id=58479" frameborder="0" allow="autoplay; fullscreen; picture-in-picture; clipboard-write; encrypted-media; web-share" referrerpolicy="strict-origin-when-cross-origin" style="position:absolute;top:0;left:0;width:100%;height:100%;" title="AI-Powered Me"></iframe>
        return `https://player.vimeo.com/video/${historyRecord.text}`;

      case SearchEmbeddedType.YouTube:
        // <iframe width="560" height="315" src="https://www.youtube.com/embed/***" title="YouTube video player" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share" referrerpolicy="strict-origin-when-cross-origin" allowfullscreen></iframe>
        return `https://www.youtube.com/embed/${historyRecord.text}`;

      default:
        return '';
    }
  }
}