import { ApplicationConfig, inject, LOCALE_ID, provideAppInitializer, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import { provideHttpClient } from '@angular/common/http';
import { ConfigService } from '../services/config-service/config-service';
import { HealthCheckService } from '../services/healthcheck-service/healthcheck-service';
import { NgcCookieConsentConfig, provideNgcCookieConsent } from 'ngx-cookieconsent';

const cookieConsentConfig: NgcCookieConsentConfig = {
  cookie: { domain: window.location.hostname },
  position: 'top',
  theme: 'block',
  palette: { 
    popup: { background: '#222222', text: '#fcfcfcbf' },
    button: { background: '#fdb201', text: '#000000' },
  },  
  type: 'opt-in',
  content: {
    message: 'Этот веб-сайт хочет использовать cookies чтобы следить за вашими действиями.',
    allow: 'Разрешить',
    deny: 'Запретить',
    link: 'Google Analytics Terms of Service',
    href: 'https://marketingplatform.google.com/about/analytics/terms/',
  }
};

export const appConfig: ApplicationConfig = {
  providers: [
    { provide: LOCALE_ID, useValue: navigator.language },
    provideNgcCookieConsent(cookieConsentConfig),
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(),
    provideAppInitializer((configService = inject(ConfigService)) => configService.initialize()),
    provideAppInitializer((healthcheckService = inject(HealthCheckService)) => healthcheckService.initialize()),
  ]
};