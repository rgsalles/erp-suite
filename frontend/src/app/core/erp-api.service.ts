import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { environment } from '../../environments/environment';
import {
  BusinessPartner,
  Branch,
  CatalogItem,
  Company,
  CostCenter,
  CurrencyUnit,
  AuditLog,
  Dashboard,
  ExchangeRate,
  FinancialEntry,
  FinancialEntryStatus,
  FinancialEntryType,
  FinancialSummary,
  Material,
  PurchaseOrder,
  RegisterRequest,
  SalesOrder,
  StockBalance,
  StockMovement,
  StockMovementType,
  UnitOfMeasure,
  UserRole,
  UserSummary,
  Warehouse
} from './models';

@Injectable({ providedIn: 'root' })
export class ErpApiService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  dashboard() {
    return this.http.get<Dashboard>(`${this.apiUrl}/dashboard`);
  }

  users() {
    return this.http.get<UserSummary[]>(`${this.apiUrl}/users`);
  }

  updateUser(id: string, body: { fullName: string; email: string; role: UserRole; isActive: boolean }) {
    return this.http.put<UserSummary>(`${this.apiUrl}/users/${id}`, body);
  }

  categories() {
    return this.http.get<CatalogItem[]>(`${this.apiUrl}/catalog/categories`);
  }

  companies() {
    return this.http.get<Company[]>(`${this.apiUrl}/catalog/companies`);
  }

  saveCompany(body: Partial<Company>) {
    return body.id
      ? this.http.put<Company>(`${this.apiUrl}/catalog/companies/${body.id}`, body)
      : this.http.post<Company>(`${this.apiUrl}/catalog/companies`, body);
  }

  branches() {
    return this.http.get<Branch[]>(`${this.apiUrl}/catalog/branches`);
  }

  saveBranch(body: Partial<Branch>) {
    return body.id
      ? this.http.put<Branch>(`${this.apiUrl}/catalog/branches/${body.id}`, body)
      : this.http.post<Branch>(`${this.apiUrl}/catalog/branches`, body);
  }

  costCenters() {
    return this.http.get<CostCenter[]>(`${this.apiUrl}/catalog/cost-centers`);
  }

  saveCostCenter(body: Partial<CostCenter>) {
    return body.id
      ? this.http.put<CostCenter>(`${this.apiUrl}/catalog/cost-centers/${body.id}`, body)
      : this.http.post<CostCenter>(`${this.apiUrl}/catalog/cost-centers`, body);
  }

  units() {
    return this.http.get<UnitOfMeasure[]>(`${this.apiUrl}/catalog/units`);
  }

  saveUnit(body: Partial<UnitOfMeasure>) {
    return body.id
      ? this.http.put<UnitOfMeasure>(`${this.apiUrl}/catalog/units/${body.id}`, body)
      : this.http.post<UnitOfMeasure>(`${this.apiUrl}/catalog/units`, body);
  }

  currencies() {
    return this.http.get<CurrencyUnit[]>(`${this.apiUrl}/catalog/currencies`);
  }

  saveCurrency(body: Partial<CurrencyUnit>) {
    return body.id
      ? this.http.put<CurrencyUnit>(`${this.apiUrl}/catalog/currencies/${body.id}`, body)
      : this.http.post<CurrencyUnit>(`${this.apiUrl}/catalog/currencies`, body);
  }

  exchangeRates() {
    return this.http.get<ExchangeRate[]>(`${this.apiUrl}/catalog/exchange-rates`);
  }

  saveExchangeRate(body: Partial<ExchangeRate>) {
    return body.id
      ? this.http.put<ExchangeRate>(`${this.apiUrl}/catalog/exchange-rates/${body.id}`, body)
      : this.http.post<ExchangeRate>(`${this.apiUrl}/catalog/exchange-rates`, body);
  }

  suppliers() {
    return this.http.get<BusinessPartner[]>(`${this.apiUrl}/catalog/suppliers`);
  }

  saveSupplier(body: Partial<BusinessPartner>) {
    return body.id
      ? this.http.put<BusinessPartner>(`${this.apiUrl}/catalog/suppliers/${body.id}`, body)
      : this.http.post<BusinessPartner>(`${this.apiUrl}/catalog/suppliers`, body);
  }

  customers() {
    return this.http.get<BusinessPartner[]>(`${this.apiUrl}/catalog/customers`);
  }

  saveCustomer(body: Partial<BusinessPartner>) {
    return body.id
      ? this.http.put<BusinessPartner>(`${this.apiUrl}/catalog/customers/${body.id}`, body)
      : this.http.post<BusinessPartner>(`${this.apiUrl}/catalog/customers`, body);
  }

  warehouses() {
    return this.http.get<Warehouse[]>(`${this.apiUrl}/catalog/warehouses`);
  }

  saveWarehouse(body: Partial<Warehouse>) {
    return body.id
      ? this.http.put<Warehouse>(`${this.apiUrl}/catalog/warehouses/${body.id}`, body)
      : this.http.post<Warehouse>(`${this.apiUrl}/catalog/warehouses`, body);
  }

  materials(search = '') {
    const suffix = search ? `?search=${encodeURIComponent(search)}` : '';
    return this.http.get<Material[]>(`${this.apiUrl}/materials${suffix}`);
  }

  saveMaterial(body: Partial<Material>) {
    return body.id
      ? this.http.put<Material>(`${this.apiUrl}/materials/${body.id}`, body)
      : this.http.post<Material>(`${this.apiUrl}/materials`, body);
  }

  deleteMaterial(id: string) {
    return this.http.delete<void>(`${this.apiUrl}/materials/${id}`);
  }

  stockBalances() {
    return this.http.get<StockBalance[]>(`${this.apiUrl}/inventory/balances`);
  }

  stockMovements() {
    return this.http.get<StockMovement[]>(`${this.apiUrl}/inventory/movements`);
  }

  createStockMovement(body: {
    materialId: string;
    warehouseId: string;
    type: StockMovementType;
    quantity: number;
    unitCost?: number | null;
    reference?: string | null;
    notes?: string | null;
  }) {
    return this.http.post<StockMovement>(`${this.apiUrl}/inventory/movements`, body);
  }

  purchaseOrders() {
    return this.http.get<PurchaseOrder[]>(`${this.apiUrl}/purchasing/orders`);
  }

  createPurchaseOrder(body: {
    supplierId: string;
    expectedDate?: string | null;
    notes?: string | null;
    items: { materialId: string; quantity: number; unitCost: number }[];
  }) {
    return this.http.post<PurchaseOrder>(`${this.apiUrl}/purchasing/orders`, body);
  }

  receivePurchaseOrder(id: string, warehouseId: string) {
    return this.http.post<PurchaseOrder>(`${this.apiUrl}/purchasing/orders/${id}/receive`, { warehouseId });
  }

  salesOrders() {
    return this.http.get<SalesOrder[]>(`${this.apiUrl}/sales/orders`);
  }

  createSalesOrder(body: {
    customerId: string;
    notes?: string | null;
    items: { materialId: string; quantity: number; unitPrice: number }[];
  }) {
    return this.http.post<SalesOrder>(`${this.apiUrl}/sales/orders`, body);
  }

  shipSalesOrder(id: string, warehouseId: string) {
    return this.http.post<SalesOrder>(`${this.apiUrl}/sales/orders/${id}/ship`, { warehouseId });
  }

  registerUser(body: RegisterRequest) {
    return this.http.post(`${this.apiUrl}/auth/register`, body);
  }

  auditLogs(params: { userId?: string; entityName?: string; take?: number } = {}) {
    const searchParams = new URLSearchParams();

    if (params.userId) {
      searchParams.set('userId', params.userId);
    }

    if (params.entityName) {
      searchParams.set('entityName', params.entityName);
    }

    if (params.take) {
      searchParams.set('take', String(params.take));
    }

    const query = searchParams.toString();
    return this.http.get<AuditLog[]>(`${this.apiUrl}/audit-logs${query ? `?${query}` : ''}`);
  }

  financialSummary() {
    return this.http.get<FinancialSummary>(`${this.apiUrl}/finance/summary`);
  }

  payables(status?: FinancialEntryStatus) {
    const query = status ? `?status=${encodeURIComponent(status)}` : '';
    return this.http.get<FinancialEntry[]>(`${this.apiUrl}/finance/payables${query}`);
  }

  receivables(status?: FinancialEntryStatus) {
    const query = status ? `?status=${encodeURIComponent(status)}` : '';
    return this.http.get<FinancialEntry[]>(`${this.apiUrl}/finance/receivables${query}`);
  }

  createFinancialEntry(body: {
    type: FinancialEntryType;
    dueDate: string;
    amount: number;
    description?: string | null;
    supplierId?: string | null;
    customerId?: string | null;
  }) {
    return this.http.post<FinancialEntry>(`${this.apiUrl}/finance/entries`, body);
  }

  settleFinancialEntry(id: string, body: { settledAt?: string | null; paidAmount?: number | null } = {}) {
    return this.http.post<FinancialEntry>(`${this.apiUrl}/finance/entries/${id}/settle`, body);
  }

  cancelFinancialEntry(id: string) {
    return this.http.post<FinancialEntry>(`${this.apiUrl}/finance/entries/${id}/cancel`, {});
  }
}
