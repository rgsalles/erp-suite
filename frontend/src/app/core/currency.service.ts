import { computed, inject, Injectable, signal } from '@angular/core';

import { CurrencyUnit, ExchangeRate } from './models';
import { ErpApiService } from './erp-api.service';

const STORAGE_KEY = 'erp-suite-currency';

@Injectable({ providedIn: 'root' })
export class CurrencyService {
  private readonly api = inject(ErpApiService);

  readonly currencies = signal<CurrencyUnit[]>([]);
  readonly exchangeRates = signal<ExchangeRate[]>([]);
  readonly selectedCode = signal(localStorage.getItem(STORAGE_KEY) || 'BRL');
  readonly selected = computed(() => {
    const currencies = this.currencies();
    return currencies.find((currency) => currency.code === this.selectedCode())
      ?? currencies.find((currency) => currency.isDefault)
      ?? currencies[0]
      ?? { id: '', code: this.selectedCode(), name: this.selectedCode(), symbol: this.selectedCode(), isDefault: true };
  });

  readonly code = computed(() => this.selected().code);
  readonly baseCurrency = computed(() => this.currencies().find((currency) => currency.isDefault) ?? this.currencies()[0]);
  readonly conversionRate = computed(() => {
    const base = this.baseCurrency();
    const selected = this.selected();
    if (!base || !selected || base.code === selected.code) {
      return 1;
    }

    const today = this.today();
    const direct = this.latestRate(base.id, selected.id, today);
    if (direct) {
      return Number(direct.rate);
    }

    const inverse = this.latestRate(selected.id, base.id, today);
    return inverse && inverse.rate > 0 ? 1 / Number(inverse.rate) : 1;
  });

  load() {
    this.api.currencies().subscribe((currencies) => {
      this.currencies.set(currencies);
      const selected = currencies.find((currency) => currency.code === this.selectedCode())
        ?? currencies.find((currency) => currency.isDefault)
        ?? currencies[0];

      if (selected) {
        this.select(selected.code);
      }
    });

    this.api.exchangeRates().subscribe({
      next: (exchangeRates) => this.exchangeRates.set(exchangeRates),
      error: () => this.exchangeRates.set([])
    });
  }

  select(code: string) {
    this.selectedCode.set(code);
    localStorage.setItem(STORAGE_KEY, code);
  }

  convert(value: number | null | undefined) {
    return Number(value ?? 0) * this.conversionRate();
  }

  private latestRate(fromCurrencyId: string, toCurrencyId: string, maxDate: string) {
    return this.exchangeRates()
      .filter((rate) =>
        rate.fromCurrencyId === fromCurrencyId &&
        rate.toCurrencyId === toCurrencyId &&
        rate.rateDate <= maxDate)
      .sort((a, b) => b.rateDate.localeCompare(a.rateDate))[0];
  }

  private today() {
    const date = new Date();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${date.getFullYear()}-${month}-${day}`;
  }
}
