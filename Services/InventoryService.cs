using RestaurantPos.Data;

namespace RestaurantPos.Services;

public class InventoryService
{
    private readonly AppDbContext _db;
    private readonly Gate _gate;
    private readonly Tracker _tracker;
    private readonly Dictionary<(int Branch, int Ingredient), StockBalance> _cache = new();

    public InventoryService(AppDbContext db, Gate gate, Tracker tracker)
    {
        _db = db;
        _gate = gate;
        _tracker = tracker;
    }

    public async Task<List<IngredientDto>> Ingredients()
    {
        var me = await _gate.Me();
        me.Require("inventory.view", "purchase.manage", "pos.order");
        return await _db.Ingredients.Where(i => i.OrganizationId == me.OrganizationId)
            .OrderBy(i => i.Name)
            .Select(i => new IngredientDto
            {
                Id = i.Id, Name = i.Name, Sku = i.Sku, Unit = i.Unit, ReorderLevel = i.ReorderLevel,
                CostPerUnit = i.CostPerUnit, ExpiryTracked = i.ExpiryTracked, IsActive = i.IsActive
            }).ToListAsync();
    }

    public async Task<IngredientDto> SaveIngredient(SaveIngredientRequest req)
    {
        var me = await _gate.Me();
        me.Require("inventory.manage");
        if (string.IsNullOrWhiteSpace(req.Name)) throw new AppException("Ingredient name is required.");
        Ingredient row;
        if (req.Id is int id)
        {
            row = await _db.Ingredients.FirstOrDefaultAsync(i => i.Id == id && i.OrganizationId == me.OrganizationId)
                ?? throw new AppException("Ingredient not found.", 404);
        }
        else
        {
            row = new Ingredient { OrganizationId = me.OrganizationId };
            _db.Ingredients.Add(row);
        }
        row.Name = req.Name.Trim();
        row.Sku = NullIfBlank(req.Sku);
        row.Unit = string.IsNullOrWhiteSpace(req.Unit) ? "pcs" : req.Unit.Trim();
        row.ReorderLevel = req.ReorderLevel;
        row.CostPerUnit = req.CostPerUnit;
        row.ExpiryTracked = req.ExpiryTracked;
        row.IsActive = req.IsActive;
        _tracker.Log(me.OrganizationId, me.Id, "Save", "Ingredient", row.Id.ToString(), row.Name);
        await _db.SaveChangesAsync();
        return new IngredientDto
        {
            Id = row.Id, Name = row.Name, Sku = row.Sku, Unit = row.Unit, ReorderLevel = row.ReorderLevel,
            CostPerUnit = row.CostPerUnit, ExpiryTracked = row.ExpiryTracked, IsActive = row.IsActive
        };
    }

    public async Task<List<RecipeLineDto>> Recipe(int productId)
    {
        var me = await _gate.Me();
        me.Require("inventory.view", "menu.manage");
        var ok = await _db.Products.AnyAsync(p => p.Id == productId && p.OrganizationId == me.OrganizationId);
        if (!ok) throw new AppException("Product not found.", 404);
        return await _db.RecipeLines.Where(r => r.ProductId == productId)
            .Select(r => new RecipeLineDto
            {
                IngredientId = r.IngredientId,
                IngredientName = r.Ingredient.Name,
                Unit = r.Ingredient.Unit,
                Quantity = r.Quantity
            }).ToListAsync();
    }

    public async Task<List<RecipeLineDto>> SaveRecipe(int productId, SaveRecipeRequest req)
    {
        var me = await _gate.Me();
        me.Require("inventory.manage", "menu.manage");
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == productId && p.OrganizationId == me.OrganizationId)
            ?? throw new AppException("Product not found.", 404);
        var ids = req.Lines.Select(l => l.IngredientId).Distinct().ToList();
        var known = await _db.Ingredients.CountAsync(i => ids.Contains(i.Id) && i.OrganizationId == me.OrganizationId);
        if (known != ids.Count) throw new AppException("One or more ingredients are not in this restaurant.");
        var existing = await _db.RecipeLines.Where(r => r.ProductId == productId).ToListAsync();
        _db.RecipeLines.RemoveRange(existing);
        foreach (var line in req.Lines.Where(l => l.Quantity > 0))
        {
            _db.RecipeLines.Add(new RecipeLine { ProductId = productId, IngredientId = line.IngredientId, Quantity = Money.Q(line.Quantity) });
        }
        _tracker.Log(me.OrganizationId, me.Id, "SaveRecipe", "Product", product.Id.ToString(), product.Name);
        await _db.SaveChangesAsync();
        return await Recipe(productId);
    }

    public async Task<List<StockRowDto>> Stock(int branchId)
    {
        var (me, _) = await _gate.Branch(branchId);
        me.Require("inventory.view", "purchase.manage");
        var ingredients = await _db.Ingredients.Where(i => i.OrganizationId == me.OrganizationId && i.IsActive).OrderBy(i => i.Name).ToListAsync();
        var balances = await _db.StockBalances.Where(b => b.BranchId == branchId).ToListAsync();
        var soon = DateTime.UtcNow.Date.AddDays(7);
        return ingredients.Select(i =>
        {
            var bal = balances.FirstOrDefault(b => b.IngredientId == i.Id);
            var qty = bal?.Quantity ?? 0;
            var cost = bal?.AvgCost ?? i.CostPerUnit;
            return new StockRowDto
            {
                IngredientId = i.Id,
                Name = i.Name,
                Unit = i.Unit,
                Quantity = qty,
                AvgCost = cost,
                Value = Money.R(qty * cost),
                ReorderLevel = i.ReorderLevel,
                Low = qty <= i.ReorderLevel,
                ExpiryDate = bal?.ExpiryDate,
                Expiring = bal?.ExpiryDate != null && bal.ExpiryDate <= soon
            };
        }).ToList();
    }

    public async Task<List<LedgerDto>> Ledger(int branchId, int? ingredientId)
    {
        var (me, _) = await _gate.Branch(branchId);
        me.Require("inventory.view");
        var q = _db.StockLedgers.Include(l => l.Ingredient).Where(l => l.BranchId == branchId);
        if (ingredientId is int id) q = q.Where(l => l.IngredientId == id);
        return await q.OrderByDescending(l => l.Id).Take(300).Select(l => new LedgerDto
        {
            Id = l.Id,
            IngredientName = l.Ingredient != null ? l.Ingredient.Name : "",
            TxnType = l.TxnType,
            Quantity = l.Quantity,
            UnitCost = l.UnitCost,
            BalanceAfter = l.BalanceAfter,
            Note = l.Note,
            CreatedAt = l.CreatedAt
        }).ToListAsync();
    }

    public async Task Wastage(WastageRequest req)
    {
        var (me, _) = await _gate.Branch(req.BranchId);
        me.Require("inventory.manage");
        if (req.Quantity <= 0) throw new AppException("Wastage quantity must be greater than zero.");
        if (string.IsNullOrWhiteSpace(req.Reason)) throw new AppException("A reason is required for wastage.");
        var ingredient = await OwnIngredient(me.OrganizationId, req.IngredientId);
        var bal = await GetBalance(req.BranchId, ingredient.Id);
        var cost = Money.R(req.Quantity * (bal.AvgCost > 0 ? bal.AvgCost : ingredient.CostPerUnit));
        await Apply(req.BranchId, ingredient.Id, -Money.Q(req.Quantity), bal.AvgCost, "Wastage", "Wastage", null, req.Reason.Trim(), me.Id, false);
        var row = new Wastage
        {
            BranchId = req.BranchId,
            IngredientId = ingredient.Id,
            Quantity = Money.Q(req.Quantity),
            Cost = cost,
            Reason = req.Reason.Trim(),
            UserId = me.Id
        };
        _db.Wastages.Add(row);
        _tracker.Log(me.OrganizationId, me.Id, "Wastage", "Ingredient", ingredient.Id.ToString(), req.Reason.Trim());
        await _db.SaveChangesAsync();
    }

    public async Task<ActionResultDto> Adjust(AdjustmentRequest req, bool force = false)
    {
        var (me, _) = await _gate.Branch(req.BranchId);
        me.Require("inventory.manage");
        if (req.QtyDelta == 0) throw new AppException("Enter a quantity change.");
        if (string.IsNullOrWhiteSpace(req.Reason)) throw new AppException("A reason is required for a stock adjustment.");
        var ingredient = await OwnIngredient(me.OrganizationId, req.IngredientId);
        var limit = await Settings.Dec(_db, me.OrganizationId, SettingKey.AdjustmentLimit, 20);
        if (!force && req.QtyDelta < -limit && !me.Has("approval.review"))
        {
            var pending = new StockAdjustment
            {
                BranchId = req.BranchId,
                IngredientId = ingredient.Id,
                QtyDelta = req.QtyDelta,
                Reason = req.Reason.Trim(),
                Status = "Pending",
                UserId = me.Id
            };
            _db.StockAdjustments.Add(pending);
            await _db.SaveChangesAsync();
            await Queue(me, req.BranchId, "StockAdjustment", "adj:" + pending.Id, req.Reason.Trim(), Math.Abs(req.QtyDelta),
                "{\"adjustmentId\":" + pending.Id + "}");
            return new ActionResultDto { ApprovalRequired = true, Message = "This adjustment is waiting for a manager to approve it." };
        }

        await Apply(req.BranchId, ingredient.Id, Money.Q(req.QtyDelta), null, "Adjustment", "Adjustment", null, req.Reason.Trim(), me.Id, req.QtyDelta > 0);
        _db.StockAdjustments.Add(new StockAdjustment
        {
            BranchId = req.BranchId,
            IngredientId = ingredient.Id,
            QtyDelta = req.QtyDelta,
            Reason = req.Reason.Trim(),
            Status = "Posted",
            UserId = me.Id
        });
        _tracker.Log(me.OrganizationId, me.Id, "Adjust", "Ingredient", ingredient.Id.ToString(), req.Reason.Trim());
        await _db.SaveChangesAsync();
        return new ActionResultDto { Message = "Stock adjustment posted." };
    }

    public async Task PostApprovedAdjustment(int adjustmentId, int userId)
    {
        var row = await _db.StockAdjustments.Include(a => a.Ingredient).FirstOrDefaultAsync(a => a.Id == adjustmentId)
            ?? throw new AppException("Adjustment not found.", 404);
        if (row.Status != "Pending") throw new AppException("This adjustment is no longer pending.");
        await Apply(row.BranchId, row.IngredientId, Money.Q(row.QtyDelta), null, "Adjustment", "Adjustment", row.Id, row.Reason, userId, row.QtyDelta > 0);
        row.Status = "Posted";
    }

    public async Task Count(CountRequest req)
    {
        var (me, _) = await _gate.Branch(req.BranchId);
        me.Require("inventory.manage");
        if (req.Lines.Count == 0) throw new AppException("Add at least one counted item.");
        var count = new PhysicalCount { BranchId = req.BranchId, UserId = me.Id, Note = NullIfBlank(req.Note), Status = "Posted" };
        _db.PhysicalCounts.Add(count);
        await _db.SaveChangesAsync();
        foreach (var line in req.Lines)
        {
            var ingredient = await OwnIngredient(me.OrganizationId, line.IngredientId);
            var bal = await GetBalance(req.BranchId, ingredient.Id);
            var delta = Money.Q(line.CountedQty - bal.Quantity);
            _db.PhysicalCountLines.Add(new PhysicalCountLine
            {
                PhysicalCountId = count.Id,
                IngredientId = ingredient.Id,
                SystemQty = bal.Quantity,
                CountedQty = line.CountedQty
            });
            if (delta != 0)
                await Apply(req.BranchId, ingredient.Id, delta, bal.AvgCost, "Count", "PhysicalCount", count.Id, "Physical count", me.Id, true);
        }
        _tracker.Log(me.OrganizationId, me.Id, "PhysicalCount", "PhysicalCount", count.Id.ToString(), req.Note);
        await _db.SaveChangesAsync();
    }

    public async Task Transfer(TransferRequest req)
    {
        var me = await _gate.Me();
        me.Require("inventory.manage");
        if (req.FromBranchId == req.ToBranchId) throw new AppException("Choose two different branches.");
        await _gate.Branch(req.FromBranchId);
        await _gate.Branch(req.ToBranchId);
        if (req.Lines.Count == 0) throw new AppException("Add at least one item to transfer.");
        var transfer = new StockTransfer
        {
            OrganizationId = me.OrganizationId,
            FromBranchId = req.FromBranchId,
            ToBranchId = req.ToBranchId,
            Note = NullIfBlank(req.Note),
            UserId = me.Id
        };
        _db.StockTransfers.Add(transfer);
        await _db.SaveChangesAsync();
        foreach (var line in req.Lines)
        {
            if (line.Quantity <= 0) throw new AppException("Transfer quantity must be greater than zero.");
            var ingredient = await OwnIngredient(me.OrganizationId, line.IngredientId);
            var from = await GetBalance(req.FromBranchId, ingredient.Id);
            var qty = Money.Q(line.Quantity);
            await Apply(req.FromBranchId, ingredient.Id, -qty, from.AvgCost, "TransferOut", "Transfer", transfer.Id, req.Note, me.Id, false);
            await Apply(req.ToBranchId, ingredient.Id, qty, from.AvgCost, "TransferIn", "Transfer", transfer.Id, req.Note, me.Id, true);
            _db.StockTransferLines.Add(new StockTransferLine { StockTransferId = transfer.Id, IngredientId = ingredient.Id, Quantity = qty });
        }
        _tracker.Log(me.OrganizationId, me.Id, "Transfer", "StockTransfer", transfer.Id.ToString(), req.Note);
        await _db.SaveChangesAsync();
    }

    public async Task<List<SupplierDto>> Suppliers()
    {
        var me = await _gate.Me();
        me.Require("purchase.manage", "reports.view");
        return await _db.Suppliers.Where(s => s.OrganizationId == me.OrganizationId).OrderBy(s => s.Name)
            .Select(s => new SupplierDto { Id = s.Id, Name = s.Name, Phone = s.Phone, Email = s.Email, Address = s.Address, DueBalance = s.DueBalance, IsActive = s.IsActive })
            .ToListAsync();
    }

    public async Task<SupplierDto> SaveSupplier(SaveSupplierRequest req)
    {
        var me = await _gate.Me();
        me.Require("purchase.manage");
        if (string.IsNullOrWhiteSpace(req.Name)) throw new AppException("Supplier name is required.");
        Supplier row;
        if (req.Id is int id)
            row = await _db.Suppliers.FirstOrDefaultAsync(s => s.Id == id && s.OrganizationId == me.OrganizationId) ?? throw new AppException("Supplier not found.", 404);
        else
        {
            row = new Supplier { OrganizationId = me.OrganizationId };
            _db.Suppliers.Add(row);
        }
        row.Name = req.Name.Trim();
        row.Phone = NullIfBlank(req.Phone);
        row.Email = NullIfBlank(req.Email);
        row.Address = NullIfBlank(req.Address);
        row.IsActive = req.IsActive;
        await _db.SaveChangesAsync();
        return new SupplierDto { Id = row.Id, Name = row.Name, Phone = row.Phone, Email = row.Email, Address = row.Address, DueBalance = row.DueBalance, IsActive = row.IsActive };
    }

    public async Task PaySupplier(int id, SettleRequest req)
    {
        var me = await _gate.Me();
        me.Require("purchase.manage");
        var supplier = await _db.Suppliers.FirstOrDefaultAsync(s => s.Id == id && s.OrganizationId == me.OrganizationId)
            ?? throw new AppException("Supplier not found.", 404);
        if (req.Amount <= 0) throw new AppException("Payment amount must be greater than zero.");
        if (req.Amount > supplier.DueBalance + 0.009m) throw new AppException("Amount is more than the supplier due.");
        supplier.DueBalance = Money.R(supplier.DueBalance - req.Amount);
        _db.SupplierPayments.Add(new SupplierPayment { SupplierId = supplier.Id, Amount = Money.R(req.Amount), Method = Pricing.Method(req.Method), Note = NullIfBlank(req.Note), UserId = me.Id });
        _tracker.Log(me.OrganizationId, me.Id, "SupplierPayment", "Supplier", supplier.Id.ToString(), req.Amount.ToString("0.00"));
        await _db.SaveChangesAsync();
    }

    public async Task<List<PurchaseOrderDto>> PurchaseOrders(int? branchId)
    {
        var me = await _gate.Me();
        me.Require("purchase.manage");
        var ids = await _gate.BranchIds();
        var q = _db.PurchaseOrders.Include(p => p.Branch).Include(p => p.Supplier).Include(p => p.Lines).ThenInclude(l => l.Ingredient)
            .Where(p => p.OrganizationId == me.OrganizationId && ids.Contains(p.BranchId));
        if (branchId is int b) q = q.Where(p => p.BranchId == b);
        var rows = await q.OrderByDescending(p => p.Id).Take(100).ToListAsync();
        return rows.Select(MapPo).ToList();
    }

    public async Task<PurchaseOrderDto> CreatePo(SavePoRequest req)
    {
        var (me, _) = await _gate.Branch(req.BranchId);
        me.Require("purchase.manage");
        if (req.Lines.Count == 0) throw new AppException("Add at least one purchase line.");
        var supplier = await _db.Suppliers.FirstOrDefaultAsync(s => s.Id == req.SupplierId && s.OrganizationId == me.OrganizationId && s.IsActive)
            ?? throw new AppException("Supplier not found.", 404);
        var po = new PurchaseOrder
        {
            OrganizationId = me.OrganizationId,
            BranchId = req.BranchId,
            SupplierId = supplier.Id,
            PoNo = await Sequences.Next(_db, me.OrganizationId, "po", "PO-"),
            Status = "Open",
            ExpectedDate = req.ExpectedDate,
            Notes = NullIfBlank(req.Notes),
            UserId = me.Id
        };
        foreach (var line in req.Lines)
        {
            if (line.Quantity <= 0) throw new AppException("Purchase quantity must be greater than zero.");
            await OwnIngredient(me.OrganizationId, line.IngredientId);
            po.Lines.Add(new PurchaseOrderLine { IngredientId = line.IngredientId, Quantity = Money.Q(line.Quantity), UnitCost = Money.R(line.UnitCost) });
            po.Total += Money.R(line.Quantity * line.UnitCost);
        }
        po.Total = Money.R(po.Total);
        _db.PurchaseOrders.Add(po);
        _tracker.Log(me.OrganizationId, me.Id, "CreatePO", "PurchaseOrder", null, po.PoNo);
        await _db.SaveChangesAsync();
        return await GetPo(po.Id);
    }

    public async Task<PurchaseOrderDto> GetPo(int id)
    {
        var me = await _gate.Me();
        var po = await _db.PurchaseOrders.Include(p => p.Branch).Include(p => p.Supplier).Include(p => p.Lines).ThenInclude(l => l.Ingredient)
            .FirstOrDefaultAsync(p => p.Id == id && p.OrganizationId == me.OrganizationId)
            ?? throw new AppException("Purchase order not found.", 404);
        await _gate.Branch(po.BranchId);
        return MapPo(po);
    }

    public async Task<PurchaseOrderDto> Receive(int id, ReceiveRequest req)
    {
        var me = await _gate.Me();
        me.Require("purchase.manage");
        var po = await _db.PurchaseOrders.Include(p => p.Lines).ThenInclude(l => l.Ingredient).Include(p => p.Supplier)
            .FirstOrDefaultAsync(p => p.Id == id && p.OrganizationId == me.OrganizationId)
            ?? throw new AppException("Purchase order not found.", 404);
        await _gate.Branch(po.BranchId);
        if (po.Status is "Received" or "Cancelled") throw new AppException("This purchase order is closed.");
        if (req.Lines.Count == 0) throw new AppException("Enter the quantities you received.");
        var grn = new GoodsReceipt
        {
            PurchaseOrderId = po.Id,
            GrnNo = await Sequences.Next(_db, me.OrganizationId, "grn", "GRN-"),
            UserId = me.Id,
            Note = NullIfBlank(req.Note)
        };
        foreach (var line in req.Lines.Where(l => l.Quantity > 0))
        {
            var poLine = po.Lines.FirstOrDefault(l => l.Id == line.PoLineId) ?? throw new AppException("Purchase line not found.");
            var remaining = poLine.Quantity - poLine.ReceivedQty;
            if (line.Quantity > remaining + 0.0001m) throw new AppException($"Received quantity is more than ordered for {poLine.Ingredient.Name}.");
            var qty = Money.Q(line.Quantity);
            poLine.ReceivedQty = Money.Q(poLine.ReceivedQty + qty);
            var cost = poLine.UnitCost;
            await Apply(po.BranchId, poLine.IngredientId, qty, cost, "Purchase", "GoodsReceipt", null, po.PoNo, me.Id, true);
            if (line.ExpiryDate != null)
            {
                var bal = await GetBalance(po.BranchId, poLine.IngredientId);
                if (bal.ExpiryDate == null || line.ExpiryDate < bal.ExpiryDate) bal.ExpiryDate = line.ExpiryDate;
            }
            var amount = Money.R(qty * cost);
            grn.Total += amount;
            po.Supplier.DueBalance = Money.R(po.Supplier.DueBalance + amount);
            grn.Lines.Add(new GoodsReceiptLine
            {
                PurchaseOrderLineId = poLine.Id,
                IngredientId = poLine.IngredientId,
                Quantity = qty,
                UnitCost = cost,
                ExpiryDate = line.ExpiryDate
            });
        }
        if (grn.Lines.Count == 0) throw new AppException("Enter the quantities you received.");
        po.Status = po.Lines.All(l => l.ReceivedQty + 0.0001m >= l.Quantity) ? "Received" : "Partial";
        _db.GoodsReceipts.Add(grn);
        _tracker.Log(me.OrganizationId, me.Id, "Receive", "PurchaseOrder", po.Id.ToString(), grn.GrnNo);
        await _db.SaveChangesAsync();
        return await GetPo(po.Id);
    }

    public async Task ConsumeLine(SaleOrder order, SaleLine line, int userId)
    {
        if (line.StockConsumed || line.IsVoid) return;
        var recipes = await _db.RecipeLines.Where(r => r.ProductId == line.ProductId).ToListAsync();
        decimal cost = 0;
        if (recipes.Count == 0)
        {
            var product = await _db.Products.FirstAsync(p => p.Id == line.ProductId);
            cost = Money.R(product.CostPrice * line.Qty);
        }
        else
        {
            foreach (var recipe in recipes)
            {
                var need = Money.Q(recipe.Quantity * line.Qty);
                var bal = await GetBalance(order.BranchId, recipe.IngredientId);
                var unit = bal.AvgCost > 0 ? bal.AvgCost : 0;
                cost += need * unit;
                await Apply(order.BranchId, recipe.IngredientId, -need, unit, "Consume", "SaleOrder", order.Id, line.Name, userId, true);
            }
            cost = Money.R(cost);
        }
        line.FoodCost = cost;
        line.StockConsumed = true;
        order.FoodCost = Money.R(order.Lines.Sum(l => l.FoodCost));
    }

    public async Task RestoreLine(SaleOrder order, SaleLine line, decimal ratio, int userId)
    {
        if (!line.StockConsumed || ratio <= 0) return;
        ratio = Math.Min(1, ratio);
        var recipes = await _db.RecipeLines.Where(r => r.ProductId == line.ProductId).ToListAsync();
        foreach (var recipe in recipes)
        {
            var qty = Money.Q(recipe.Quantity * line.Qty * ratio);
            if (qty <= 0) continue;
            var bal = await GetBalance(order.BranchId, recipe.IngredientId);
            await Apply(order.BranchId, recipe.IngredientId, qty, bal.AvgCost, "Refund", "SaleOrder", order.Id, line.Name, userId, true);
        }
        var cut = Money.R(line.FoodCost * ratio);
        line.FoodCost = Money.R(line.FoodCost - cut);
        if (ratio >= 0.999m)
        {
            line.StockConsumed = false;
            line.FoodCost = 0;
        }
        order.FoodCost = Money.R(order.Lines.Where(l => !l.IsVoid).Sum(l => l.FoodCost));
    }

    async Task<StockBalance> GetBalance(int branchId, int ingredientId)
    {
        if (_cache.TryGetValue((branchId, ingredientId), out var hit)) return hit;
        var bal = await _db.StockBalances.FirstOrDefaultAsync(b => b.BranchId == branchId && b.IngredientId == ingredientId);
        if (bal == null)
        {
            bal = new StockBalance { BranchId = branchId, IngredientId = ingredientId };
            _db.StockBalances.Add(bal);
        }
        _cache[(branchId, ingredientId)] = bal;
        return bal;
    }

    async Task Apply(int branchId, int ingredientId, decimal signedQty, decimal? unitCost, string txnType, string? refType, int? refId, string? note, int userId, bool allowNegative)
    {
        var bal = await GetBalance(branchId, ingredientId);
        if (!allowNegative && bal.Quantity + signedQty < -0.0001m)
            throw new AppException("Not enough stock for this movement.");
        if (signedQty > 0)
        {
            var cost = unitCost ?? bal.AvgCost;
            var oldQty = Math.Max(bal.Quantity, 0);
            var newQty = oldQty + signedQty;
            bal.AvgCost = newQty <= 0 ? cost : Money.R((oldQty * bal.AvgCost + signedQty * cost) / newQty);
        }
        bal.Quantity = Money.Q(bal.Quantity + signedQty);
        _db.StockLedgers.Add(new StockLedger
        {
            BranchId = branchId,
            IngredientId = ingredientId,
            TxnType = txnType,
            Quantity = signedQty,
            UnitCost = unitCost ?? bal.AvgCost,
            BalanceAfter = bal.Quantity,
            RefType = refType,
            RefId = refId,
            Note = note,
            UserId = userId
        });
    }

    async Task<Ingredient> OwnIngredient(int orgId, int id) =>
        await _db.Ingredients.FirstOrDefaultAsync(i => i.Id == id && i.OrganizationId == orgId)
        ?? throw new AppException("Ingredient not found.", 404);

    async Task Queue(UserPrincipal me, int? branchId, string type, string entityKey, string reason, decimal amount, string payload)
    {
        var exists = await _db.ApprovalRequests.AnyAsync(a => a.OrganizationId == me.OrganizationId && a.Status == "Pending" && a.Type == type && a.EntityKey == entityKey);
        if (exists) throw new AppException("An approval for this change is already waiting.");
        _db.ApprovalRequests.Add(new ApprovalRequest
        {
            OrganizationId = me.OrganizationId,
            BranchId = branchId,
            Type = type,
            EntityKey = entityKey,
            PayloadJson = payload,
            Reason = reason,
            Amount = amount,
            RequestedById = me.Id
        });
        _tracker.Log(me.OrganizationId, me.Id, "RequestApproval", type, entityKey, reason);
        await _db.SaveChangesAsync();
    }

    static PurchaseOrderDto MapPo(PurchaseOrder po) => new()
    {
        Id = po.Id,
        PoNo = po.PoNo,
        BranchId = po.BranchId,
        BranchName = po.Branch?.Name ?? "",
        SupplierId = po.SupplierId,
        SupplierName = po.Supplier?.Name ?? "",
        Status = po.Status,
        OrderDate = po.OrderDate,
        ExpectedDate = po.ExpectedDate,
        Total = po.Total,
        Notes = po.Notes,
        Lines = po.Lines.Select(l => new PoLineDto
        {
            Id = l.Id,
            IngredientId = l.IngredientId,
            IngredientName = l.Ingredient?.Name ?? "",
            Unit = l.Ingredient?.Unit ?? "",
            Quantity = l.Quantity,
            UnitCost = l.UnitCost,
            ReceivedQty = l.ReceivedQty
        }).ToList()
    };

    static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
