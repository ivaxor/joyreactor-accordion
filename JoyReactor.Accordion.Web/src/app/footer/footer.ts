import { Component, inject } from '@angular/core';
import { NgcCookieConsentService } from 'ngx-cookieconsent';

@Component({
  selector: 'app-footer',
  imports: [],
  templateUrl: './footer.html',
  styleUrl: './footer.scss',
})
export class Footer {
  private сookieConsentService = inject(NgcCookieConsentService);

  openCookieConsent(): void {
    if (this.сookieConsentService.isOpen())
      return;

    this.сookieConsentService.open();
  }
}