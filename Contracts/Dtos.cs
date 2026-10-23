namespace RestaurantPos.Contracts;

public class LoginRequest
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}

public class BranchDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal ServiceChargePercent { get; set; }
    public bool IsActive { get; set; }
}

public class UserDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Username { get; set; } = "";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string Role { get; set; } = "";
    public int RoleId { get; set; }
    public int OrganizationId { get; set; }
    public string OrganizationName { get; set; } = "";
    public int? BranchId { get; set; }
    public bool IsActive { get; set; }
    public List<int> BranchIds { get; set; } = new();
    public List<BranchDto> Branches { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
}

public class LoginResponse
{
    public string Token { get; set; } = "";
    public UserDto User { get; set; } = new();
}

public class StaffDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Role { get; set; } = "";
}

public class TerminalDto
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public string Name { get; set; } = "";
    public bool IsActive { get; set; }
}

public class ShiftDto
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public int TerminalId { get; set; }
    public string TerminalName { get; set; } = "";
    public int UserId { get; set; }
    public string UserName { get; set; } = "";
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public decimal OpeningCash { get; set; }
    public decimal ClosingCash { get; set; }
    public decimal ExpectedCash { get; set; }
    public decimal Variance { get; set; }
    public string Status { get; set; } = "";
}

public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int SortOrder { get; set; }
    public string Color { get; set; } = "";
    public bool IsActive { get; set; }
}

public class ModOptionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
}

public class ModGroupDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public bool Required { get; set; }
    public int MinSelect { get; set; }
    public int MaxSelect { get; set; }
    public List<ModOptionDto> Options { get; set; } = new();
}

public class AddonDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
}

public class ProductDto
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string Name { get; set; } = "";
    public string? Sku { get; set; }
    public string? Barcode { get; set; }
    public decimal Price { get; set; }
    public decimal CostPrice { get; set; }
    public int PrepMinutes { get; set; }
    public string Color { get; set; } = "";
    public bool IsActive { get; set; }
    public List<int> ModifierGroupIds { get; set; } = new();
    public List<int> AddonIds { get; set; } = new();
}

public class CouponDto
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string DiscountType { get; set; } = "";
    public decimal Value { get; set; }
    public decimal MinOrder { get; set; }
    public decimal MaxDiscount { get; set; }
    public DateTime? ExpiresOn { get; set; }
    public bool IsActive { get; set; }
}

public class FloorDto
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public string Name { get; set; } = "";
    public int SortOrder { get; set; }
}

public class TableDto
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public int FloorId { get; set; }
    public string Name { get; set; } = "";
    public int Seats { get; set; }
    public string Status { get; set; } = "";
    public int? OpenOrderId { get; set; }
}

public class CustomerDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public decimal DueBalance { get; set; }
    public bool IsActive { get; set; }
}

public class CatalogDto
{
    public BranchDto Branch { get; set; } = new();
    public List<CategoryDto> Categories { get; set; } = new();
    public List<ProductDto> Products { get; set; } = new();
    public List<ModGroupDto> ModifierGroups { get; set; } = new();
    public List<AddonDto> Addons { get; set; } = new();
    public List<CouponDto> Coupons { get; set; } = new();
    public List<FloorDto> Floors { get; set; } = new();
    public List<TableDto> Tables { get; set; } = new();
    public List<StaffDto> Staff { get; set; } = new();
    public List<CustomerDto> Customers { get; set; } = new();
    public List<TerminalDto> Terminals { get; set; } = new();
    public ShiftDto? Shift { get; set; }
}

public class ExtraDto
{
    public string Kind { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
}

public class LineDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string Name { get; set; } = "";
    public decimal Qty { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FoodCost { get; set; }
    public string Course { get; set; } = "";
    public string? Notes { get; set; }
    public string PrepStatus { get; set; } = "";
    public bool IsVoid { get; set; }
    public bool SentToKitchen { get; set; }
    public decimal LineTotal { get; set; }
    public List<ExtraDto> Extras { get; set; } = new();
}

public class TenderDto
{
    public int Id { get; set; }
    public string Method { get; set; } = "";
    public decimal Amount { get; set; }
    public decimal Tendered { get; set; }
    public decimal ChangeDue { get; set; }
    public string? Reference { get; set; }
    public bool IsRefund { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ReceiptEventDto
{
    public string Action { get; set; } = "";
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class DispatchDto
{
    public string Channel { get; set; } = "";
    public string Destination { get; set; } = "";
    public string Status { get; set; } = "";
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ReceiptDto
{
    public int Id { get; set; }
    public string ReceiptNo { get; set; } = "";
    public string Token { get; set; } = "";
    public int PrintCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ReceiptEventDto> Events { get; set; } = new();
    public List<DispatchDto> Dispatches { get; set; } = new();
}

public class OrderSummaryDto
{
    public int Id { get; set; }
    public string OrderNo { get; set; } = "";
    public int BranchId { get; set; }
    public string BranchName { get; set; } = "";
    public string OrderType { get; set; } = "";
    public string Status { get; set; } = "";
    public string KitchenStatus { get; set; } = "";
    public string PaymentStatus { get; set; } = "";
    public string? TableName { get; set; }
    public string? CustomerName { get; set; }
    public string? WaiterName { get; set; }
    public string? CashierName { get; set; }
    public string? DeliveryStatus { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal PaidTotal { get; set; }
    public decimal RefundedTotal { get; set; }
    public decimal Balance { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class OrderDto : OrderSummaryDto
{
    public int? TerminalId { get; set; }
    public int? TableId { get; set; }
    public int? CustomerId { get; set; }
    public int? WaiterId { get; set; }
    public int? CashierId { get; set; }
    public int? RiderId { get; set; }
    public string? RiderName { get; set; }
    public string? CouponCode { get; set; }
    public string? Notes { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? DeliveryPhone { get; set; }
    public decimal Subtotal { get; set; }
    public decimal ManualDiscount { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal ServiceCharge { get; set; }
    public decimal FoodCost { get; set; }
    public List<LineDto> Lines { get; set; } = new();
    public List<TenderDto> Tenders { get; set; } = new();
    public ReceiptDto? Receipt { get; set; }
}

public class CreateOrderRequest
{
    public int BranchId { get; set; }
    public int? TerminalId { get; set; }
    public string OrderType { get; set; } = "DineIn";
    public int? TableId { get; set; }
    public int? CustomerId { get; set; }
    public int? WaiterId { get; set; }
    public string? Notes { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? DeliveryPhone { get; set; }
    public bool ReuseTable { get; set; } = true;
}

public class UpdateOrderRequest
{
    public string? OrderType { get; set; }
    public int? TableId { get; set; }
    public bool ClearTable { get; set; }
    public int? WaiterId { get; set; }
    public int? CustomerId { get; set; }
    public bool ClearCustomer { get; set; }
    public string? Notes { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? DeliveryPhone { get; set; }
    public int? RiderId { get; set; }
    public string? DeliveryStatus { get; set; }
}

public class LineRequest
{
    public int ProductId { get; set; }
    public decimal Qty { get; set; } = 1;
    public decimal DiscountAmount { get; set; }
    public string Course { get; set; } = "Main";
    public string? Notes { get; set; }
    public List<int> ModifierOptionIds { get; set; } = new();
    public List<int> AddonIds { get; set; } = new();
}

public class CouponRequest
{
    public string? Code { get; set; }
}

public class DiscountRequest
{
    public decimal Amount { get; set; }
}

public class PayRequest
{
    public string Method { get; set; } = "Cash";
    public decimal Amount { get; set; }
    public decimal Tendered { get; set; }
    public string? Reference { get; set; }
    public int? CustomerId { get; set; }
}

public class PayResultDto
{
    public OrderDto Order { get; set; } = new();
    public decimal ChangeDue { get; set; }
    public string Message { get; set; } = "";
}

public class RefundRequest
{
    public decimal Amount { get; set; }
    public string Reason { get; set; } = "";
    public string Method { get; set; } = "Cash";
    public bool Restock { get; set; } = true;
}

public class ReasonRequest
{
    public string Reason { get; set; } = "";
}

public class ActionResultDto
{
    public bool ApprovalRequired { get; set; }
    public string Message { get; set; } = "";
    public OrderDto? Order { get; set; }
}

public class PrepRequest
{
    public string Status { get; set; } = "";
}

public class DispatchRequest
{
    public string Channel { get; set; } = "Email";
    public string Destination { get; set; } = "";
}

public class OpenShiftRequest
{
    public int BranchId { get; set; }
    public int TerminalId { get; set; }
    public decimal OpeningCash { get; set; }
}

public class CloseShiftRequest
{
    public decimal ClosingCash { get; set; }
    public string? Note { get; set; }
}

public class KitchenLineDto
{
    public int SaleLineId { get; set; }
    public string Name { get; set; } = "";
    public decimal Qty { get; set; }
    public string? Notes { get; set; }
    public string PrepStatus { get; set; } = "";
    public string? Extras { get; set; }
}

public class KitchenCardDto
{
    public int TicketId { get; set; }
    public string TicketNo { get; set; } = "";
    public string Course { get; set; } = "";
    public string Status { get; set; } = "";
    public string OrderNo { get; set; } = "";
    public string OrderType { get; set; } = "";
    public string? TableName { get; set; }
    public DateTime CreatedAt { get; set; }
    public int OrderId { get; set; }
    public List<KitchenLineDto> Lines { get; set; } = new();
}

public class VerifyDto
{
    public bool Valid { get; set; }
    public string Message { get; set; } = "";
    public string? ReceiptNo { get; set; }
    public string? OrderNo { get; set; }
    public string? Branch { get; set; }
    public string? PaymentStatus { get; set; }
    public decimal GrandTotal { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int PrintCount { get; set; }
    public List<LineDto> Lines { get; set; } = new();
}

public class SaveCategoryRequest
{
    public int? Id { get; set; }
    public string Name { get; set; } = "";
    public int SortOrder { get; set; }
    public string Color { get; set; } = "#d4652f";
    public bool IsActive { get; set; } = true;
}

public class SaveProductRequest
{
    public int? Id { get; set; }
    public int CategoryId { get; set; }
    public string Name { get; set; } = "";
    public string? Sku { get; set; }
    public string? Barcode { get; set; }
    public decimal Price { get; set; }
    public decimal CostPrice { get; set; }
    public int PrepMinutes { get; set; } = 15;
    public string Color { get; set; } = "#efe6d9";
    public bool IsActive { get; set; } = true;
    public List<int> ModifierGroupIds { get; set; } = new();
    public List<int> AddonIds { get; set; } = new();
}

public class SaveAddonRequest
{
    public int? Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
}

public class SaveCouponRequest
{
    public int? Id { get; set; }
    public string Code { get; set; } = "";
    public string DiscountType { get; set; } = "Percent";
    public decimal Value { get; set; }
    public decimal MinOrder { get; set; }
    public decimal MaxDiscount { get; set; }
    public DateTime? ExpiresOn { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ModOptionRequest
{
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
}

public class SaveModifierGroupRequest
{
    public int? Id { get; set; }
    public string Name { get; set; } = "";
    public bool Required { get; set; }
    public int MinSelect { get; set; }
    public int MaxSelect { get; set; } = 1;
    public List<ModOptionRequest> Options { get; set; } = new();
}

public class SaveFloorRequest
{
    public int? Id { get; set; }
    public int BranchId { get; set; }
    public string Name { get; set; } = "";
    public int SortOrder { get; set; }
}

public class SaveTableRequest
{
    public int? Id { get; set; }
    public int BranchId { get; set; }
    public int FloorId { get; set; }
    public string Name { get; set; } = "";
    public int Seats { get; set; } = 4;
    public string Status { get; set; } = "Free";
}

public class SaveCustomerRequest
{
    public int? Id { get; set; }
    public string Name { get; set; } = "";
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
}

public class SettleRequest
{
    public decimal Amount { get; set; }
    public string Method { get; set; } = "Cash";
    public string? Note { get; set; }
}

public class SaveExpenseRequest
{
    public int BranchId { get; set; }
    public string Category { get; set; } = "";
    public decimal Amount { get; set; }
    public string? Note { get; set; }
    public DateTime? SpentAt { get; set; }
}

public class ExpenseDto
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public string BranchName { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal Amount { get; set; }
    public string? Note { get; set; }
    public DateTime SpentAt { get; set; }
}

public class IngredientDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Sku { get; set; }
    public string Unit { get; set; } = "";
    public decimal ReorderLevel { get; set; }
    public decimal CostPerUnit { get; set; }
    public bool ExpiryTracked { get; set; }
    public bool IsActive { get; set; }
}

public class SaveIngredientRequest
{
    public int? Id { get; set; }
    public string Name { get; set; } = "";
    public string? Sku { get; set; }
    public string Unit { get; set; } = "pcs";
    public decimal ReorderLevel { get; set; }
    public decimal CostPerUnit { get; set; }
    public bool ExpiryTracked { get; set; }
    public bool IsActive { get; set; } = true;
}

public class RecipeLineDto
{
    public int IngredientId { get; set; }
    public string IngredientName { get; set; } = "";
    public string Unit { get; set; } = "";
    public decimal Quantity { get; set; }
}

public class SaveRecipeRequest
{
    public List<RecipeLineDto> Lines { get; set; } = new();
}

public class StockRowDto
{
    public int IngredientId { get; set; }
    public string Name { get; set; } = "";
    public string Unit { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal AvgCost { get; set; }
    public decimal Value { get; set; }
    public decimal ReorderLevel { get; set; }
    public bool Low { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool Expiring { get; set; }
}

public class LedgerDto
{
    public int Id { get; set; }
    public string IngredientName { get; set; } = "";
    public string TxnType { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal BalanceAfter { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class WastageRequest
{
    public int BranchId { get; set; }
    public int IngredientId { get; set; }
    public decimal Quantity { get; set; }
    public string Reason { get; set; } = "";
}

public class AdjustmentRequest
{
    public int BranchId { get; set; }
    public int IngredientId { get; set; }
    public decimal QtyDelta { get; set; }
    public string Reason { get; set; } = "";
}

public class CountLineRequest
{
    public int IngredientId { get; set; }
    public decimal CountedQty { get; set; }
}

public class CountRequest
{
    public int BranchId { get; set; }
    public string? Note { get; set; }
    public List<CountLineRequest> Lines { get; set; } = new();
}

public class TransferLineRequest
{
    public int IngredientId { get; set; }
    public decimal Quantity { get; set; }
}

public class TransferRequest
{
    public int FromBranchId { get; set; }
    public int ToBranchId { get; set; }
    public string? Note { get; set; }
    public List<TransferLineRequest> Lines { get; set; } = new();
}

public class SupplierDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public decimal DueBalance { get; set; }
    public bool IsActive { get; set; }
}

public class SaveSupplierRequest
{
    public int? Id { get; set; }
    public string Name { get; set; } = "";
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
}

public class PoLineDto
{
    public int Id { get; set; }
    public int IngredientId { get; set; }
    public string IngredientName { get; set; } = "";
    public string Unit { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal ReceivedQty { get; set; }
}

public class PoLineRequest
{
    public int IngredientId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
}

public class SavePoRequest
{
    public int BranchId { get; set; }
    public int SupplierId { get; set; }
    public DateTime? ExpectedDate { get; set; }
    public string? Notes { get; set; }
    public List<PoLineRequest> Lines { get; set; } = new();
}

public class PurchaseOrderDto
{
    public int Id { get; set; }
    public string PoNo { get; set; } = "";
    public int BranchId { get; set; }
    public string BranchName { get; set; } = "";
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime OrderDate { get; set; }
    public DateTime? ExpectedDate { get; set; }
    public decimal Total { get; set; }
    public string? Notes { get; set; }
    public List<PoLineDto> Lines { get; set; } = new();
}

public class ReceiveLineRequest
{
    public int PoLineId { get; set; }
    public decimal Quantity { get; set; }
    public DateTime? ExpiryDate { get; set; }
}

public class ReceiveRequest
{
    public string? Note { get; set; }
    public List<ReceiveLineRequest> Lines { get; set; } = new();
}

public class NamedAmount
{
    public string Name { get; set; } = "";
    public decimal Amount { get; set; }
    public decimal Qty { get; set; }
    public int Count { get; set; }
}

public class OverviewDto
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public decimal GrossSales { get; set; }
    public decimal Discount { get; set; }
    public decimal Tax { get; set; }
    public decimal ServiceCharge { get; set; }
    public decimal NetSales { get; set; }
    public decimal Refunds { get; set; }
    public decimal FoodCost { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal Expenses { get; set; }
    public decimal WastageCost { get; set; }
    public decimal CustomerDues { get; set; }
    public decimal SupplierDues { get; set; }
    public int OrderCount { get; set; }
    public int OpenOrders { get; set; }
    public int LowStockCount { get; set; }
    public int PendingApprovals { get; set; }
    public List<NamedAmount> ByPayment { get; set; } = new();
    public List<NamedAmount> ByCashier { get; set; } = new();
    public List<NamedAmount> BestSellers { get; set; } = new();
    public List<NamedAmount> SlowMovers { get; set; } = new();
    public List<NamedAmount> ByBranch { get; set; } = new();
    public List<NamedAmount> Daily { get; set; } = new();
}

public class SaveUserRequest
{
    public int? Id { get; set; }
    public string FullName { get; set; } = "";
    public string Username { get; set; } = "";
    public string? Password { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public int RoleId { get; set; }
    public int? DefaultBranchId { get; set; }
    public bool IsActive { get; set; } = true;
    public List<int> BranchIds { get; set; } = new();
}

public class SaveBranchRequest
{
    public int? Id { get; set; }
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal ServiceChargePercent { get; set; }
    public bool IsActive { get; set; } = true;
}

public class SaveTerminalRequest
{
    public int? Id { get; set; }
    public int BranchId { get; set; }
    public string Name { get; set; } = "";
    public bool IsActive { get; set; } = true;
}

public class RoleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public List<string> Permissions { get; set; } = new();
}

public class AuditDto
{
    public int Id { get; set; }
    public string? UserName { get; set; }
    public string Action { get; set; } = "";
    public string Entity { get; set; } = "";
    public string? EntityId { get; set; }
    public string? Detail { get; set; }
    public string? Ip { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class LoginHistoryDto
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public bool Success { get; set; }
    public string? Ip { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SettingDto
{
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
}

public class ApprovalDto
{
    public int Id { get; set; }
    public string Type { get; set; } = "";
    public string Status { get; set; } = "";
    public string? Reason { get; set; }
    public decimal Amount { get; set; }
    public string RequestedBy { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string? ReviewNote { get; set; }
}

public class ReviewRequest
{
    public string? Note { get; set; }
}

public class BackupDto
{
    public string JsonFile { get; set; } = "";
    public string? SqlFile { get; set; }
    public string Message { get; set; } = "";
}

public class MonitorDto
{
    public bool DatabaseOnline { get; set; }
    public DateTime ServerTimeUtc { get; set; }
    public string Today { get; set; } = "";
    public int Users { get; set; }
    public int OpenOrders { get; set; }
    public int PendingApprovals { get; set; }
    public int LowStock { get; set; }
    public List<string> Backups { get; set; } = new();
}

public static class Map
{
    public static BranchDto Branch(Branch b) => new()
    {
        Id = b.Id,
        Name = b.Name,
        Code = b.Code,
        Address = b.Address,
        Phone = b.Phone,
        TaxPercent = b.TaxPercent,
        ServiceChargePercent = b.ServiceChargePercent,
        IsActive = b.IsActive
    };

    public static ShiftDto Shift(Shift s) => new()
    {
        Id = s.Id,
        BranchId = s.BranchId,
        TerminalId = s.TerminalId,
        TerminalName = s.Terminal?.Name ?? "",
        UserId = s.UserId,
        UserName = s.User?.FullName ?? "",
        OpenedAt = s.OpenedAt,
        ClosedAt = s.ClosedAt,
        OpeningCash = s.OpeningCash,
        ClosingCash = s.ClosingCash,
        ExpectedCash = s.ExpectedCash,
        Variance = s.Status == "Closed" ? Money.R(s.ClosingCash - s.ExpectedCash) : 0,
        Status = s.Status
    };

    public static OrderSummaryDto Summary(SaleOrder o) => new()
    {
        Id = o.Id,
        OrderNo = o.OrderNo,
        BranchId = o.BranchId,
        BranchName = o.Branch?.Name ?? "",
        OrderType = o.OrderType,
        Status = o.Status,
        KitchenStatus = o.KitchenStatus,
        PaymentStatus = o.PaymentStatus,
        TableName = o.Table?.Name,
        CustomerName = o.Customer?.Name,
        WaiterName = o.Waiter?.FullName,
        CashierName = o.Cashier?.FullName,
        DeliveryStatus = o.DeliveryStatus,
        GrandTotal = o.GrandTotal,
        PaidTotal = o.PaidTotal,
        RefundedTotal = o.RefundedTotal,
        Balance = Money.R(o.GrandTotal - o.PaidTotal),
        CreatedAt = o.CreatedAt
    };

    public static OrderDto Order(SaleOrder o)
    {
        var dto = new OrderDto
        {
            Id = o.Id,
            OrderNo = o.OrderNo,
            BranchId = o.BranchId,
            BranchName = o.Branch?.Name ?? "",
            OrderType = o.OrderType,
            Status = o.Status,
            KitchenStatus = o.KitchenStatus,
            PaymentStatus = o.PaymentStatus,
            TableName = o.Table?.Name,
            CustomerName = o.Customer?.Name,
            WaiterName = o.Waiter?.FullName,
            CashierName = o.Cashier?.FullName,
            DeliveryStatus = o.DeliveryStatus,
            GrandTotal = o.GrandTotal,
            PaidTotal = o.PaidTotal,
            RefundedTotal = o.RefundedTotal,
            Balance = Money.R(o.GrandTotal - o.PaidTotal),
            CreatedAt = o.CreatedAt,
            TerminalId = o.TerminalId,
            TableId = o.TableId,
            CustomerId = o.CustomerId,
            WaiterId = o.WaiterId,
            CashierId = o.CashierId,
            RiderId = o.RiderId,
            RiderName = o.Rider?.FullName,
            CouponCode = o.CouponCode,
            Notes = o.Notes,
            DeliveryAddress = o.DeliveryAddress,
            DeliveryPhone = o.DeliveryPhone,
            Subtotal = o.Subtotal,
            ManualDiscount = o.ManualDiscount,
            DiscountTotal = o.DiscountTotal,
            TaxTotal = o.TaxTotal,
            ServiceCharge = o.ServiceCharge,
            FoodCost = o.FoodCost
        };
        dto.Lines = o.Lines.OrderBy(l => l.Id).Select(l => new LineDto
        {
            Id = l.Id,
            ProductId = l.ProductId,
            Name = l.Name,
            Qty = l.Qty,
            UnitPrice = l.UnitPrice,
            DiscountAmount = l.DiscountAmount,
            FoodCost = l.FoodCost,
            Course = l.Course,
            Notes = l.Notes,
            PrepStatus = l.PrepStatus,
            IsVoid = l.IsVoid,
            SentToKitchen = l.SentToKitchen,
            LineTotal = l.LineTotal,
            Extras = l.Extras.Select(e => new ExtraDto { Kind = e.Kind, Name = e.Name, Price = e.Price }).ToList()
        }).ToList();
        dto.Tenders = o.Tenders.OrderBy(t => t.Id).Select(t => new TenderDto
        {
            Id = t.Id,
            Method = t.Method,
            Amount = t.Amount,
            Tendered = t.Tendered,
            ChangeDue = t.ChangeDue,
            Reference = t.Reference,
            IsRefund = t.IsRefund,
            CreatedAt = t.CreatedAt
        }).ToList();
        var receipt = o.Receipts.OrderByDescending(r => r.Id).FirstOrDefault();
        if (receipt != null)
        {
            dto.Receipt = new ReceiptDto
            {
                Id = receipt.Id,
                ReceiptNo = receipt.ReceiptNo,
                Token = receipt.Token,
                PrintCount = receipt.PrintCount,
                CreatedAt = receipt.CreatedAt,
                Events = receipt.Events.OrderByDescending(e => e.Id).Select(e => new ReceiptEventDto { Action = e.Action, Note = e.Note, CreatedAt = e.CreatedAt }).ToList(),
                Dispatches = receipt.Dispatches.OrderByDescending(d => d.Id).Select(d => new DispatchDto { Channel = d.Channel, Destination = d.Destination, Status = d.Status, Note = d.Note, CreatedAt = d.CreatedAt }).ToList()
            };
        }
        return dto;
    }
}
