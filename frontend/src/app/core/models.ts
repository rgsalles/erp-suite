export type UserRole = 'Admin' | 'Manager' | 'Buyer' | 'Seller' | 'Stock' | 'Operator';
export type OrderStatus = 'Draft' | 'Confirmed' | 'PartiallyReceived' | 'Received' | 'PartiallyShipped' | 'Shipped' | 'Cancelled';
export type StockMovementType = 'Inbound' | 'Outbound' | 'Adjustment' | 'PurchaseReceipt' | 'SalesShipment';
export type FinancialEntryType = 'Payable' | 'Receivable';
export type FinancialEntryStatus = 'Open' | 'Paid' | 'Cancelled';

export interface UserSummary {
  id: string;
  fullName: string;
  email: string;
  role: UserRole;
  isActive: boolean;
}

export interface AuthResponse {
  token: string;
  expiresAt: string;
  user: UserSummary;
}

export interface RegisterRequest {
  fullName: string;
  email: string;
  password: string;
  role?: UserRole | null;
}

export interface CatalogItem {
  id: string;
  name: string;
  description?: string | null;
}

export interface Company {
  id: string;
  code: string;
  name: string;
  taxId?: string | null;
  isActive: boolean;
}

export interface Branch {
  id: string;
  companyId: string;
  companyCode: string;
  companyName: string;
  code: string;
  name: string;
  taxId?: string | null;
  address?: string | null;
  isActive: boolean;
}

export interface CostCenter {
  id: string;
  companyId: string;
  companyCode: string;
  companyName: string;
  code: string;
  name: string;
  description?: string | null;
  isActive: boolean;
}

export interface UnitOfMeasure {
  id: string;
  code: string;
  name: string;
}

export interface CurrencyUnit {
  id: string;
  code: string;
  name: string;
  symbol: string;
  isDefault: boolean;
}

export interface ExchangeRate {
  id: string;
  fromCurrencyId: string;
  fromCurrencyCode: string;
  toCurrencyId: string;
  toCurrencyCode: string;
  rateDate: string;
  rate: number;
  source?: string | null;
}

export interface BusinessPartner {
  id: string;
  name: string;
  taxId: string;
  email?: string | null;
  phone?: string | null;
  contactName?: string | null;
  isActive: boolean;
}

export interface Warehouse {
  id: string;
  code: string;
  name: string;
  location?: string | null;
  branchId?: string | null;
  branchName?: string | null;
  isActive: boolean;
}

export interface Material {
  id: string;
  code: string;
  description: string;
  categoryId: string;
  categoryName: string;
  unitOfMeasureId: string;
  unitCode: string;
  supplierId?: string | null;
  supplierName?: string | null;
  standardCost: number;
  salePrice: number;
  minimumStock: number;
  currentStock: number;
  isActive: boolean;
}

export interface StockBalance {
  materialId: string;
  materialCode: string;
  materialDescription: string;
  unitCode: string;
  minimumStock: number;
  currentStock: number;
  inventoryValue: number;
  belowMinimum: boolean;
}

export interface StockMovement {
  id: string;
  materialId: string;
  materialCode: string;
  materialDescription: string;
  warehouseId: string;
  warehouseCode: string;
  type: StockMovementType;
  quantity: number;
  signedQuantity: number;
  unitCost?: number | null;
  reference?: string | null;
  notes?: string | null;
  movementDate: string;
}

export interface Dashboard {
  activeMaterials: number;
  activeCustomers: number;
  activeSuppliers: number;
  openPurchaseOrders: number;
  openSalesOrders: number;
  lowStockMaterials: number;
  inventoryValue: number;
  openPayables: number;
  openReceivables: number;
  overdueFinancialEntries: number;
  lowStockItems: StockBalance[];
}

export interface PurchaseOrder {
  id: string;
  number: string;
  supplierId: string;
  supplierName: string;
  status: OrderStatus;
  orderDate: string;
  expectedDate?: string | null;
  receivedAt?: string | null;
  notes?: string | null;
  total: number;
  items: PurchaseOrderItem[];
}

export interface PurchaseOrderItem {
  id: string;
  materialId: string;
  materialCode: string;
  materialDescription: string;
  quantity: number;
  unitCost: number;
  receivedQuantity: number;
  lineTotal: number;
}

export interface SalesOrder {
  id: string;
  number: string;
  customerId: string;
  customerName: string;
  status: OrderStatus;
  orderDate: string;
  shippedAt?: string | null;
  notes?: string | null;
  total: number;
  items: SalesOrderItem[];
}

export interface SalesOrderItem {
  id: string;
  materialId: string;
  materialCode: string;
  materialDescription: string;
  quantity: number;
  unitPrice: number;
  shippedQuantity: number;
  lineTotal: number;
}

export interface AuditLog {
  id: string;
  occurredAt: string;
  userId?: string | null;
  userName?: string | null;
  userEmail?: string | null;
  action: string;
  httpMethod: string;
  path: string;
  controller?: string | null;
  entityName?: string | null;
  entityId?: string | null;
  statusCode: number;
  ipAddress?: string | null;
  userAgent?: string | null;
  details?: string | null;
}

export interface FinancialEntry {
  id: string;
  number: string;
  type: FinancialEntryType;
  status: FinancialEntryStatus;
  issueDate: string;
  dueDate: string;
  settledAt?: string | null;
  amount: number;
  paidAmount: number;
  openAmount: number;
  isOverdue: boolean;
  description?: string | null;
  supplierId?: string | null;
  supplierName?: string | null;
  customerId?: string | null;
  customerName?: string | null;
  purchaseOrderId?: string | null;
  purchaseOrderNumber?: string | null;
  salesOrderId?: string | null;
  salesOrderNumber?: string | null;
}

export interface FinancialSummary {
  openPayables: number;
  overduePayables: number;
  openReceivables: number;
  overdueReceivables: number;
  paidThisMonth: number;
  receivedThisMonth: number;
  netCashFlowThisMonth: number;
  nextPayables: FinancialEntry[];
  nextReceivables: FinancialEntry[];
}
