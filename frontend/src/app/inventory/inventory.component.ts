import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { CurrencyService } from '../core/currency.service';
import { ErpApiService } from '../core/erp-api.service';
import { LanguageService } from '../core/language.service';
import { Material, StockBalance, StockMovement, StockMovementType, Warehouse } from '../core/models';
import { TranslatePipe } from '../core/translate.pipe';

@Component({
  selector: 'app-inventory',
  imports: [ReactiveFormsModule, CurrencyPipe, DecimalPipe, DatePipe, TranslatePipe],
  templateUrl: './inventory.component.html'
})
export class InventoryComponent implements OnInit {
  private readonly api = inject(ErpApiService);
  private readonly fb = inject(FormBuilder);
  private readonly language = inject(LanguageService);
  readonly currencyService = inject(CurrencyService);

  readonly balances = signal<StockBalance[]>([]);
  readonly movements = signal<StockMovement[]>([]);
  readonly materials = signal<Material[]>([]);
  readonly warehouses = signal<Warehouse[]>([]);
  readonly types: StockMovementType[] = ['Adjustment', 'Inbound', 'Outbound'];
  readonly error = signal('');

  readonly form = this.fb.nonNullable.group({
    materialId: ['', Validators.required],
    warehouseId: ['', Validators.required],
    type: ['Adjustment' as StockMovementType],
    quantity: [0, [Validators.required, Validators.min(0.001)]],
    unitCost: [0],
    reference: [''],
    notes: ['']
  });

  ngOnInit() {
    this.currencyService.load();
    this.load();
  }

  load() {
    this.api.stockBalances().subscribe((items) => this.balances.set(items));
    this.api.stockMovements().subscribe((items) => this.movements.set(items));
    this.api.materials().subscribe((items) => {
      this.materials.set(items);
      if (!this.form.value.materialId && items[0]) {
        this.form.patchValue({ materialId: items[0].id });
      }
    });
    this.api.warehouses().subscribe((items) => {
      this.warehouses.set(items);
      if (!this.form.value.warehouseId && items[0]) {
        this.form.patchValue({ warehouseId: items[0].id });
      }
    });
  }

  saveMovement() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    this.api.createStockMovement({
      ...value,
      quantity: Number(value.quantity),
      unitCost: value.unitCost ? Number(value.unitCost) : null,
      reference: value.reference || null,
      notes: value.notes || null
    }).subscribe({
      next: () => {
        this.form.patchValue({ quantity: 0, reference: '', notes: '' });
        this.load();
      },
      error: () => this.error.set(this.language.language() === 'en' ? 'Could not register the movement.' : 'Nao foi possivel registrar a movimentacao.')
    });
  }
}
