import { ApplicationConfig, inject, LOCALE_ID, provideAppInitializer, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import { provideHttpClient } from '@angular/common/http';
import { ConfigService } from '../services/config-service/config-service';
import { HealthCheckService } from '../services/healthcheck-service/healthcheck-service';

export const appConfig: ApplicationConfig = {
  providers: [    
    { provide: LOCALE_ID, useValue: navigator.language },
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(),
    provideAppInitializer((configService = inject(ConfigService)) => configService.initialize()),
    provideAppInitializer((healthcheckService = inject(HealthCheckService)) => healthcheckService.initialize()),
  ]
};