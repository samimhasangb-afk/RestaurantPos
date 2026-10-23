using RestaurantPos.Data;

namespace RestaurantPos.Services;

public class CatalogService
{
    private readonly AppDbContext _db;
    private readonly Gate _gate;
    private readonly Tracker _tracker;

    public CatalogService(AppDbContext db, Gate gate, Tracker tracker)
    {
        _db = db;
        _gate = gate;
        _tracker = tracker;
    }

    public async Task<CatalogDto> Get(int branchId, int? terminalId)
    {
        var (me, branch) = await _gate.Branch(branchId);
        var categories = await _db.Categories.Where(c => c.OrganizationId == me.OrganizationId).OrderBy(c => c.SortOrder).ToListAsync();
        var products = await _db.Products.Include(p => p.ModifierGroups).Include(p => p.Addons)
            .Where(p => p.OrganizationId == me.OrganizationId).OrderBy(p => p.Name).ToListAsync();
        var groups = await _db.ModifierGroups.Include(g => g.Options).Where(g => g.OrganizationId == me.OrganizationId).ToListAsync();
        var addons = await _db.Addons.Where(a => a.OrganizationId == me.OrganizationId).OrderBy(a => a.Name).ToListAsync();
        var coupons = await _db.Coupons.Where(c => c.OrganizationId == me.OrganizationId).OrderBy(c => c.Code).ToListAsync();
        var floors = await _db.Floors.Where(f => f.BranchId == branchId).OrderBy(f => f.SortOrder).ToListAsync();
        var tables = await _db.DiningTables.Where(t => t.BranchId == branchId).OrderBy(t => t.Name).ToListAsync();
        var openOrders = await _db.SaleOrders.Where(o => o.BranchId == branchId && o.TableId != null && o.Status != "Void" && (o.PaymentStatus == "Unpaid" || o.PaymentStatus == "Partial"))
            .Select(o => new { o.TableId, o.Id }).ToListAsync();
        var branchIds = new List<int> { branchId };
        var staff = await _db.Users.Include(u => u.Role).Include(u => u.Branches)
            .Where(u => u.OrganizationId == me.OrganizationId && u.IsActive && (u.Role.Name == "Admin" || u.Role.Name == "Manager" || u.Branches.Any(b => branchIds.Contains(b.BranchId))))
            .OrderBy(u => u.FullName).ToListAsync();
        var customers = await _db.Customers.Where(c => c.OrganizationId == me.OrganizationId && c.IsActive).OrderBy(c => c.Name).ToListAsync();
        var terminals = await _db.Terminals.Where(t => t.BranchId == branchId && t.IsActive).OrderBy(t => t.Name).ToListAsync();
        var shiftQ = _db.Shifts.Include(s => s.Terminal).Include(s => s.User).Where(s => s.BranchId == branchId && s.Status == "Open");
        if (terminalId is int tid) shiftQ = shiftQ.Where(s => s.TerminalId == tid);
        var shift = await shiftQ.OrderByDescending(s => s.Id).FirstOrDefaultAsync();

        return new CatalogDto
        {
            Branch = Map.Branch(branch),
            Categories = categories.Select(c => new CategoryDto { Id = c.Id, Name = c.Name, SortOrder = c.SortOrder, Color = c.Color, IsActive = c.IsActive }).ToList(),
            Products = products.Select(p => new ProductDto
            {
                Id = p.Id, CategoryId = p.CategoryId, Name = p.Name, Sku = p.Sku, Barcode = p.Barcode, Price = p.Price, CostPrice = p.CostPrice,
                PrepMinutes = p.PrepMinutes, Color = p.Color, IsActive = p.IsActive,
                ModifierGroupIds = p.ModifierGroups.Select(m => m.ModifierGroupId).ToList(),
                AddonIds = p.Addons.Select(a => a.AddonId).ToList()
            }).ToList(),
            ModifierGroups = groups.Select(g => new ModGroupDto
            {
                Id = g.Id, Name = g.Name, Required = g.Required, MinSelect = g.MinSelect, MaxSelect = g.MaxSelect,
                Options = g.Options.Where(o => o.IsActive).Select(o => new ModOptionDto { Id = o.Id, Name = o.Name, Price = o.Price }).ToList()
            }).ToList(),
            Addons = addons.Select(a => new AddonDto { Id = a.Id, Name = a.Name, Price = a.Price, IsActive = a.IsActive }).ToList(),
            Coupons = coupons.Select(c => new CouponDto { Id = c.Id, Code = c.Code, DiscountType = c.DiscountType, Value = c.Value, MinOrder = c.MinOrder, MaxDiscount = c.MaxDiscount, ExpiresOn = c.ExpiresOn, IsActive = c.IsActive }).ToList(),
            Floors = floors.Select(f => new FloorDto { Id = f.Id, BranchId = f.BranchId, Name = f.Name, SortOrder = f.SortOrder }).ToList(),
            Tables = tables.Select(t => new TableDto
            {
                Id = t.Id, BranchId = t.BranchId, FloorId = t.FloorId, Name = t.Name, Seats = t.Seats, Status = t.Status,
                OpenOrderId = openOrders.FirstOrDefault(o => o.TableId == t.Id)?.Id
            }).ToList(),
            Staff = staff.Select(s => new StaffDto { Id = s.Id, FullName = s.FullName, Role = s.Role.Name }).ToList(),
            Customers = customers.Select(c => new CustomerDto { Id = c.Id, Name = c.Name, Phone = c.Phone, Email = c.Email, Address = c.Address, DueBalance = c.DueBalance, IsActive = c.IsActive }).ToList(),
            Terminals = terminals.Select(t => new TerminalDto { Id = t.Id, BranchId = t.BranchId, Name = t.Name, IsActive = t.IsActive }).ToList(),
            Shift = shift == null ? null : Map.Shift(shift)
        };
    }

    public async Task<CategoryDto> SaveCategory(SaveCategoryRequest req)
    {
        var me = await _gate.Me();
        me.Require("menu.manage");
        if (string.IsNullOrWhiteSpace(req.Name)) throw new AppException("Category name is required.");
        Category row;
        if (req.Id is int id)
            row = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == me.OrganizationId) ?? throw new AppException("Category not found.", 404);
        else { row = new Category { OrganizationId = me.OrganizationId }; _db.Categories.Add(row); }
        row.Name = req.Name.Trim();
        row.SortOrder = req.SortOrder;
        row.Color = string.IsNullOrWhiteSpace(req.Color) ? "#d4652f" : req.Color.Trim();
        row.IsActive = req.IsActive;
        await _db.SaveChangesAsync();
        return new CategoryDto { Id = row.Id, Name = row.Name, SortOrder = row.SortOrder, Color = row.Color, IsActive = row.IsActive };
    }

    public async Task<ProductDto> SaveProduct(SaveProductRequest req)
    {
        var me = await _gate.Me();
        me.Require("menu.manage");
        if (string.IsNullOrWhiteSpace(req.Name)) throw new AppException("Item name is required.");
        var category = await _db.Categories.AnyAsync(c => c.Id == req.CategoryId && c.OrganizationId == me.OrganizationId);
        if (!category) throw new AppException("Category not found.", 404);
        Product row;
        if (req.Id is int id)
            row = await _db.Products.Include(p => p.ModifierGroups).Include(p => p.Addons).FirstOrDefaultAsync(p => p.Id == id && p.OrganizationId == me.OrganizationId)
                ?? throw new AppException("Item not found.", 404);
        else { row = new Product { OrganizationId = me.OrganizationId }; _db.Products.Add(row); }
        row.CategoryId = req.CategoryId;
        row.Name = req.Name.Trim();
        row.Sku = Clean(req.Sku);
        row.Barcode = Clean(req.Barcode);
        row.Price = Money.R(req.Price);
        row.CostPrice = Money.R(req.CostPrice);
        row.PrepMinutes = req.PrepMinutes;
        row.Color = string.IsNullOrWhiteSpace(req.Color) ? "#efe6d9" : req.Color.Trim();
        row.IsActive = req.IsActive;
        await _db.SaveChangesAsync();
        var oldMods = await _db.ProductModifierGroups.Where(x => x.ProductId == row.Id).ToListAsync();
        var oldAdds = await _db.ProductAddons.Where(x => x.ProductId == row.Id).ToListAsync();
        _db.ProductModifierGroups.RemoveRange(oldMods);
        _db.ProductAddons.RemoveRange(oldAdds);
        foreach (var gid in req.ModifierGroupIds.Distinct())
            _db.ProductModifierGroups.Add(new ProductModifierGroup { ProductId = row.Id, ModifierGroupId = gid });
        foreach (var aid in req.AddonIds.Distinct())
            _db.ProductAddons.Add(new ProductAddon { ProductId = row.Id, AddonId = aid });
        _tracker.Log(me.OrganizationId, me.Id, "SaveProduct", "Product", row.Id.ToString(), row.Name);
        await _db.SaveChangesAsync();
        return new ProductDto
        {
            Id = row.Id, CategoryId = row.CategoryId, Name = row.Name, Sku = row.Sku, Barcode = row.Barcode, Price = row.Price,
            CostPrice = row.CostPrice, PrepMinutes = row.PrepMinutes, Color = row.Color, IsActive = row.IsActive,
            ModifierGroupIds = req.ModifierGroupIds.Distinct().ToList(), AddonIds = req.AddonIds.Distinct().ToList()
        };
    }

    public async Task<AddonDto> SaveAddon(SaveAddonRequest req)
    {
        var me = await _gate.Me();
        me.Require("menu.manage");
        if (string.IsNullOrWhiteSpace(req.Name)) throw new AppException("Add-on name is required.");
        Addon row;
        if (req.Id is int id) row = await _db.Addons.FirstOrDefaultAsync(a => a.Id == id && a.OrganizationId == me.OrganizationId) ?? throw new AppException("Add-on not found.", 404);
        else { row = new Addon { OrganizationId = me.OrganizationId }; _db.Addons.Add(row); }
        row.Name = req.Name.Trim();
        row.Price = Money.R(req.Price);
        row.IsActive = req.IsActive;
        await _db.SaveChangesAsync();
        return new AddonDto { Id = row.Id, Name = row.Name, Price = row.Price, IsActive = row.IsActive };
    }

    public async Task<CouponDto> SaveCoupon(SaveCouponRequest req)
    {
        var me = await _gate.Me();
        me.Require("menu.manage");
        if (string.IsNullOrWhiteSpace(req.Code)) throw new AppException("Coupon code is required.");
        Coupon row;
        if (req.Id is int id) row = await _db.Coupons.FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == me.OrganizationId) ?? throw new AppException("Coupon not found.", 404);
        else { row = new Coupon { OrganizationId = me.OrganizationId }; _db.Coupons.Add(row); }
        row.Code = req.Code.Trim().ToUpperInvariant();
        row.DiscountType = req.DiscountType == "Fixed" ? "Fixed" : "Percent";
        row.Value = req.Value;
        row.MinOrder = req.MinOrder;
        row.MaxDiscount = req.MaxDiscount;
        row.ExpiresOn = req.ExpiresOn;
        row.IsActive = req.IsActive;
        await _db.SaveChangesAsync();
        return new CouponDto { Id = row.Id, Code = row.Code, DiscountType = row.DiscountType, Value = row.Value, MinOrder = row.MinOrder, MaxDiscount = row.MaxDiscount, ExpiresOn = row.ExpiresOn, IsActive = row.IsActive };
    }

    public async Task<ModGroupDto> SaveModifierGroup(SaveModifierGroupRequest req)
    {
        var me = await _gate.Me();
        me.Require("menu.manage");
        if (string.IsNullOrWhiteSpace(req.Name)) throw new AppException("Modifier group name is required.");
        ModifierGroup row;
        if (req.Id is int id)
            row = await _db.ModifierGroups.Include(g => g.Options).FirstOrDefaultAsync(g => g.Id == id && g.OrganizationId == me.OrganizationId) ?? throw new AppException("Modifier group not found.", 404);
        else { row = new ModifierGroup { OrganizationId = me.OrganizationId }; _db.ModifierGroups.Add(row); }
        row.Name = req.Name.Trim();
        row.Required = req.Required;
        row.MinSelect = req.MinSelect;
        row.MaxSelect = req.MaxSelect;
        await _db.SaveChangesAsync();
        if (req.Options.Count > 0)
        {
            var old = await _db.ModifierOptions.Where(o => o.ModifierGroupId == row.Id).ToListAsync();
            foreach (var option in old) option.IsActive = false;
            foreach (var option in req.Options.Where(o => !string.IsNullOrWhiteSpace(o.Name)))
                _db.ModifierOptions.Add(new ModifierOption { ModifierGroupId = row.Id, Name = option.Name.Trim(), Price = Money.R(option.Price), IsActive = true });
        }
        await _db.SaveChangesAsync();
        var options = await _db.ModifierOptions.Where(o => o.ModifierGroupId == row.Id && o.IsActive).ToListAsync();
        return new ModGroupDto
        {
            Id = row.Id, Name = row.Name, Required = row.Required, MinSelect = row.MinSelect, MaxSelect = row.MaxSelect,
            Options = options.Select(o => new ModOptionDto { Id = o.Id, Name = o.Name, Price = o.Price }).ToList()
        };
    }

    public async Task<FloorDto> SaveFloor(SaveFloorRequest req)
    {
        var (me, _) = await _gate.Branch(req.BranchId);
        me.Require("menu.manage", "admin.settings");
        if (string.IsNullOrWhiteSpace(req.Name)) throw new AppException("Floor name is required.");
        Floor row;
        if (req.Id is int id) row = await _db.Floors.FirstOrDefaultAsync(f => f.Id == id && f.BranchId == req.BranchId) ?? throw new AppException("Floor not found.", 404);
        else { row = new Floor { BranchId = req.BranchId }; _db.Floors.Add(row); }
        row.Name = req.Name.Trim();
        row.SortOrder = req.SortOrder;
        await _db.SaveChangesAsync();
        return new FloorDto { Id = row.Id, BranchId = row.BranchId, Name = row.Name, SortOrder = row.SortOrder };
    }

    public async Task<TableDto> SaveTable(SaveTableRequest req)
    {
        var (me, _) = await _gate.Branch(req.BranchId);
        me.Require("menu.manage", "admin.settings");
        if (string.IsNullOrWhiteSpace(req.Name)) throw new AppException("Table name is required.");
        var floorOk = await _db.Floors.AnyAsync(f => f.Id == req.FloorId && f.BranchId == req.BranchId);
        if (!floorOk) throw new AppException("Floor not found.", 404);
        DiningTable row;
        if (req.Id is int id) row = await _db.DiningTables.FirstOrDefaultAsync(t => t.Id == id && t.BranchId == req.BranchId) ?? throw new AppException("Table not found.", 404);
        else { row = new DiningTable { BranchId = req.BranchId }; _db.DiningTables.Add(row); }
        row.FloorId = req.FloorId;
        row.Name = req.Name.Trim();
        row.Seats = req.Seats <= 0 ? 4 : req.Seats;
        if (req.Status is "Free" or "Occupied" or "Reserved") row.Status = req.Status;
        await _db.SaveChangesAsync();
        return new TableDto { Id = row.Id, BranchId = row.BranchId, FloorId = row.FloorId, Name = row.Name, Seats = row.Seats, Status = row.Status };
    }

    public async Task<List<CustomerDto>> Customers()
    {
        var me = await _gate.Me();
        me.Require("customer.manage", "pos.order");
        return await _db.Customers.Where(c => c.OrganizationId == me.OrganizationId).OrderBy(c => c.Name)
            .Select(c => new CustomerDto { Id = c.Id, Name = c.Name, Phone = c.Phone, Email = c.Email, Address = c.Address, DueBalance = c.DueBalance, IsActive = c.IsActive }).ToListAsync();
    }

    public async Task<CustomerDto> SaveCustomer(SaveCustomerRequest req)
    {
        var me = await _gate.Me();
        me.Require("customer.manage", "pos.order");
        if (string.IsNullOrWhiteSpace(req.Name)) throw new AppException("Customer name is required.");
        Customer row;
        if (req.Id is int id) row = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == me.OrganizationId) ?? throw new AppException("Customer not found.", 404);
        else { row = new Customer { OrganizationId = me.OrganizationId }; _db.Customers.Add(row); }
        row.Name = req.Name.Trim();
        row.Phone = Clean(req.Phone);
        row.Email = Clean(req.Email);
        row.Address = Clean(req.Address);
        row.IsActive = req.IsActive;
        await _db.SaveChangesAsync();
        return new CustomerDto { Id = row.Id, Name = row.Name, Phone = row.Phone, Email = row.Email, Address = row.Address, DueBalance = row.DueBalance, IsActive = row.IsActive };
    }

    public async Task<CustomerDto> Settle(int id, SettleRequest req)
    {
        var me = await _gate.Me();
        me.Require("customer.manage", "pos.pay");
        var row = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == me.OrganizationId) ?? throw new AppException("Customer not found.", 404);
        if (req.Amount <= 0) throw new AppException("Amount must be greater than zero.");
        if (req.Amount > row.DueBalance + 0.009m) throw new AppException("Amount is more than the customer due.");
        row.DueBalance = Money.R(row.DueBalance - req.Amount);
        _db.DuePayments.Add(new DuePayment { CustomerId = row.Id, Amount = Money.R(req.Amount), Method = Pricing.Method(req.Method), Note = Clean(req.Note), UserId = me.Id });
        _tracker.Log(me.OrganizationId, me.Id, "SettleDue", "Customer", row.Id.ToString(), req.Amount.ToString("0.00"));
        await _db.SaveChangesAsync();
        return new CustomerDto { Id = row.Id, Name = row.Name, Phone = row.Phone, Email = row.Email, Address = row.Address, DueBalance = row.DueBalance, IsActive = row.IsActive };
    }

    public async Task<List<ExpenseDto>> Expenses(int? branchId, DateOnly? from, DateOnly? to)
    {
        var me = await _gate.Me();
        me.Require("expense.manage", "reports.view");
        var ids = await _gate.BranchIds();
        var q = _db.Expenses.Include(e => e.Branch).Where(e => e.OrganizationId == me.OrganizationId && ids.Contains(e.BranchId));
        if (branchId is int b) q = q.Where(e => e.BranchId == b);
        if (from is DateOnly f) q = q.Where(e => e.SpentAt >= Clock.BdDateStartUtc(f));
        if (to is DateOnly t) q = q.Where(e => e.SpentAt < Clock.BdDateStartUtc(t.AddDays(1)));
        return await q.OrderByDescending(e => e.SpentAt).Take(200).Select(e => new ExpenseDto
        {
            Id = e.Id, BranchId = e.BranchId, BranchName = e.Branch.Name, Category = e.Category, Amount = e.Amount, Note = e.Note, SpentAt = e.SpentAt
        }).ToListAsync();
    }

    public async Task<ExpenseDto> SaveExpense(SaveExpenseRequest req)
    {
        var (me, branch) = await _gate.Branch(req.BranchId);
        me.Require("expense.manage");
        if (string.IsNullOrWhiteSpace(req.Category)) throw new AppException("Expense category is required.");
        if (req.Amount <= 0) throw new AppException("Amount must be greater than zero.");
        var row = new Expense
        {
            OrganizationId = me.OrganizationId,
            BranchId = branch.Id,
            Category = req.Category.Trim(),
            Amount = Money.R(req.Amount),
            Note = Clean(req.Note),
            UserId = me.Id,
            SpentAt = req.SpentAt ?? DateTime.UtcNow
        };
        _db.Expenses.Add(row);
        _tracker.Log(me.OrganizationId, me.Id, "Expense", "Expense", null, row.Category + " " + row.Amount.ToString("0.00"));
        await _db.SaveChangesAsync();
        return new ExpenseDto { Id = row.Id, BranchId = row.BranchId, BranchName = branch.Name, Category = row.Category, Amount = row.Amount, Note = row.Note, SpentAt = row.SpentAt };
    }

    static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
