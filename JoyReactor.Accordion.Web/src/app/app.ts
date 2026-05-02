import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Footer } from './footer/footer';
import { NavbarRoot } from './navbar/navbar-root/navbar-root';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Footer, NavbarRoot],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  protected readonly title = signal('JR Accordion');
}