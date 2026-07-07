import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';

import { CurrencyService } from '../core/currency.service';
import { ErpApiService } from '../core/erp-api.service';
import { LanguageService } from '../core/language.service';
import { Dashboard } from '../core/models';
import { TranslatePipe } from '../core/translate.pipe';

@Component({
  selector: 'app-dashboard',
  imports: [CurrencyPipe, DecimalPipe, TranslatePipe],
  templateUrl: './dashboard.component.html'
})
export class DashboardComponent implements OnInit {
  private readonly api = inject(ErpApiService);
  private readonly language = inject(LanguageService);
  readonly currencyService = inject(CurrencyService);

  readonly dashboard = signal<Dashboard | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');

  ngOnInit() {
    this.currencyService.load();
    this.api.dashboard().subscribe({
      next: (dashboard) => {
        this.dashboard.set(dashboard);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(this.language.language() === 'en' ? 'Could not load the dashboard.' : 'Nao foi possivel carregar o dashboard.');
        this.loading.set(false);
      }
    });
  }
}
