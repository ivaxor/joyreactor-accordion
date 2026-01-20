import { ChangeDetectorRef, Component, inject } from '@angular/core';
import { HealthCheckService } from '../../../services/healthcheck-service/healthcheck-service';
import { HealthCheckResult } from '../../../services/healthcheck-service/healthcheck-result';
import { filter } from 'rxjs';

@Component({
  selector: 'app-navbar-healthcheck',
  imports: [],
  templateUrl: './navbar-healthcheck.html',
  styleUrl: './navbar-healthcheck.scss',
})
export class NavbarHealthCheck {
  private healthCheckService = inject(HealthCheckService);
  private changeDetector = inject(ChangeDetectorRef);

  healthCheckResults: HealthCheckResult[] | null = null;
  healthy: boolean | null = null;
  unhealthyResources: string[] | null = null;

  ngOnInit(): void {
    this.healthCheckService.results$
      .pipe(
        filter(results => results !== null))
      .subscribe(results => {
        this.healthCheckResults = results;
        this.healthy = results.every(result => result.success);
        this.unhealthyResources = results.filter(result => result.success === false).map(result => result.name);
        this.changeDetector.markForCheck();
      });
  }
}