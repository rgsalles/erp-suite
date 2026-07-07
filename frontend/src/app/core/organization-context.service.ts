import { computed, inject, Injectable, signal } from '@angular/core';

import { ErpApiService } from './erp-api.service';
import { Branch, Company, Warehouse } from './models';

const COMPANY_STORAGE_KEY = 'erp-suite-company';
const BRANCH_STORAGE_KEY = 'erp-suite-branch';

@Injectable({ providedIn: 'root' })
export class OrganizationContextService {
  private readonly api = inject(ErpApiService);
  private loaded = false;

  readonly companies = signal<Company[]>([]);
  readonly branches = signal<Branch[]>([]);
  readonly selectedCompanyId = signal(localStorage.getItem(COMPANY_STORAGE_KEY) || '');
  readonly selectedBranchId = signal(localStorage.getItem(BRANCH_STORAGE_KEY) || '');

  readonly selectedCompany = computed(() =>
    this.companies().find((company) => company.id === this.selectedCompanyId())
    ?? this.companies().find((company) => company.isActive)
    ?? this.companies()[0]);

  readonly companyBranches = computed(() => {
    const company = this.selectedCompany();
    return company ? this.branches().filter((branch) => branch.companyId === company.id) : [];
  });

  readonly selectedBranch = computed(() =>
    this.companyBranches().find((branch) => branch.id === this.selectedBranchId())
    ?? this.companyBranches().find((branch) => branch.isActive)
    ?? this.companyBranches()[0]);

  load(force = false) {
    if (this.loaded && !force) {
      return;
    }

    this.loaded = true;
    this.api.companies().subscribe((companies) => {
      this.companies.set(companies);
      this.ensureSelectedCompany();
    });
    this.api.branches().subscribe((branches) => {
      this.branches.set(branches);
      this.ensureSelectedBranch();
    });
  }

  refresh() {
    this.loaded = false;
    this.load(true);
  }

  selectCompany(companyId: string) {
    this.selectedCompanyId.set(companyId);
    localStorage.setItem(COMPANY_STORAGE_KEY, companyId);
    this.ensureSelectedBranch();
  }

  selectBranch(branchId: string) {
    this.selectedBranchId.set(branchId);
    localStorage.setItem(BRANCH_STORAGE_KEY, branchId);
  }

  filterWarehouses(warehouses: Warehouse[]) {
    const branch = this.selectedBranch();
    return branch ? warehouses.filter((warehouse) => warehouse.branchId === branch.id) : warehouses;
  }

  private ensureSelectedCompany() {
    const selected = this.selectedCompany();
    if (selected && selected.id !== this.selectedCompanyId()) {
      this.selectCompany(selected.id);
    }
  }

  private ensureSelectedBranch() {
    const selected = this.selectedBranch();
    if (selected && selected.id !== this.selectedBranchId()) {
      this.selectBranch(selected.id);
      return;
    }

    if (!selected && this.selectedBranchId()) {
      this.selectBranch('');
    }
  }
}
