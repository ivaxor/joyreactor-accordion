import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Footer } from './footer/footer';
import { NavbarRoot } from './navbar/navbar-root/navbar-root';
import { GoogleAnalytics } from "./google-analytics/google-analytics";

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Footer, NavbarRoot, GoogleAnalytics],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  protected readonly title = signal('JR Accordion');
}