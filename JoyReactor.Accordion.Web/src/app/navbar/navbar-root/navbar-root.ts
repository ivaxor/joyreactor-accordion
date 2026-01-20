import { Component } from '@angular/core';
import { NavbarHealthcheck } from "../navbar-healthcheck/navbar-healthcheck";

@Component({
  selector: 'app-navbar-root',
  imports: [NavbarHealthcheck],
  templateUrl: './navbar-root.html',
  styleUrl: './navbar-root.scss',
})
export class NavbarRoot { }