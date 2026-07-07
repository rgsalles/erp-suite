import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';

import { ErpApiService } from '../core/erp-api.service';
import { Dashboard } from '../core/models';

@Component({
  selector: 'app-dashboard',
  imports: [CurrencyPipe, DecimalPipe],
  templateUrl: './dashboard.component.html'
})
export class DashboardComponent implements OnInit {
  private readonly api = inject(ErpApiService);

  readonly dashboard = signal<Dashboard | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');

  ngOnInit() {
    this.api.dashboard().subscribe({
      next: (dashboard) => {
        this.dashboard.set(dashboard);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Nao foi possivel carregar o dashboard.');
        this.loading.set(false);
      }
    });
  }
}
