import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { CurrencyService } from '../core/currency.service';
import { ErpApiService } from '../core/erp-api.service';
import { LanguageService } from '../core/language.service';
import { BusinessPartner, CatalogItem, Material, UnitOfMeasure } from '../core/models';
import { TranslatePipe } from '../core/translate.pipe';

@Component({
  selector: 'app-materials',
  imports: [ReactiveFormsModule, CurrencyPipe, DecimalPipe, TranslatePipe],
  templateUrl: './materials.component.html'
})
export class MaterialsComponent implements OnInit {
  private readonly api = inject(ErpApiService);
  private readonly fb = inject(FormBuilder);
  private readonly language = inject(LanguageService);
  readonly currencyService = inject(CurrencyService);

  readonly materials = signal<Material[]>([]);
  readonly categories = signal<CatalogItem[]>([]);
  readonly units = signal<UnitOfMeasure[]>([]);
  readonly suppliers = signal<BusinessPartner[]>([]);
  readonly search = signal('');
  readonly saving = signal(false);
  readonly error = signal('');

  readonly form = this.fb.nonNullable.group({
    id: [''],
    code: ['', Validators.required],
    description: ['', Validators.required],
    categoryId: ['', Validators.required],
    unitOfMeasureId: ['', Validators.required],
    supplierId: [''],
    standardCost: [0, Validators.min(0)],
    salePrice: [0, Validators.min(0)],
    minimumStock: [0, Validators.min(0)],
    isActive: [true]
  });

  ngOnInit() {
    this.currencyService.load();
    this.loadLookups();
    this.loadMaterials();
  }

  loadLookups() {
    this.api.categories().subscribe((items) => {
      this.categories.set(items);
      if (!this.form.value.categoryId && items[0]) {
        this.form.patchValue({ categoryId: items[0].id });
      }
    });

    this.api.units().subscribe((items) => {
      this.units.set(items);
      if (!this.form.value.unitOfMeasureId && items[0]) {
        this.form.patchValue({ unitOfMeasureId: items[0].id });
      }
    });

    this.api.suppliers().subscribe((items) => this.suppliers.set(items));
  }

  loadMaterials() {
    this.api.materials(this.search()).subscribe({
      next: (materials) => this.materials.set(materials),
      error: () => this.error.set(this.language.language() === 'en' ? 'Could not load materials.' : 'Nao foi possivel carregar materiais.')
    });
  }

  edit(material: Material) {
    this.form.patchValue({
      id: material.id,
      code: material.code,
      description: material.description,
      categoryId: material.categoryId,
      unitOfMeasureId: material.unitOfMeasureId,
      supplierId: material.supplierId ?? '',
      standardCost: material.standardCost,
      salePrice: material.salePrice,
      minimumStock: material.minimumStock,
      isActive: material.isActive
    });
  }

  reset() {
    this.form.reset({
      id: '',
      code: '',
      description: '',
      categoryId: this.categories()[0]?.id ?? '',
      unitOfMeasureId: this.units()[0]?.id ?? '',
      supplierId: '',
      standardCost: 0,
      salePrice: 0,
      minimumStock: 0,
      isActive: true
    });
  }

  save() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.error.set('');
    const value = this.form.getRawValue();

    this.api.saveMaterial({
      ...value,
      supplierId: value.supplierId || null,
      standardCost: Number(value.standardCost),
      salePrice: Number(value.salePrice),
      minimumStock: Number(value.minimumStock)
    }).subscribe({
      next: () => {
        this.saving.set(false);
        this.reset();
        this.loadMaterials();
      },
      error: () => {
        this.saving.set(false);
        this.error.set(this.language.language() === 'en' ? 'Could not save the material.' : 'Nao foi possivel salvar o material.');
      }
    });
  }

  remove(material: Material) {
    this.api.deleteMaterial(material.id).subscribe(() => this.loadMaterials());
  }
}
