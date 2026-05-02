import { ApplicationConfig, inject, LOCALE_ID, provideAppInitializer, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import { provideHttpClient } from '@angular/common/http';
import { ConfigService } from '../services/config-service/config-service';
import { HealthCheckService } from '../services/healthcheck-service/healthcheck-service';
import { NgcCookieConsentConfig, NgcCookieConsentService, provideNgcCookieConsent } from 'ngx-cookieconsent';

const cookieConsentConfig: NgcCookieConsentConfig = {
  cookie: { domain: window.location.hostname },
  position: 'bottom-right',
  theme: 'block',
  palette: {
    popup: { background: '#626262', text: '#fcfcfc' },
    button: { background: '#fdb201', text: '#000000' },
  },
  type: 'opt-in',
  content: {
    message: 'Этот веб-сайт хочет использовать cookies чтобы следить за вашими действиями.',
    allow: 'Разрешить',
    deny: 'Запретить',
    link: 'Google Analytics Terms of Service',
    href: 'https://marketingplatform.google.com/about/analytics/terms/',
    policy: '🍪'
  },
};

export const appConfig: ApplicationConfig = {
  providers: [
    { provide: LOCALE_ID, useValue: navigator.language },
    provideNgcCookieConsent(cookieConsentConfig),
    provideAppInitializer(() => {
      const gtagConsentUpdate = (status: string) => {
        console.log(status);
        const googleStatus = status === сookieConsentService.getStatus().allow! ? 'granted' : 'denied';
        gtag('consent', 'update', {
          'ad_storage': googleStatus,
          'ad_user_data': googleStatus,
          'ad_personalization': googleStatus,
          'analytics_storage': googleStatus,
        });
      };

      const сookieConsentService = inject(NgcCookieConsentService);
      сookieConsentService.statusChange$.subscribe(event => gtagConsentUpdate(event.status));
      if (сookieConsentService.hasConsented())
        gtagConsentUpdate(сookieConsentService.getStatus().allow!);
    }),
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(),
    provideAppInitializer((configService = inject(ConfigService)) => configService.initialize()),
    provideAppInitializer((healthcheckService = inject(HealthCheckService)) => healthcheckService.initialize()),
  ]
};