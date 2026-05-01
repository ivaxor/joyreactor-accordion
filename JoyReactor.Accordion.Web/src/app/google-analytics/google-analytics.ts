import { Component, inject, OnInit } from '@angular/core';
import { NgcCookieConsentService } from 'ngx-cookieconsent';
import { ConfigService } from '../../services/config-service/config-service';
import { filter, switchMap, take, tap } from 'rxjs';

@Component({
  selector: 'app-google-analytics',
  imports: [],
  templateUrl: './google-analytics.html',
  styleUrl: './google-analytics.scss',
})
export class GoogleAnalytics implements OnInit {
  private configService = inject(ConfigService);
  private cookieConsentService = inject(NgcCookieConsentService);

  ngOnInit(): void {
    this.configService.config$.pipe(
      filter(config => !!config && config.googleAnalyticsEnabled),
      take(1),
      switchMap(config => {
        if (this.cookieConsentService.hasConsented())
          this.injectScripts(config!.googleAnalyticsId);

        return this.cookieConsentService.statusChange$.pipe(
          filter(event => event.status === 'allow'),
          tap(() => this.injectScripts(config!.googleAnalyticsId)));
      })).subscribe();

    if (!this.configService.config!.googleAnalyticsEnabled)
      return;

    const script1 = document.createElement('script');
    script1.async = true;
    script1.src = `https://www.googletagmanager.com/gtag/js?id=${this.configService.config!.googleAnalyticsId}`;
    document.head.appendChild(script1);

    const script2 = document.createElement('script');
    script2.innerHTML = `
  window.dataLayer = window.dataLayer || [];
  function gtag() { dataLayer.push(arguments); }
  gtag('js', new Date());
  gtag('config', '${this.configService.config!.googleAnalyticsId}');
    `;
    document.head.appendChild(script2);
  }

  private injectScripts(id: string): void {
    if ((window as any).gtagInitialized)
      return;

    (window as any).gtagInitialized = true;

    const script1 = document.createElement('script');
    script1.async = true;
    script1.src = `https://www.googletagmanager.com/gtag/js?id=${id}`;
    document.head.appendChild(script1);

    const script2 = document.createElement('script');
    script2.innerHTML = `
      window.dataLayer = window.dataLayer || [];
      function gtag() { dataLayer.push(arguments); }
      gtag('js', new Date());
      gtag('config', '${id}');
    `;
    document.head.appendChild(script2);
  }
}
