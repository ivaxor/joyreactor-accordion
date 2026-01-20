import { Component } from '@angular/core';
import { NavbarHealthCheck } from "../navbar-healthcheck/navbar-healthcheck";

@Component({
  selector: 'app-navbar-root',
  imports: [NavbarHealthCheck],
  templateUrl: './navbar-root.html',
  styleUrl: './navbar-root.scss',
})
export class NavbarRoot { }