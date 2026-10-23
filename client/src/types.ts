export type Branch = {
  id: number
  name: string
  code: string
  address?: string | null
  phone?: string | null
  taxPercent: number
  serviceChargePercent: number
  isActive: boolean
}

export type User = {
  id: number
  fullName: string
  username: string
  email?: string | null
  phone?: string | null
  role: string
  roleId: number
  organizationId: number
  organizationName: string
  branchId?: number | null
  isActive: boolean
  branchIds: number[]
  branches: Branch[]
  permissions: string[]
}

export type Terminal = { id: number; branchId: number; name: string; isActive: boolean }
export type Shift = {
  id: number
  branchId: number
  terminalId: number
  terminalName: string
  userId: number
  userName: string
  openedAt: string
  closedAt?: string | null
  openingCash: number
  closingCash: number
  expectedCash: number
  variance: number
  status: string
}
export type Category = { id: number; name: string; sortOrder: number; color: string; isActive: boolean }
export type ModOption = { id: number; name: string; price: number }
export type ModGroup = { id: number; name: string; required: boolean; minSelect: number; maxSelect: number; options: ModOption[] }
export type Addon = { id: number; name: string; price: number; isActive: boolean }
export type Product = {
  id: number
  categoryId: number
  name: string
  sku?: string | null
  barcode?: string | null
  price: number
  costPrice: number
  prepMinutes: number
  color: string
  isActive: boolean
  modifierGroupIds: number[]
  addonIds: number[]
}
export type Coupon = { id: number; code: string; discountType: string; value: number; minOrder: number; maxDiscount: number; expiresOn?: string | null; isActive: boolean }
export type Floor = { id: number; branchId: number; name: string; sortOrder: number }
export type DiningTable = { id: number; branchId: number; floorId: number; name: string; seats: number; status: string; openOrderId?: number | null }
export type Customer = { id: number; name: string; phone?: string | null; email?: string | null; address?: string | null; dueBalance: number; isActive: boolean }
export type Staff = { id: number; fullName: string; role: string }
export type Catalog = {
  branch: Branch
  categories: Category[]
  products: Product[]
  modifierGroups: ModGroup[]
  addons: Addon[]
  coupons: Coupon[]
  floors: Floor[]
  tables: DiningTable[]
  staff: Staff[]
  customers: Customer[]
  terminals: Terminal[]
  shift?: Shift | null
}
export type Extra = { kind: string; name: string; price: number }
export type Line = {
  id: number
  productId: number
  name: string
  qty: number
  unitPrice: number
  discountAmount: number
  foodCost: number
  course: string
  notes?: string | null
  prepStatus: string
  isVoid: boolean
  sentToKitchen: boolean
  lineTotal: number
  extras: Extra[]
}
export type Tender = { id: number; method: string; amount: number; tendered: number; changeDue: number; reference?: string | null; isRefund: boolean; createdAt: string }
export type ReceiptEvent = { action: string; note?: string | null; createdAt: string }
export type Dispatch = { channel: string; destination: string; status: string; note?: string | null; createdAt: string }
export type Receipt = { id: number; receiptNo: string; token: string; printCount: number; createdAt: string; events: ReceiptEvent[]; dispatches: Dispatch[] }
export type Order = {
  id: number
  orderNo: string
  branchId: number
  branchName: string
  orderType: string
  status: string
  kitchenStatus: string
  paymentStatus: string
  tableName?: string | null
  customerName?: string | null
  waiterName?: string | null
  cashierName?: string | null
  deliveryStatus?: string | null
  grandTotal: number
  paidTotal: number
  refundedTotal: number
  balance: number
  createdAt: string
  terminalId?: number | null
  tableId?: number | null
  customerId?: number | null
  waiterId?: number | null
  cashierId?: number | null
  riderId?: number | null
  riderName?: string | null
  couponCode?: string | null
  notes?: string | null
  deliveryAddress?: string | null
  deliveryPhone?: string | null
  subtotal: number
  manualDiscount: number
  discountTotal: number
  taxTotal: number
  serviceCharge: number
  foodCost: number
  lines: Line[]
  tenders: Tender[]
  receipt?: Receipt | null
}
export type OrderSummary = {
  id: number
  orderNo: string
  branchId: number
  branchName: string
  orderType: string
  status: string
  kitchenStatus: string
  paymentStatus: string
  tableName?: string | null
  customerName?: string | null
  waiterName?: string | null
  cashierName?: string | null
  deliveryStatus?: string | null
  grandTotal: number
  paidTotal: number
  refundedTotal: number
  balance: number
  createdAt: string
}
export type KitchenCard = {
  ticketId: number
  ticketNo: string
  course: string
  status: string
  orderNo: string
  orderType: string
  tableName?: string | null
  createdAt: string
  orderId: number
  lines: { saleLineId: number; name: string; qty: number; notes?: string | null; prepStatus: string }[]
}
export type Named = { name: string; amount: number; qty: number; count: number }
export type Overview = {
  from: string
  to: string
  grossSales: number
  discount: number
  tax: number
  serviceCharge: number
  netSales: number
  refunds: number
  foodCost: number
  grossProfit: number
  expenses: number
  wastageCost: number
  customerDues: number
  supplierDues: number
  orderCount: number
  openOrders: number
  lowStockCount: number
  pendingApprovals: number
  byPayment: Named[]
  byCashier: Named[]
  bestSellers: Named[]
  slowMovers: Named[]
  byBranch: Named[]
  daily: Named[]
}
export type Ingredient = { id: number; name: string; sku?: string | null; unit: string; reorderLevel: number; costPerUnit: number; expiryTracked: boolean; isActive: boolean }
export type StockRow = { ingredientId: number; name: string; unit: string; quantity: number; avgCost: number; value: number; reorderLevel: number; low: boolean; expiryDate?: string | null; expiring: boolean }
export type LedgerRow = { id: number; ingredientName: string; txnType: string; quantity: number; unitCost: number; balanceAfter: number; note?: string | null; createdAt: string }
export type Supplier = { id: number; name: string; phone?: string | null; email?: string | null; address?: string | null; dueBalance: number; isActive: boolean }
export type PoLine = { id: number; ingredientId: number; ingredientName: string; unit: string; quantity: number; unitCost: number; receivedQty: number }
export type PurchaseOrder = { id: number; poNo: string; branchId: number; branchName: string; supplierId: number; supplierName: string; status: string; orderDate: string; expectedDate?: string | null; total: number; notes?: string | null; lines: PoLine[] }
export type Expense = { id: number; branchId: number; branchName: string; category: string; amount: number; note?: string | null; spentAt: string }
export type Approval = { id: number; type: string; status: string; reason?: string | null; amount: number; requestedBy: string; createdAt: string; reviewNote?: string | null }
export type Role = { id: number; name: string; permissions: string[] }
export type Audit = { id: number; userName?: string | null; action: string; entity: string; entityId?: string | null; detail?: string | null; ip?: string | null; createdAt: string }
export type LoginRow = { id: number; username: string; success: boolean; ip?: string | null; createdAt: string }
export type Setting = { key: string; value: string }
export type Monitor = { databaseOnline: boolean; serverTimeUtc: string; today: string; users: number; openOrders: number; pendingApprovals: number; lowStock: number; backups: string[] }
export type ActionResult = { approvalRequired: boolean; message: string; order?: Order | null }
export type PayResult = { order: Order; changeDue: number; message: string }
export type Verify = { valid: boolean; message: string; receiptNo?: string; orderNo?: string; branch?: string; paymentStatus?: string; grandTotal: number; createdAt?: string; printCount: number; lines: { name: string; qty: number; lineTotal: number }[] }
