import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { CurrencyService } from '../core/currency.service';
import { ErpApiService } from '../core/erp-api.service';
import { LanguageService } from '../core/language.service';
import { Branch, CurrencyUnit, ExchangeRate, UnitOfMeasure, Warehouse } from '../core/models';
import { TranslatePipe } from '../core/translate.pipe';

@Component({
  selector: 'app-catalog',
  imports: [ReactiveFormsModule, TranslatePipe],
  templateUrl: './catalog.component.html'
})
export class CatalogComponent implements OnInit {
  private readonly api = inject(ErpApiService);
  private readonly fb = inject(FormBuilder);
  private readonly language = inject(LanguageService);
  readonly currencyService = inject(CurrencyService);

  readonly units = signal<UnitOfMeasure[]>([]);
  readonly currencies = signal<CurrencyUnit[]>([]);
  readonly exchangeRates = signal<ExchangeRate[]>([]);
  readonly warehouses = signal<Warehouse[]>([]);
  readonly branches = signal<Branch[]>([]);
  readonly error = signal('');

  readonly warehouseForm = this.fb.nonNullable.group({
    id: [''],
    code: ['', [Validators.required, Validators.maxLength(20)]],
    name: ['', [Validators.required, Validators.maxLength(120)]],
    location: ['', [Validators.maxLength(200)]],
    branchId: [''],
    isActive: [true]
  });

  readonly unitForm = this.fb.nonNullable.group({
    id: [''],
    code: ['', [Validators.required, Validators.maxLength(12)]],
    name: ['', [Validators.required, Validators.maxLength(80)]]
  });

  readonly currencyForm = this.fb.nonNullable.group({
    id: [''],
    code: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(3)]],
    name: ['', [Validators.required, Validators.maxLength(80)]],
    symbol: ['', [Validators.required, Validators.maxLength(8)]],
    isDefault: [false]
  });

  readonly exchangeRateForm = this.fb.nonNullable.group({
    id: [''],
    fromCurrencyId: ['', Validators.required],
    toCurrencyId: ['', Validators.required],
    rateDate: [this.today(), Validators.required],
    rate: [0, [Validators.required, Validators.min(0.00000001)]],
    source: ['', [Validators.maxLength(120)]]
  });

  ngOnInit() {
    this.load();
  }

  load() {
    this.api.warehouses().subscribe((warehouses) => this.warehouses.set(warehouses));
    this.api.branches().subscribe((branches) => this.branches.set(branches));
    this.api.units().subscribe((units) => this.units.set(units));
    this.api.currencies().subscribe((currencies) => {
      this.currencies.set(currencies);
      this.ensureExchangeRateCurrencies();
      this.currencyService.load();
    });
    this.api.exchangeRates().subscribe((exchangeRates) => this.exchangeRates.set(exchangeRates));
  }

  editWarehouse(warehouse: Warehouse) {
    this.warehouseForm.patchValue({
      ...warehouse,
      location: warehouse.location ?? '',
      branchId: warehouse.branchId ?? ''
    });
  }

  resetWarehouse() {
    this.warehouseForm.reset({ id: '', code: '', name: '', location: '', branchId: '', isActive: true });
  }

  saveWarehouse() {
    if (this.warehouseForm.invalid) {
      this.warehouseForm.markAllAsTouched();
      return;
    }

    const value = this.warehouseForm.getRawValue();
    this.api.saveWarehouse({
      ...value,
      branchId: value.branchId || null
    }).subscribe({
      next: () => {
        this.resetWarehouse();
        this.load();
      },
      error: () => this.error.set(this.language.language() === 'en' ? 'Could not save the warehouse.' : 'Nao foi possivel salvar o almoxarifado.')
    });
  }

  editUnit(unit: UnitOfMeasure) {
    this.unitForm.patchValue(unit);
  }

  resetUnit() {
    this.unitForm.reset({ id: '', code: '', name: '' });
  }

  saveUnit() {
    if (this.unitForm.invalid) {
      this.unitForm.markAllAsTouched();
      return;
    }

    this.api.saveUnit(this.unitForm.getRawValue()).subscribe({
      next: () => {
        this.resetUnit();
        this.load();
      },
      error: () => this.error.set(this.language.language() === 'en' ? 'Could not save the unit of measure.' : 'Nao foi possivel salvar a unidade de medida.')
    });
  }

  editCurrency(currency: CurrencyUnit) {
    this.currencyForm.patchValue(currency);
  }

  resetCurrency() {
    this.currencyForm.reset({ id: '', code: '', name: '', symbol: '', isDefault: false });
  }

  saveCurrency() {
    if (this.currencyForm.invalid) {
      this.currencyForm.markAllAsTouched();
      return;
    }

    this.api.saveCurrency(this.currencyForm.getRawValue()).subscribe({
      next: () => {
        this.resetCurrency();
        this.load();
      },
      error: () => this.error.set(this.language.language() === 'en' ? 'Could not save the currency.' : 'Nao foi possivel salvar a moeda.')
    });
  }

  selectCurrency(currency: CurrencyUnit) {
    this.currencyService.select(currency.code);
  }

  editExchangeRate(exchangeRate: ExchangeRate) {
    this.exchangeRateForm.patchValue({
      ...exchangeRate,
      source: exchangeRate.source ?? ''
    });
  }

  resetExchangeRate() {
    this.exchangeRateForm.reset({
      id: '',
      fromCurrencyId: '',
      toCurrencyId: '',
      rateDate: this.today(),
      rate: 0,
      source: ''
    });
    this.ensureExchangeRateCurrencies();
  }

  saveExchangeRate() {
    if (this.exchangeRateForm.invalid) {
      this.exchangeRateForm.markAllAsTouched();
      return;
    }

    const value = this.exchangeRateForm.getRawValue();
    this.api.saveExchangeRate({
      ...value,
      rate: Number(value.rate),
      source: value.source || null
    }).subscribe({
      next: () => {
        this.resetExchangeRate();
        this.load();
        this.currencyService.load();
      },
      error: () => this.error.set(this.language.language() === 'en' ? 'Could not save the exchange rate.' : 'Nao foi possivel salvar a cotacao.')
    });
  }

  private ensureExchangeRateCurrencies() {
    const currencies = this.currencies();
    const value = this.exchangeRateForm.getRawValue();
    if (!currencies.length || (value.fromCurrencyId && value.toCurrencyId)) {
      return;
    }

    const baseCurrency = currencies.find((currency) => currency.isDefault) ?? currencies[0];
    const targetCurrency = currencies.find((currency) => currency.id !== baseCurrency.id);
    this.exchangeRateForm.patchValue({
      fromCurrencyId: value.fromCurrencyId || baseCurrency.id,
      toCurrencyId: value.toCurrencyId || targetCurrency?.id || ''
    });
  }

  private today() {
    const date = new Date();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${date.getFullYear()}-${month}-${day}`;
  }
}
