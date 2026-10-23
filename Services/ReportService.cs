using RestaurantPos.Data;

namespace RestaurantPos.Services;

public class ReportService
{
    private readonly AppDbContext _db;
    private readonly Gate _gate;

    public ReportService(AppDbContext db, Gate gate)
    {
        _db = db;
        _gate = gate;
    }

    public async Task<OverviewDto> Overview(int branchId, DateOnly? from, DateOnly? to)
    {
        var me = await _gate.Me();
        me.Require("reports.view");
        var allowed = await _gate.BranchIds();
        var ids = branchId > 0 ? allowed.Where(id => id == branchId).ToList() : allowed;
        if (ids.Count == 0) throw new AppException("Branch not found.", 404);
        var start = from ?? Clock.TodayBd();
        var end = to ?? start;
        if (end < start) (start, end) = (end, start);
        var startUtc = Clock.BdDateStartUtc(start);
        var endUtc = Clock.BdDateStartUtc(end.AddDays(1));

        var orders = await _db.SaleOrders.Include(o => o.Branch).Include(o => o.Cashier).Include(o => o.Tenders)
            .Where(o => ids.Contains(o.BranchId) && o.Status != "Void" && o.CreatedAt >= startUtc && o.CreatedAt < endUtc)
            .ToListAsync();
        var orderIds = orders.Select(o => o.Id).ToList();
        var lines = await _db.SaleLines.Where(l => orderIds.Contains(l.SaleOrderId) && !l.IsVoid).ToListAsync();
        var products = await _db.Products.Where(p => p.OrganizationId == me.OrganizationId && p.IsActive).ToListAsync();
        var expenses = await _db.Expenses.Where(e => ids.Contains(e.BranchId) && e.SpentAt >= startUtc && e.SpentAt < endUtc).SumAsync(e => (decimal?)e.Amount) ?? 0;
        var wastage = await _db.Wastages.Where(w => ids.Contains(w.BranchId) && w.CreatedAt >= startUtc && w.CreatedAt < endUtc).SumAsync(w => (decimal?)w.Cost) ?? 0;

        var gross = orders.Sum(o => o.Subtotal);
        var discount = orders.Sum(o => o.DiscountTotal);
        var tax = orders.Sum(o => o.TaxTotal);
        var service = orders.Sum(o => o.ServiceCharge);
        var refunds = orders.Sum(o => o.RefundedTotal);
        var net = Money.R(gross - discount - refunds);
        var food = orders.Sum(o => o.FoodCost);

        var sold = lines.GroupBy(l => l.ProductId).Select(g => new { Id = g.Key, Name = g.First().Name, Qty = g.Sum(x => x.Qty), Amount = g.Sum(x => x.LineTotal) }).ToList();
        var best = sold.OrderByDescending(x => x.Qty).Take(8).Select(x => new NamedAmount { Name = x.Name, Qty = x.Qty, Amount = x.Amount }).ToList();
        var slow = products.Select(p =>
        {
            var hit = sold.FirstOrDefault(s => s.Id == p.Id);
            return new NamedAmount { Name = p.Name, Qty = hit?.Qty ?? 0, Amount = hit?.Amount ?? 0 };
        }).OrderBy(x => x.Qty).ThenBy(x => x.Name).Take(8).ToList();

        var branches = await _db.Branches.Where(b => allowed.Contains(b.Id)).ToListAsync();
        var allForCompare = await _db.SaleOrders.Where(o => allowed.Contains(o.BranchId) && o.Status != "Void" && o.CreatedAt >= startUtc && o.CreatedAt < endUtc)
            .Select(o => new { o.BranchId, Net = o.Subtotal - o.DiscountTotal - o.RefundedTotal }).ToListAsync();

        var daily = orders.GroupBy(o => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(o.CreatedAt, DateTimeKind.Utc), Clock.Zone)))
            .OrderBy(g => g.Key)
            .Select(g => new NamedAmount { Name = g.Key.ToString("dd MMM"), Amount = g.Sum(o => o.Subtotal - o.DiscountTotal - o.RefundedTotal), Count = g.Count() })
            .ToList();

        var payments = orders.SelectMany(o => o.Tenders).Where(t => !t.IsRefund).GroupBy(t => t.Method)
            .Select(g => new NamedAmount { Name = g.Key, Amount = g.Sum(t => t.Amount), Count = g.Count() }).OrderByDescending(x => x.Amount).ToList();
        var cashiers = orders.Where(o => o.PaidTotal > 0).GroupBy(o => o.Cashier?.FullName ?? "Unassigned")
            .Select(g => new NamedAmount { Name = g.Key, Amount = g.Sum(o => o.PaidTotal - o.RefundedTotal), Count = g.Count() }).OrderByDescending(x => x.Amount).ToList();

        return new OverviewDto
        {
            From = start,
            To = end,
            GrossSales = Money.R(gross),
            Discount = Money.R(discount),
            Tax = Money.R(tax),
            ServiceCharge = Money.R(service),
            NetSales = net,
            Refunds = Money.R(refunds),
            FoodCost = Money.R(food),
            GrossProfit = Money.R(net - food),
            Expenses = Money.R(expenses),
            WastageCost = Money.R(wastage),
            CustomerDues = await _db.Customers.Where(c => c.OrganizationId == me.OrganizationId).SumAsync(c => (decimal?)c.DueBalance) ?? 0,
            SupplierDues = await _db.Suppliers.Where(s => s.OrganizationId == me.OrganizationId).SumAsync(s => (decimal?)s.DueBalance) ?? 0,
            OrderCount = orders.Count,
            OpenOrders = await _db.SaleOrders.CountAsync(o => ids.Contains(o.BranchId) && o.Status != "Void" && (o.PaymentStatus == "Unpaid" || o.PaymentStatus == "Partial")),
            LowStockCount = await LowStock(ids, me.OrganizationId),
            PendingApprovals = await _db.ApprovalRequests.CountAsync(a => a.OrganizationId == me.OrganizationId && a.Status == "Pending"),
            ByPayment = payments,
            ByCashier = cashiers,
            BestSellers = best,
            SlowMovers = slow,
            ByBranch = branches.Select(b => new NamedAmount
            {
                Name = b.Name,
                Amount = allForCompare.Where(x => x.BranchId == b.Id).Sum(x => x.Net),
                Count = allForCompare.Count(x => x.BranchId == b.Id)
            }).ToList(),
            Daily = daily
        };
    }

    async Task<int> LowStock(List<int> branchIds, int orgId)
    {
        var ingredients = await _db.Ingredients.Where(i => i.OrganizationId == orgId && i.IsActive).Select(i => new { i.Id, i.ReorderLevel }).ToListAsync();
        var balances = await _db.StockBalances.Where(b => branchIds.Contains(b.BranchId)).Select(b => new { b.BranchId, b.IngredientId, b.Quantity }).ToListAsync();
        var count = 0;
        foreach (var branch in branchIds)
        {
            foreach (var ingredient in ingredients)
            {
                var qty = balances.FirstOrDefault(b => b.BranchId == branch && b.IngredientId == ingredient.Id)?.Quantity ?? 0;
                if (qty <= ingredient.ReorderLevel) count++;
            }
        }
        return count;
    }
}
