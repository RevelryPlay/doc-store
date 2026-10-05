import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { routes } from './app.routes';
import { redirectOnUnauthorized } from './core/auth';

// HttpClient's built-in XSRF support reads the XSRF-TOKEN cookie and sends X-XSRF-TOKEN,
// which is exactly what the API issues, so no extra configuration is needed.
export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withInterceptors([redirectOnUnauthorized])),
  ],
};
