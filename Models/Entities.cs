namespace RestaurantPos.Models;

public class Organization
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string Currency { get; set; } = "BDT";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Branch
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal ServiceChargePercent { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Terminal
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public Branch Branch { get; set; } = null!;
    public string Name { get; set; } = "";
    public bool IsActive { get; set; } = true;
}

public class Permission
{
    public int Id { get; set; }
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";
    public string Module { get; set; } = "";
}

public class Role
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public string Name { get; set; } = "";
    public List<RolePermission> RolePermissions { get; set; } = new();
}

public class RolePermission
{
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
    public int PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
}

public class AppUser
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public string FullName { get; set; } = "";
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
    public int? DefaultBranchId { get; set; }
    public Branch? DefaultBranch { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<UserBranch> Branches { get; set; } = new();
}

public class UserBranch
{
    public int UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public int BranchId { get; set; }
    public Branch Branch { get; set; } = null!;
}

public class Floor
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public Branch Branch { get; set; } = null!;
    public string Name { get; set; } = "";
    public int SortOrder { get; set; }
}

public class DiningTable
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public Branch Branch { get; set; } = null!;
    public int FloorId { get; set; }
    public Floor Floor { get; set; } = null!;
    public string Name { get; set; } = "";
    public int Seats { get; set; }
    public string Status { get; set; } = "Free";
}

public class Category
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public string Name { get; set; } = "";
    public int SortOrder { get; set; }
    public string Color { get; set; } = "#d4652f";
    public bool IsActive { get; set; } = true;
}

public class Product
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public string Name { get; set; } = "";
    public string? Sku { get; set; }
    public string? Barcode { get; set; }
    public decimal Price { get; set; }
    public decimal CostPrice { get; set; }
    public int PrepMinutes { get; set; } = 15;
    public string Color { get; set; } = "#efe6d9";
    public bool IsActive { get; set; } = true;
    public List<ProductModifierGroup> ModifierGroups { get; set; } = new();
    public List<ProductAddon> Addons { get; set; } = new();
}

public class ModifierGroup
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public string Name { get; set; } = "";
    public bool Required { get; set; }
    public int MinSelect { get; set; }
    public int MaxSelect { get; set; } = 1;
    public List<ModifierOption> Options { get; set; } = new();
}

public class ModifierOption
{
    public int Id { get; set; }
    public int ModifierGroupId { get; set; }
    public ModifierGroup ModifierGroup { get; set; } = null!;
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ProductModifierGroup
{
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int ModifierGroupId { get; set; }
    public ModifierGroup ModifierGroup { get; set; } = null!;
}

public class Addon
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ProductAddon
{
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int AddonId { get; set; }
    public Addon Addon { get; set; } = null!;
}

public class Coupon
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public string Code { get; set; } = "";
    public string DiscountType { get; set; } = "Percent";
    public decimal Value { get; set; }
    public decimal MinOrder { get; set; }
    public decimal MaxDiscount { get; set; }
    public DateTime? ExpiresOn { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Customer
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public string Name { get; set; } = "";
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public decimal DueBalance { get; set; }
    public bool IsActive { get; set; } = true;
}

public class DuePayment
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Method { get; set; } = "Cash";
    public string? Note { get; set; }
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Shift
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public Branch Branch { get; set; } = null!;
    public int TerminalId { get; set; }
    public Terminal Terminal { get; set; } = null!;
    public int UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }
    public decimal OpeningCash { get; set; }
    public decimal ClosingCash { get; set; }
    public decimal ExpectedCash { get; set; }
    public string Status { get; set; } = "Open";
    public string? Note { get; set; }
}

public class SaleOrder
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public int BranchId { get; set; }
    public Branch Branch { get; set; } = null!;
    public int? TerminalId { get; set; }
    public int? ShiftId { get; set; }
    public string OrderNo { get; set; } = "";
    public string OrderType { get; set; } = "DineIn";
    public string Status { get; set; } = "Open";
    public string KitchenStatus { get; set; } = "None";
    public string PaymentStatus { get; set; } = "Unpaid";
    public int? TableId { get; set; }
    public DiningTable? Table { get; set; }
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int? WaiterId { get; set; }
    public AppUser? Waiter { get; set; }
    public int? CashierId { get; set; }
    public AppUser? Cashier { get; set; }
    public int? RiderId { get; set; }
    public AppUser? Rider { get; set; }
    public string? CouponCode { get; set; }
    public string? Notes { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? DeliveryPhone { get; set; }
    public string DeliveryStatus { get; set; } = "None";
    public decimal Subtotal { get; set; }
    public decimal ManualDiscount { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal ServiceCharge { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal PaidTotal { get; set; }
    public decimal RefundedTotal { get; set; }
    public decimal FoodCost { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }
    public List<SaleLine> Lines { get; set; } = new();
    public List<Tender> Tenders { get; set; } = new();
    public List<Receipt> Receipts { get; set; } = new();
}

public class SaleLine
{
    public int Id { get; set; }
    public int SaleOrderId { get; set; }
    public SaleOrder SaleOrder { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string Name { get; set; } = "";
    public decimal Qty { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FoodCost { get; set; }
    public string Course { get; set; } = "Main";
    public string? Notes { get; set; }
    public string PrepStatus { get; set; } = "Pending";
    public bool IsVoid { get; set; }
    public bool SentToKitchen { get; set; }
    public bool StockConsumed { get; set; }
    public decimal LineTotal { get; set; }
    public List<SaleLineExtra> Extras { get; set; } = new();
}

public class SaleLineExtra
{
    public int Id { get; set; }
    public int SaleLineId { get; set; }
    public SaleLine SaleLine { get; set; } = null!;
    public string Kind { get; set; } = "Modifier";
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
}

public class Tender
{
    public int Id { get; set; }
    public int SaleOrderId { get; set; }
    public SaleOrder SaleOrder { get; set; } = null!;
    public string Method { get; set; } = "Cash";
    public decimal Amount { get; set; }
    public decimal Tendered { get; set; }
    public decimal ChangeDue { get; set; }
    public string? Reference { get; set; }
    public bool IsRefund { get; set; }
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Receipt
{
    public int Id { get; set; }
    public int SaleOrderId { get; set; }
    public SaleOrder SaleOrder { get; set; } = null!;
    public string ReceiptNo { get; set; } = "";
    public string Token { get; set; } = "";
    public int PrintCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<ReceiptEvent> Events { get; set; } = new();
    public List<ReceiptDispatch> Dispatches { get; set; } = new();
}

public class ReceiptEvent
{
    public int Id { get; set; }
    public int ReceiptId { get; set; }
    public Receipt Receipt { get; set; } = null!;
    public string Action { get; set; } = "";
    public string? Note { get; set; }
    public int? UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class ReceiptDispatch
{
    public int Id { get; set; }
    public int ReceiptId { get; set; }
    public Receipt Receipt { get; set; } = null!;
    public string Channel { get; set; } = "";
    public string Destination { get; set; } = "";
    public string Status { get; set; } = "Queued";
    public string? Note { get; set; }
    public int? UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class KitchenTicket
{
    public int Id { get; set; }
    public int SaleOrderId { get; set; }
    public SaleOrder SaleOrder { get; set; } = null!;
    public string TicketNo { get; set; } = "";
    public string Course { get; set; } = "Main";
    public string Status { get; set; } = "Queued";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<KitchenTicketLine> Lines { get; set; } = new();
}

public class KitchenTicketLine
{
    public int Id { get; set; }
    public int KitchenTicketId { get; set; }
    public KitchenTicket KitchenTicket { get; set; } = null!;
    public int SourceLineId { get; set; }
    public string Name { get; set; } = "";
    public decimal Qty { get; set; }
    public string? Notes { get; set; }
    public string PrepStatus { get; set; } = "Pending";
}

public class NumberSequence
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public string Key { get; set; } = "";
    public int LastValue { get; set; }
}

public class Ingredient
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public string Name { get; set; } = "";
    public string? Sku { get; set; }
    public string Unit { get; set; } = "pcs";
    public decimal ReorderLevel { get; set; }
    public decimal CostPerUnit { get; set; }
    public bool ExpiryTracked { get; set; }
    public bool IsActive { get; set; } = true;
}

public class RecipeLine
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int IngredientId { get; set; }
    public Ingredient Ingredient { get; set; } = null!;
    public decimal Quantity { get; set; }
}

public class StockBalance
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public Branch Branch { get; set; } = null!;
    public int IngredientId { get; set; }
    public Ingredient Ingredient { get; set; } = null!;
    public decimal Quantity { get; set; }
    public decimal AvgCost { get; set; }
    public DateTime? ExpiryDate { get; set; }
}

public class StockLedger
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public int IngredientId { get; set; }
    public Ingredient? Ingredient { get; set; }
    public string TxnType { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal BalanceAfter { get; set; }
    public string? RefType { get; set; }
    public int? RefId { get; set; }
    public string? Note { get; set; }
    public int? UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Supplier
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public string Name { get; set; } = "";
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public decimal DueBalance { get; set; }
    public bool IsActive { get; set; } = true;
}

public class SupplierPayment
{
    public int Id { get; set; }
    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Method { get; set; } = "Cash";
    public string? Note { get; set; }
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class PurchaseOrder
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public int BranchId { get; set; }
    public Branch Branch { get; set; } = null!;
    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;
    public string PoNo { get; set; } = "";
    public string Status { get; set; } = "Open";
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public DateTime? ExpectedDate { get; set; }
    public decimal Total { get; set; }
    public string? Notes { get; set; }
    public int UserId { get; set; }
    public List<PurchaseOrderLine> Lines { get; set; } = new();
}

public class PurchaseOrderLine
{
    public int Id { get; set; }
    public int PurchaseOrderId { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; } = null!;
    public int IngredientId { get; set; }
    public Ingredient Ingredient { get; set; } = null!;
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal ReceivedQty { get; set; }
}

public class GoodsReceipt
{
    public int Id { get; set; }
    public int PurchaseOrderId { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; } = null!;
    public string GrnNo { get; set; } = "";
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public int UserId { get; set; }
    public string? Note { get; set; }
    public decimal Total { get; set; }
    public List<GoodsReceiptLine> Lines { get; set; } = new();
}

public class GoodsReceiptLine
{
    public int Id { get; set; }
    public int GoodsReceiptId { get; set; }
    public GoodsReceipt GoodsReceipt { get; set; } = null!;
    public int PurchaseOrderLineId { get; set; }
    public int IngredientId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public DateTime? ExpiryDate { get; set; }
}

public class StockTransfer
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public int FromBranchId { get; set; }
    public Branch FromBranch { get; set; } = null!;
    public int ToBranchId { get; set; }
    public Branch ToBranch { get; set; } = null!;
    public string Status { get; set; } = "Posted";
    public string? Note { get; set; }
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<StockTransferLine> Lines { get; set; } = new();
}

public class StockTransferLine
{
    public int Id { get; set; }
    public int StockTransferId { get; set; }
    public StockTransfer StockTransfer { get; set; } = null!;
    public int IngredientId { get; set; }
    public decimal Quantity { get; set; }
}

public class Wastage
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public int IngredientId { get; set; }
    public Ingredient Ingredient { get; set; } = null!;
    public decimal Quantity { get; set; }
    public decimal Cost { get; set; }
    public string Reason { get; set; } = "";
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class StockAdjustment
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public int IngredientId { get; set; }
    public Ingredient Ingredient { get; set; } = null!;
    public decimal QtyDelta { get; set; }
    public string Reason { get; set; } = "";
    public string Status { get; set; } = "Posted";
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class PhysicalCount
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public string Status { get; set; } = "Posted";
    public int UserId { get; set; }
    public DateTime CountedAt { get; set; } = DateTime.UtcNow;
    public string? Note { get; set; }
    public List<PhysicalCountLine> Lines { get; set; } = new();
}

public class PhysicalCountLine
{
    public int Id { get; set; }
    public int PhysicalCountId { get; set; }
    public PhysicalCount PhysicalCount { get; set; } = null!;
    public int IngredientId { get; set; }
    public decimal SystemQty { get; set; }
    public decimal CountedQty { get; set; }
}

public class Expense
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public int BranchId { get; set; }
    public Branch Branch { get; set; } = null!;
    public string Category { get; set; } = "";
    public decimal Amount { get; set; }
    public string? Note { get; set; }
    public int UserId { get; set; }
    public DateTime SpentAt { get; set; } = DateTime.UtcNow;
}

public class ApprovalRequest
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public int? BranchId { get; set; }
    public string Type { get; set; } = "";
    public string? EntityKey { get; set; }
    public string PayloadJson { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public string? Reason { get; set; }
    public decimal Amount { get; set; }
    public int RequestedById { get; set; }
    public AppUser RequestedBy { get; set; } = null!;
    public int? ReviewedById { get; set; }
    public string? ReviewNote { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
}

public class AuditLog
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public int? UserId { get; set; }
    public string Action { get; set; } = "";
    public string Entity { get; set; } = "";
    public string? EntityId { get; set; }
    public string? Detail { get; set; }
    public string? Ip { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class LoginHistory
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string Username { get; set; } = "";
    public bool Success { get; set; }
    public string? Ip { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class SystemSetting
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
}
