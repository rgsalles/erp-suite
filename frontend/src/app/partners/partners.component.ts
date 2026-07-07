import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { ErpApiService } from '../core/erp-api.service';
import { LanguageService } from '../core/language.service';
import { BusinessPartner } from '../core/models';
import { TranslatePipe } from '../core/translate.pipe';

type PartnerTab = 'customers' | 'suppliers';

@Component({
  selector: 'app-partners',
  imports: [ReactiveFormsModule, TranslatePipe],
  templateUrl: './partners.component.html'
})
export class PartnersComponent implements OnInit {
  private readonly api = inject(ErpApiService);
  private readonly fb = inject(FormBuilder);
  private readonly language = inject(LanguageService);

  readonly tab = signal<PartnerTab>('customers');
  readonly customers = signal<BusinessPartner[]>([]);
  readonly suppliers = signal<BusinessPartner[]>([]);
  readonly error = signal('');

  readonly form = this.fb.nonNullable.group({
    id: [''],
    name: ['', Validators.required],
    taxId: [''],
    email: [''],
    phone: [''],
    contactName: [''],
    isActive: [true]
  });

  ngOnInit() {
    this.load();
  }

  get currentList() {
    return this.tab() === 'customers' ? this.customers() : this.suppliers();
  }

  load() {
    this.api.customers().subscribe((items) => this.customers.set(items));
    this.api.suppliers().subscribe((items) => this.suppliers.set(items));
  }

  changeTab(tab: PartnerTab) {
    this.tab.set(tab);
    this.reset();
  }

  edit(partner: BusinessPartner) {
    this.form.patchValue({
      id: partner.id,
      name: partner.name,
      taxId: partner.taxId,
      email: partner.email ?? '',
      phone: partner.phone ?? '',
      contactName: partner.contactName ?? '',
      isActive: partner.isActive
    });
  }

  reset() {
    this.form.reset({
      id: '',
      name: '',
      taxId: '',
      email: '',
      phone: '',
      contactName: '',
      isActive: true
    });
  }

  save() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const request = {
      ...value,
      email: value.email || null,
      phone: value.phone || null,
      contactName: value.contactName || null
    };

    const save = this.tab() === 'customers'
      ? this.api.saveCustomer(request)
      : this.api.saveSupplier(request);

    save.subscribe({
      next: () => {
        this.reset();
        this.load();
      },
      error: () => this.error.set(this.language.language() === 'en' ? 'Could not save the record.' : 'Nao foi possivel salvar o cadastro.')
    });
  }
}
