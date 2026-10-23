namespace RestaurantPos.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Terminal> Terminals => Set<Terminal>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<UserBranch> UserBranches => Set<UserBranch>();
    public DbSet<Floor> Floors => Set<Floor>();
    public DbSet<DiningTable> DiningTables => Set<DiningTable>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ModifierGroup> ModifierGroups => Set<ModifierGroup>();
    public DbSet<ModifierOption> ModifierOptions => Set<ModifierOption>();
    public DbSet<ProductModifierGroup> ProductModifierGroups => Set<ProductModifierGroup>();
    public DbSet<Addon> Addons => Set<Addon>();
    public DbSet<ProductAddon> ProductAddons => Set<ProductAddon>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<DuePayment> DuePayments => Set<DuePayment>();
    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<SaleOrder> SaleOrders => Set<SaleOrder>();
    public DbSet<SaleLine> SaleLines => Set<SaleLine>();
    public DbSet<SaleLineExtra> SaleLineExtras => Set<SaleLineExtra>();
    public DbSet<Tender> Tenders => Set<Tender>();
    public DbSet<Receipt> Receipts => Set<Receipt>();
    public DbSet<ReceiptEvent> ReceiptEvents => Set<ReceiptEvent>();
    public DbSet<ReceiptDispatch> ReceiptDispatches => Set<ReceiptDispatch>();
    public DbSet<KitchenTicket> KitchenTickets => Set<KitchenTicket>();
    public DbSet<KitchenTicketLine> KitchenTicketLines => Set<KitchenTicketLine>();
    public DbSet<NumberSequence> NumberSequences => Set<NumberSequence>();
    public DbSet<Ingredient> Ingredients => Set<Ingredient>();
    public DbSet<RecipeLine> RecipeLines => Set<RecipeLine>();
    public DbSet<StockBalance> StockBalances => Set<StockBalance>();
    public DbSet<StockLedger> StockLedgers => Set<StockLedger>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<SupplierPayment> SupplierPayments => Set<SupplierPayment>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();
    public DbSet<GoodsReceipt> GoodsReceipts => Set<GoodsReceipt>();
    public DbSet<GoodsReceiptLine> GoodsReceiptLines => Set<GoodsReceiptLine>();
    public DbSet<StockTransfer> StockTransfers => Set<StockTransfer>();
    public DbSet<StockTransferLine> StockTransferLines => Set<StockTransferLine>();
    public DbSet<Wastage> Wastages => Set<Wastage>();
    public DbSet<StockAdjustment> StockAdjustments => Set<StockAdjustment>();
    public DbSet<PhysicalCount> PhysicalCounts => Set<PhysicalCount>();
    public DbSet<PhysicalCountLine> PhysicalCountLines => Set<PhysicalCountLine>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<ApprovalRequest> ApprovalRequests => Set<ApprovalRequest>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<LoginHistory> LoginHistories => Set<LoginHistory>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<RolePermission>().HasKey(x => new { x.RoleId, x.PermissionId });
        builder.Entity<RolePermission>().HasOne(x => x.Role).WithMany(x => x.RolePermissions).HasForeignKey(x => x.RoleId);
        builder.Entity<RolePermission>().HasOne(x => x.Permission).WithMany().HasForeignKey(x => x.PermissionId);

        builder.Entity<UserBranch>().HasKey(x => new { x.UserId, x.BranchId });
        builder.Entity<UserBranch>().HasOne(x => x.User).WithMany(x => x.Branches).HasForeignKey(x => x.UserId);
        builder.Entity<UserBranch>().HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId);

        builder.Entity<AppUser>().HasOne(x => x.DefaultBranch).WithMany().HasForeignKey(x => x.DefaultBranchId);
        builder.Entity<AppUser>().HasIndex(x => x.Username).IsUnique();

        builder.Entity<ProductModifierGroup>().HasKey(x => new { x.ProductId, x.ModifierGroupId });
        builder.Entity<ProductModifierGroup>().HasOne(x => x.Product).WithMany(x => x.ModifierGroups).HasForeignKey(x => x.ProductId);
        builder.Entity<ProductModifierGroup>().HasOne(x => x.ModifierGroup).WithMany().HasForeignKey(x => x.ModifierGroupId);

        builder.Entity<ProductAddon>().HasKey(x => new { x.ProductId, x.AddonId });
        builder.Entity<ProductAddon>().HasOne(x => x.Product).WithMany(x => x.Addons).HasForeignKey(x => x.ProductId);
        builder.Entity<ProductAddon>().HasOne(x => x.Addon).WithMany().HasForeignKey(x => x.AddonId);

        builder.Entity<SaleOrder>().HasOne(x => x.Waiter).WithMany().HasForeignKey(x => x.WaiterId);
        builder.Entity<SaleOrder>().HasOne(x => x.Cashier).WithMany().HasForeignKey(x => x.CashierId);
        builder.Entity<SaleOrder>().HasOne(x => x.Rider).WithMany().HasForeignKey(x => x.RiderId);

        builder.Entity<StockTransfer>().HasOne(x => x.FromBranch).WithMany().HasForeignKey(x => x.FromBranchId);
        builder.Entity<StockTransfer>().HasOne(x => x.ToBranch).WithMany().HasForeignKey(x => x.ToBranchId);

        builder.Entity<SaleOrder>().HasIndex(x => x.OrderNo).IsUnique();
        builder.Entity<Receipt>().HasIndex(x => x.ReceiptNo).IsUnique();
        builder.Entity<Receipt>().HasIndex(x => x.Token).IsUnique();
        builder.Entity<Permission>().HasIndex(x => x.Key).IsUnique();
        builder.Entity<Coupon>().HasIndex(x => new { x.OrganizationId, x.Code }).IsUnique();
        builder.Entity<NumberSequence>().HasIndex(x => new { x.OrganizationId, x.Key }).IsUnique();
        builder.Entity<StockBalance>().HasIndex(x => new { x.BranchId, x.IngredientId }).IsUnique();
        builder.Entity<Role>().HasIndex(x => new { x.OrganizationId, x.Name }).IsUnique();
        builder.Entity<SystemSetting>().HasIndex(x => new { x.OrganizationId, x.Key }).IsUnique();
        builder.Entity<Product>().HasIndex(x => new { x.OrganizationId, x.Barcode }).IsUnique().HasFilter("[Barcode] IS NOT NULL");
        builder.Entity<SaleOrder>().HasIndex(x => new { x.BranchId, x.CreatedAt });

        foreach (var entity in builder.Model.GetEntityTypes())
        {
            foreach (var fk in entity.GetForeignKeys())
                fk.DeleteBehavior = DeleteBehavior.Restrict;

            foreach (var prop in entity.GetProperties())
            {
                if (prop.ClrType == typeof(decimal) || prop.ClrType == typeof(decimal?))
                {
                    var qty = prop.Name is "Quantity" or "Qty" or "QtyDelta" or "CountedQty" or "SystemQty" or "ReceivedQty" or "ReorderLevel";
                    prop.SetPrecision(18);
                    prop.SetScale(qty ? 3 : 2);
                }
                else if (prop.ClrType == typeof(string))
                {
                    prop.SetMaxLength(prop.Name switch
                    {
                        "Name" or "FullName" => 160,
                        "Username" => 60,
                        "PasswordHash" => 500,
                        "Email" => 160,
                        "Phone" or "DeliveryPhone" => 40,
                        "Notes" or "Note" or "Reason" or "Address" or "DeliveryAddress" or "Detail" or "ReviewNote" or "Value" => 2000,
                        "UserAgent" => 400,
                        "Barcode" or "Sku" or "Code" or "OrderNo" or "ReceiptNo" or "Token" or "PoNo" or "GrnNo" or "TicketNo" or "Key" => 60,
                        _ when prop.Name.Contains("Json") => 4000,
                        _ => 80
                    });
                }
            }
        }
    }
}
