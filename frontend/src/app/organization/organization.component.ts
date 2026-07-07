import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { ErpApiService } from '../core/erp-api.service';
import { LanguageService } from '../core/language.service';
import { Branch, Company, CostCenter } from '../core/models';
import { TranslatePipe } from '../core/translate.pipe';

@Component({
  selector: 'app-organization',
  imports: [ReactiveFormsModule, TranslatePipe],
  templateUrl: './organization.component.html'
})
export class OrganizationComponent implements OnInit {
  private readonly api = inject(ErpApiService);
  private readonly fb = inject(FormBuilder);
  private readonly language = inject(LanguageService);

  readonly companies = signal<Company[]>([]);
  readonly branches = signal<Branch[]>([]);
  readonly costCenters = signal<CostCenter[]>([]);
  readonly error = signal('');

  readonly companyForm = this.fb.nonNullable.group({
    id: [''],
    code: ['', [Validators.required, Validators.maxLength(20)]],
    name: ['', [Validators.required, Validators.maxLength(160)]],
    taxId: ['', [Validators.maxLength(40)]],
    isActive: [true]
  });

  readonly branchForm = this.fb.nonNullable.group({
    id: [''],
    companyId: ['', Validators.required],
    code: ['', [Validators.required, Validators.maxLength(20)]],
    name: ['', [Validators.required, Validators.maxLength(160)]],
    taxId: ['', [Validators.maxLength(40)]],
    address: ['', [Validators.maxLength(240)]],
    isActive: [true]
  });

  readonly costCenterForm = this.fb.nonNullable.group({
    id: [''],
    companyId: ['', Validators.required],
    code: ['', [Validators.required, Validators.maxLength(30)]],
    name: ['', [Validators.required, Validators.maxLength(120)]],
    description: ['', [Validators.maxLength(300)]],
    isActive: [true]
  });

  ngOnInit() {
    this.load();
  }

  load() {
    this.api.companies().subscribe((companies) => {
      this.companies.set(companies);
      this.ensureCompanySelections();
    });
    this.api.branches().subscribe((branches) => this.branches.set(branches));
    this.api.costCenters().subscribe((costCenters) => this.costCenters.set(costCenters));
  }

  editCompany(company: Company) {
    this.companyForm.patchValue({ ...company, taxId: company.taxId ?? '' });
  }

  resetCompany() {
    this.companyForm.reset({ id: '', code: '', name: '', taxId: '', isActive: true });
  }

  saveCompany() {
    if (this.companyForm.invalid) {
      this.companyForm.markAllAsTouched();
      return;
    }

    const value = this.companyForm.getRawValue();
    this.api.saveCompany({ ...value, taxId: value.taxId || null }).subscribe({
      next: () => {
        this.resetCompany();
        this.load();
      },
      error: () => this.error.set(this.language.language() === 'en' ? 'Could not save the company.' : 'Nao foi possivel salvar a empresa.')
    });
  }

  editBranch(branch: Branch) {
    this.branchForm.patchValue({
      ...branch,
      taxId: branch.taxId ?? '',
      address: branch.address ?? ''
    });
  }

  resetBranch() {
    this.branchForm.reset({
      id: '',
      companyId: this.companies()[0]?.id ?? '',
      code: '',
      name: '',
      taxId: '',
      address: '',
      isActive: true
    });
  }

  saveBranch() {
    if (this.branchForm.invalid) {
      this.branchForm.markAllAsTouched();
      return;
    }

    const value = this.branchForm.getRawValue();
    this.api.saveBranch({
      ...value,
      taxId: value.taxId || null,
      address: value.address || null
    }).subscribe({
      next: () => {
        this.resetBranch();
        this.load();
      },
      error: () => this.error.set(this.language.language() === 'en' ? 'Could not save the branch.' : 'Nao foi possivel salvar a filial.')
    });
  }

  editCostCenter(costCenter: CostCenter) {
    this.costCenterForm.patchValue({
      ...costCenter,
      description: costCenter.description ?? ''
    });
  }

  resetCostCenter() {
    this.costCenterForm.reset({
      id: '',
      companyId: this.companies()[0]?.id ?? '',
      code: '',
      name: '',
      description: '',
      isActive: true
    });
  }

  saveCostCenter() {
    if (this.costCenterForm.invalid) {
      this.costCenterForm.markAllAsTouched();
      return;
    }

    const value = this.costCenterForm.getRawValue();
    this.api.saveCostCenter({ ...value, description: value.description || null }).subscribe({
      next: () => {
        this.resetCostCenter();
        this.load();
      },
      error: () => this.error.set(this.language.language() === 'en' ? 'Could not save the cost center.' : 'Nao foi possivel salvar o centro de custo.')
    });
  }

  private ensureCompanySelections() {
    const companyId = this.companies()[0]?.id ?? '';
    if (!companyId) {
      return;
    }

    if (!this.companies().some((company) => company.id === this.branchForm.value.companyId)) {
      this.branchForm.patchValue({ companyId });
    }

    if (!this.companies().some((company) => company.id === this.costCenterForm.value.companyId)) {
      this.costCenterForm.patchValue({ companyId });
    }
  }
}
