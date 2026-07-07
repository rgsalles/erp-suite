import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { ErpApiService } from '../core/erp-api.service';
import {
  BusinessPartner,
  FinancialEntry,
  FinancialEntryStatus,
  FinancialEntryType,
  FinancialSummary
} from '../core/models';

@Component({
  selector: 'app-finance',
  imports: [CurrencyPipe, DatePipe, FormsModule],
  templateUrl: './finance.component.html'
})
export class FinanceComponent implements OnInit {
  private readonly api = inject(ErpApiService);

  readonly summary = signal<FinancialSummary | null>(null);
  readonly payables = signal<FinancialEntry[]>([]);
  readonly receivables = signal<FinancialEntry[]>([]);
  readonly suppliers = signal<BusinessPartner[]>([]);
  readonly customers = signal<BusinessPartner[]>([]);
  readonly activeTab = signal<FinancialEntryType>('Payable');
  readonly statusFilter = signal<FinancialEntryStatus | ''>('');
  readonly error = signal('');

  entryType: FinancialEntryType = 'Payable';
  partnerId = '';
  dueDate = this.toDateInput(new Date(Date.now() + 7 * 24 * 60 * 60 * 1000));
  amount = 0;
  description = '';

  readonly currentEntries = computed(() => this.activeTab() === 'Payable' ? this.payables() : this.receivables());

  ngOnInit() {
    this.api.suppliers().subscribe((suppliers) => {
      this.suppliers.set(suppliers);
      this.ensurePartnerSelected();
    });
    this.api.customers().subscribe((customers) => {
      this.customers.set(customers);
      this.ensurePartnerSelected();
    });
    this.load();
  }

  load() {
    this.api.financialSummary().subscribe({
      next: (summary) => {
        this.summary.set(summary);
        this.error.set('');
      },
      error: () => this.error.set('Nao foi possivel carregar o financeiro. Apenas Admin e Manager podem acessar.')
    });

    const status = this.statusFilter() || undefined;
    this.api.payables(status).subscribe((entries) => this.payables.set(entries));
    this.api.receivables(status).subscribe((entries) => this.receivables.set(entries));
  }

  changeTab(type: FinancialEntryType) {
    this.activeTab.set(type);
  }

  changeType(type: FinancialEntryType) {
    this.entryType = type;
    this.ensurePartnerSelected();
  }

  createEntry() {
    if (!this.partnerId || !this.dueDate || this.amount <= 0) {
      this.error.set('Informe parceiro, vencimento e valor para criar o lancamento.');
      return;
    }

    this.api.createFinancialEntry({
      type: this.entryType,
      dueDate: this.dueDate,
      amount: Number(this.amount),
      description: this.description || null,
      supplierId: this.entryType === 'Payable' ? this.partnerId : null,
      customerId: this.entryType === 'Receivable' ? this.partnerId : null
    }).subscribe({
      next: () => {
        this.amount = 0;
        this.description = '';
        this.activeTab.set(this.entryType);
        this.load();
      },
      error: () => this.error.set('Nao foi possivel criar o lancamento financeiro.')
    });
  }

  settle(entry: FinancialEntry) {
    this.api.settleFinancialEntry(entry.id, { paidAmount: entry.openAmount || entry.amount }).subscribe({
      next: () => this.load(),
      error: () => this.error.set('Nao foi possivel baixar o lancamento.')
    });
  }

  cancel(entry: FinancialEntry) {
    this.api.cancelFinancialEntry(entry.id).subscribe({
      next: () => this.load(),
      error: () => this.error.set('Nao foi possivel cancelar o lancamento.')
    });
  }

  partnerName(entry: FinancialEntry) {
    return entry.supplierName || entry.customerName || 'Sem parceiro';
  }

  currentPartners() {
    return this.entryType === 'Payable' ? this.suppliers() : this.customers();
  }

  origin(entry: FinancialEntry) {
    return entry.purchaseOrderNumber || entry.salesOrderNumber || 'Lancamento manual';
  }

  private ensurePartnerSelected() {
    const partners = this.currentPartners();
    if (!partners.some((partner) => partner.id === this.partnerId)) {
      this.partnerId = partners[0]?.id ?? '';
    }
  }

  private toDateInput(date: Date) {
    return date.toISOString().slice(0, 10);
  }
}
