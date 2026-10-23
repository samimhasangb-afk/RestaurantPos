using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantPos.Services;

namespace RestaurantPos.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AuthService auth) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<LoginResponse> Login(LoginRequest req) => await auth.Login(req);

    [Authorize]
    [HttpGet("me")]
    public async Task<UserDto> Me()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return await auth.Me(int.Parse(id ?? "0"));
    }
}

[ApiController]
[Authorize]
[Route("api")]
public class CatalogController(CatalogService catalog) : ControllerBase
{
    [HttpGet("catalog")]
    public Task<CatalogDto> Catalog([FromQuery] int branchId, [FromQuery] int? terminalId) => catalog.Get(branchId, terminalId);

    [HttpPost("categories")] public Task<CategoryDto> SaveCategory(SaveCategoryRequest req) => catalog.SaveCategory(req);
    [HttpPost("products")] public Task<ProductDto> SaveProduct(SaveProductRequest req) => catalog.SaveProduct(req);
    [HttpPost("addons")] public Task<AddonDto> SaveAddon(SaveAddonRequest req) => catalog.SaveAddon(req);
    [HttpPost("coupons")] public Task<CouponDto> SaveCoupon(SaveCouponRequest req) => catalog.SaveCoupon(req);
    [HttpPost("modifier-groups")] public Task<ModGroupDto> SaveGroup(SaveModifierGroupRequest req) => catalog.SaveModifierGroup(req);
    [HttpPost("floors")] public Task<FloorDto> SaveFloor(SaveFloorRequest req) => catalog.SaveFloor(req);
    [HttpPost("tables")] public Task<TableDto> SaveTable(SaveTableRequest req) => catalog.SaveTable(req);
    [HttpGet("customers")] public Task<List<CustomerDto>> Customers() => catalog.Customers();
    [HttpPost("customers")] public Task<CustomerDto> SaveCustomer(SaveCustomerRequest req) => catalog.SaveCustomer(req);
    [HttpPost("customers/{id:int}/settle")] public Task<CustomerDto> Settle(int id, SettleRequest req) => catalog.Settle(id, req);
    [HttpGet("expenses")] public Task<List<ExpenseDto>> Expenses([FromQuery] int? branchId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to) => catalog.Expenses(branchId, from, to);
    [HttpPost("expenses")] public Task<ExpenseDto> SaveExpense(SaveExpenseRequest req) => catalog.SaveExpense(req);
}

[ApiController]
[Authorize]
[Route("api")]
public class OrdersController(SalesService sales) : ControllerBase
{
    [HttpGet("orders")]
    public Task<List<OrderSummaryDto>> List([FromQuery] int branchId, [FromQuery] string? status, [FromQuery] string? q, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
        => sales.List(branchId, status, q, from, to);

    [HttpPost("orders")] public Task<OrderDto> Create(CreateOrderRequest req) => sales.Create(req);
    [HttpGet("orders/{id:int}")] public Task<OrderDto> Get(int id) => sales.Get(id);
    [HttpPut("orders/{id:int}")] public Task<OrderDto> Update(int id, UpdateOrderRequest req) => sales.Update(id, req);
    [HttpPost("orders/{id:int}/lines")] public Task<OrderDto> AddLine(int id, LineRequest req) => sales.AddLine(id, req);
    [HttpPut("orders/{id:int}/lines/{lineId:int}")] public Task<OrderDto> UpdateLine(int id, int lineId, LineRequest req) => sales.UpdateLine(id, lineId, req);
    [HttpDelete("orders/{id:int}/lines/{lineId:int}")] public Task<OrderDto> RemoveLine(int id, int lineId) => sales.RemoveLine(id, lineId);
    [HttpPost("orders/{id:int}/coupon")] public Task<OrderDto> Coupon(int id, CouponRequest req) => sales.Coupon(id, req.Code);
    [HttpPost("orders/{id:int}/discount")] public Task<ActionResultDto> Discount(int id, DiscountRequest req) => sales.Discount(id, req.Amount);
    [HttpPost("orders/{id:int}/hold")] public Task<OrderDto> Hold(int id) => sales.Hold(id);
    [HttpPost("orders/{id:int}/resume")] public Task<OrderDto> Resume(int id) => sales.Resume(id);
    [HttpPost("orders/{id:int}/kitchen")] public Task<OrderDto> Kitchen(int id) => sales.SendKitchen(id);
    [HttpPost("orders/{id:int}/pay")] public Task<PayResultDto> Pay(int id, PayRequest req) => sales.Pay(id, req);
    [HttpPost("orders/{id:int}/refund")] public Task<ActionResultDto> Refund(int id, RefundRequest req) => sales.Refund(id, req);
    [HttpPost("orders/{id:int}/void")] public Task<ActionResultDto> Void(int id, ReasonRequest req) => sales.Void(id, req.Reason);
    [HttpPost("orders/{id:int}/dispatch")] public Task<DispatchDto> Dispatch(int id, DispatchRequest req) => sales.Dispatch(id, req);

    [HttpGet("orders/{id:int}/receipt")]
    public async Task<ContentResult> Receipt(int id, [FromQuery] int width = 80)
    {
        var html = await sales.Print(id, width, $"{Request.Scheme}://{Request.Host}");
        return new ContentResult { Content = html, ContentType = "text/html; charset=utf-8" };
    }

    [HttpGet("kitchen")] public Task<List<KitchenCardDto>> Board([FromQuery] int branchId) => sales.Kitchen(branchId);
    [HttpPost("kitchen/lines/{lineId:int}")] public Task<OrderDto> Prep(int lineId, PrepRequest req) => sales.SetPrep(lineId, req.Status);

    [HttpGet("shifts/current")] public Task<ShiftDto?> Current([FromQuery] int branchId, [FromQuery] int? terminalId) => sales.CurrentShift(branchId, terminalId);
    [HttpPost("shifts/open")] public Task<ShiftDto> Open(OpenShiftRequest req) => sales.OpenShift(req);
    [HttpPost("shifts/{id:int}/close")] public Task<ShiftDto> Close(int id, CloseShiftRequest req) => sales.CloseShift(id, req);
}

[ApiController]
[Authorize]
[Route("api")]
public class InventoryController(InventoryService inventory) : ControllerBase
{
    [HttpGet("ingredients")] public Task<List<IngredientDto>> Ingredients() => inventory.Ingredients();
    [HttpPost("ingredients")] public Task<IngredientDto> SaveIngredient(SaveIngredientRequest req) => inventory.SaveIngredient(req);
    [HttpGet("products/{id:int}/recipe")] public Task<List<RecipeLineDto>> Recipe(int id) => inventory.Recipe(id);
    [HttpPut("products/{id:int}/recipe")] public Task<List<RecipeLineDto>> SaveRecipe(int id, SaveRecipeRequest req) => inventory.SaveRecipe(id, req);
    [HttpGet("stock")] public Task<List<StockRowDto>> Stock([FromQuery] int branchId) => inventory.Stock(branchId);
    [HttpGet("stock/ledger")] public Task<List<LedgerDto>> Ledger([FromQuery] int branchId, [FromQuery] int? ingredientId) => inventory.Ledger(branchId, ingredientId);
    [HttpPost("stock/wastage")] public async Task<IActionResult> Wastage(WastageRequest req) { await inventory.Wastage(req); return Ok(new { message = "Wastage recorded." }); }
    [HttpPost("stock/adjustments")] public Task<ActionResultDto> Adjust(AdjustmentRequest req) => inventory.Adjust(req);
    [HttpPost("stock/counts")] public async Task<IActionResult> Count(CountRequest req) { await inventory.Count(req); return Ok(new { message = "Physical count posted." }); }
    [HttpPost("stock/transfers")] public async Task<IActionResult> Transfer(TransferRequest req) { await inventory.Transfer(req); return Ok(new { message = "Stock transferred." }); }
    [HttpGet("suppliers")] public Task<List<SupplierDto>> Suppliers() => inventory.Suppliers();
    [HttpPost("suppliers")] public Task<SupplierDto> SaveSupplier(SaveSupplierRequest req) => inventory.SaveSupplier(req);
    [HttpPost("suppliers/{id:int}/pay")] public async Task<IActionResult> PaySupplier(int id, SettleRequest req) { await inventory.PaySupplier(id, req); return Ok(new { message = "Supplier payment recorded." }); }
    [HttpGet("purchase-orders")] public Task<List<PurchaseOrderDto>> Pos([FromQuery] int? branchId) => inventory.PurchaseOrders(branchId);
    [HttpPost("purchase-orders")] public Task<PurchaseOrderDto> CreatePo(SavePoRequest req) => inventory.CreatePo(req);
    [HttpGet("purchase-orders/{id:int}")] public Task<PurchaseOrderDto> GetPo(int id) => inventory.GetPo(id);
    [HttpPost("purchase-orders/{id:int}/receive")] public Task<PurchaseOrderDto> Receive(int id, ReceiveRequest req) => inventory.Receive(id, req);
}

[ApiController]
[Authorize]
[Route("api/reports")]
public class ReportsController(ReportService reports) : ControllerBase
{
    [HttpGet("overview")]
    public Task<OverviewDto> Overview([FromQuery] int branchId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to) => reports.Overview(branchId, from, to);
}

[ApiController]
[Authorize]
[Route("api/admin")]
public class AdminController(AdminService admin) : ControllerBase
{
    [HttpGet("roles")] public Task<List<RoleDto>> Roles() => admin.Roles();
    [HttpGet("users")] public Task<List<UserDto>> Users() => admin.Users();
    [HttpPost("users")] public Task<UserDto> SaveUser(SaveUserRequest req) => admin.SaveUser(req);
    [HttpGet("branches")] public Task<List<BranchDto>> Branches() => admin.Branches();
    [HttpPost("branches")] public Task<BranchDto> SaveBranch(SaveBranchRequest req) => admin.SaveBranch(req);
    [HttpGet("terminals")] public Task<List<TerminalDto>> Terminals([FromQuery] int? branchId) => admin.Terminals(branchId);
    [HttpPost("terminals")] public Task<TerminalDto> SaveTerminal(SaveTerminalRequest req) => admin.SaveTerminal(req);
    [HttpGet("audit")] public Task<List<AuditDto>> Audit() => admin.Audit();
    [HttpGet("logins")] public Task<List<LoginHistoryDto>> Logins() => admin.Logins();
    [HttpGet("settings")] public Task<List<SettingDto>> Settings() => admin.GetSettings();
    [HttpPut("settings")] public Task<List<SettingDto>> SaveSettings(List<SettingDto> rows) => admin.SaveSettings(rows);
    [HttpGet("approvals")] public Task<List<ApprovalDto>> Approvals() => admin.Approvals();
    [HttpPost("approvals/{id:int}/approve")] public Task<ApprovalDto> Approve(int id, ReviewRequest req) => admin.Review(id, true, req.Note);
    [HttpPost("approvals/{id:int}/reject")] public Task<ApprovalDto> Reject(int id, ReviewRequest req) => admin.Review(id, false, req.Note);
    [HttpPost("backup")] public Task<BackupDto> Backup() => admin.Backup();
    [HttpGet("backups")] public Task<List<string>> Backups() => admin.BackupFiles();
    [HttpGet("monitor")] public Task<MonitorDto> Monitor() => admin.Monitor();
}

[ApiController]
[Route("api/public")]
public class PublicController(SalesService sales) : ControllerBase
{
    [HttpGet("receipts/{token}")]
    public Task<VerifyDto> Verify(string token) => sales.Verify(token);

    [HttpGet("receipts/{token}/html")]
    public async Task<ContentResult> Html(string token, [FromQuery] int width = 80)
    {
        var html = await sales.HtmlByToken(token, width, $"{Request.Scheme}://{Request.Host}");
        return new ContentResult { Content = html, ContentType = "text/html; charset=utf-8" };
    }

    [HttpGet("receipts/{token}/qr.png")]
    public async Task<IActionResult> Qr(string token) => File(await sales.Qr(token), "image/png");
}

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { ok = true, name = "Saffron POS" });
}
