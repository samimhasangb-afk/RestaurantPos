using System.Net;
using System.Net.Mail;
using System.Text.Json;
using QRCoder;
using RestaurantPos.Data;

namespace RestaurantPos.Services;

public class SalesService
{
    private readonly AppDbContext _db;
    private readonly Gate _gate;
    private readonly Tracker _tracker;
    private readonly InventoryService _inventory;

    public SalesService(AppDbContext db, Gate gate, Tracker tracker, InventoryService inventory)
    {
        _db = db;
        _gate = gate;
        _tracker = tracker;
        _inventory = inventory;
    }

    public async Task<OrderDto> Create(CreateOrderRequest req)
    {
        var (me, branch) = await _gate.Branch(req.BranchId);
        me.Require("pos.order");
        var type = Pricing.OrderType(req.OrderType);
        if (req.ReuseTable && req.TableId is int tableId)
        {
            var existing = await _db.SaleOrders.Where(o => o.TableId == tableId && o.BranchId == branch.Id && o.Status != "Void" && (o.PaymentStatus == "Unpaid" || o.PaymentStatus == "Partial"))
                .OrderByDescending(o => o.Id).FirstOrDefaultAsync();
            if (existing != null) return Map.Order(await Load(existing.Id, me.OrganizationId));
        }

        var order = new SaleOrder
        {
            OrganizationId = me.OrganizationId,
            BranchId = branch.Id,
            TerminalId = req.TerminalId,
            OrderNo = await Sequences.Next(_db, me.OrganizationId, "order-" + branch.Id, branch.Code + "-"),
            OrderType = type,
            WaiterId = req.WaiterId ?? (me.Role == "Waiter" ? me.Id : null),
            CustomerId = req.CustomerId,
            Notes = Clean(req.Notes),
            DeliveryAddress = Clean(req.DeliveryAddress),
            DeliveryPhone = Clean(req.DeliveryPhone),
            DeliveryStatus = type is "Delivery" or "Online" ? "Pending" : "None",
            CashierId = me.Has("pos.pay") ? me.Id : null
        };
        if (req.TableId is int tid)
        {
            var table = await _db.DiningTables.FirstOrDefaultAsync(t => t.Id == tid && t.BranchId == branch.Id)
                ?? throw new AppException("Table not found.", 404);
            order.TableId = table.Id;
        }
        var shift = await OpenShiftFor(branch.Id, req.TerminalId);
        if (shift != null) order.ShiftId = shift.Id;
        _db.SaleOrders.Add(order);
        _tracker.Log(me.OrganizationId, me.Id, "CreateOrder", "SaleOrder", null, order.OrderNo);
        await _db.SaveChangesAsync();
        await SyncTable(order.TableId);
        await _db.SaveChangesAsync();
        return Map.Order(await Load(order.Id, me.OrganizationId));
    }

    public async Task<OrderDto> Get(int id)
    {
        var me = await _gate.Me();
        me.Require("pos.order", "kitchen.view", "reports.view");
        var order = await Load(id, me.OrganizationId);
        await _gate.Branch(order.BranchId);
        return Map.Order(order);
    }

    public async Task<List<OrderSummaryDto>> List(int branchId, string? status, string? q, DateOnly? from, DateOnly? to)
    {
        var (me, _) = await _gate.Branch(branchId);
        me.Require("pos.order", "kitchen.view", "reports.view", "delivery.manage");
        var query = _db.SaleOrders.Include(o => o.Branch).Include(o => o.Table).Include(o => o.Customer).Include(o => o.Waiter).Include(o => o.Cashier)
            .Where(o => o.BranchId == branchId);
        if (from is DateOnly f) query = query.Where(o => o.CreatedAt >= Clock.BdDateStartUtc(f));
        if (to is DateOnly t) query = query.Where(o => o.CreatedAt < Clock.BdDateStartUtc(t.AddDays(1)));
        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("All", StringComparison.OrdinalIgnoreCase))
            query = query.Where(o => o.Status == status || o.PaymentStatus == status || o.KitchenStatus == status || o.DeliveryStatus == status);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(o => o.OrderNo.Contains(term) || (o.Customer != null && o.Customer.Name.Contains(term)) || (o.DeliveryPhone != null && o.DeliveryPhone.Contains(term)));
        }
        var rows = await query.OrderByDescending(o => o.Id).Take(200).ToListAsync();
        return rows.Select(Map.Summary).ToList();
    }

    public async Task<OrderDto> Update(int id, UpdateOrderRequest req)
    {
        var me = await _gate.Me();
        me.Require("pos.order", "delivery.manage");
        var order = await Load(id, me.OrganizationId);
        EnsureEditable(order);
        var previousTable = order.TableId;
        if (!string.IsNullOrWhiteSpace(req.OrderType)) order.OrderType = Pricing.OrderType(req.OrderType);
        if (req.ClearTable) order.TableId = null;
        else if (req.TableId is int tid)
        {
            var table = await _db.DiningTables.FirstOrDefaultAsync(t => t.Id == tid && t.BranchId == order.BranchId) ?? throw new AppException("Table not found.", 404);
            order.TableId = table.Id;
        }
        if (req.WaiterId is int wid) order.WaiterId = wid;
        if (req.ClearCustomer) order.CustomerId = null;
        else if (req.CustomerId is int cid) order.CustomerId = cid;
        if (req.Notes != null) order.Notes = Clean(req.Notes);
        if (req.DeliveryAddress != null) order.DeliveryAddress = Clean(req.DeliveryAddress);
        if (req.DeliveryPhone != null) order.DeliveryPhone = Clean(req.DeliveryPhone);
        if (req.RiderId is int rid) order.RiderId = rid;
        if (!string.IsNullOrWhiteSpace(req.DeliveryStatus)) order.DeliveryStatus = req.DeliveryStatus.Trim();
        if (order.OrderType is "Delivery" or "Online" && order.DeliveryStatus == "None") order.DeliveryStatus = "Pending";
        await Recalc(order);
        await SyncTable(previousTable);
        await SyncTable(order.TableId);
        await _db.SaveChangesAsync();
        return Map.Order(await Load(order.Id, me.OrganizationId));
    }

    public async Task<OrderDto> AddLine(int id, LineRequest req) 
    {
        var me = await _gate.Me();
        me.Require("pos.order");
        var order = await Load(id, me.OrganizationId);
        EnsureEditable(order);
        var line = await BuildLine(order, req);
        order.Lines.Add(line);
        _db.SaleLines.Add(line);
        await Recalc(order);
        await _db.SaveChangesAsync();
        return Map.Order(await Load(order.Id, me.OrganizationId));
    }

    public async Task<OrderDto> UpdateLine(int id, int lineId, LineRequest req)
    {
        var me = await _gate.Me();
        me.Require("pos.order");
        var order = await Load(id, me.OrganizationId);
        EnsureEditable(order);
        var line = order.Lines.FirstOrDefault(l => l.Id == lineId) ?? throw new AppException("Item not found.", 404);
        if (line.SentToKitchen) throw new AppException("This item is already in the kitchen. Void it and add a new one.");
        if (line.IsVoid) throw new AppException("This item is void.");
        if (req.Qty <= 0) throw new AppException("Quantity must be greater than zero.");
        if (req.ModifierOptionIds.Count == 0 && req.AddonIds.Count == 0)
        {
            line.Qty = Money.Q(req.Qty);
            line.DiscountAmount = Money.R(Math.Max(0, req.DiscountAmount));
            line.Course = Pricing.Course(req.Course);
            line.Notes = Clean(req.Notes);
            await Recalc(order);
            await _db.SaveChangesAsync();
            return Map.Order(await Load(order.Id, me.OrganizationId));
        }
        _db.SaleLineExtras.RemoveRange(line.Extras);
        line.Extras.Clear();
        var built = await BuildLine(order, req);
        line.ProductId = built.ProductId;
        line.Name = built.Name;
        line.Qty = built.Qty;
        line.UnitPrice = built.UnitPrice;
        line.DiscountAmount = built.DiscountAmount;
        line.Course = built.Course;
        line.Notes = built.Notes;
        line.Extras = built.Extras;
        await Recalc(order);
        await _db.SaveChangesAsync();
        return Map.Order(await Load(order.Id, me.OrganizationId));
    }

    public async Task<OrderDto> RemoveLine(int id, int lineId)
    {
        var me = await _gate.Me();
        me.Require("pos.order");
        var order = await Load(id, me.OrganizationId);
        EnsureEditable(order);
        var line = order.Lines.FirstOrDefault(l => l.Id == lineId) ?? throw new AppException("Item not found.", 404);
        if (line.SentToKitchen || line.StockConsumed)
        {
            line.IsVoid = true;
            if (line.StockConsumed) await _inventory.RestoreLine(order, line, 1, me.Id);
        }
        else
        {
            _db.SaleLineExtras.RemoveRange(line.Extras);
            _db.SaleLines.Remove(line);
            order.Lines.Remove(line);
        }
        await Recalc(order);
        await _db.SaveChangesAsync();
        return Map.Order(await Load(order.Id, me.OrganizationId));
    }

    public async Task<OrderDto> Coupon(int id, string? code)
    {
        var me = await _gate.Me();
        me.Require("pos.order", "pos.discount");
        var order = await Load(id, me.OrganizationId);
        EnsureEditable(order);
        if (string.IsNullOrWhiteSpace(code)) order.CouponCode = null;
        else
        {
            var coupon = await _db.Coupons.FirstOrDefaultAsync(c => c.OrganizationId == me.OrganizationId && c.Code == code.Trim().ToUpperInvariant() && c.IsActive)
                ?? throw new AppException("Coupon was not found.");
            if (coupon.ExpiresOn != null && coupon.ExpiresOn < DateTime.UtcNow) throw new AppException("This coupon has expired.");
            order.CouponCode = coupon.Code;
        }
        await Recalc(order);
        await _db.SaveChangesAsync();
        return Map.Order(await Load(order.Id, me.OrganizationId));
    }

    public async Task<ActionResultDto> Discount(int id, decimal amount, bool force = false)
    {
        var me = await _gate.Me();
        if (!force) me.Require("pos.discount");
        var order = await Load(id, me.OrganizationId);
        EnsureEditable(order);
        if (amount < 0) throw new AppException("Discount cannot be negative.");
        var limit = await Settings.Dec(_db, me.OrganizationId, SettingKey.DiscountLimit, 200);
        if (!force && amount > limit && !me.Has("pos.discount.override"))
        {
            await Queue(me, order.BranchId, "Discount", "order:" + order.Id, "Discount " + amount.ToString("0.00"), amount,
                JsonSerializer.Serialize(new { orderId = order.Id, amount }));
            return new ActionResultDto { ApprovalRequired = true, Message = "This discount needs a manager's approval.", Order = Map.Order(order) };
        }
        order.ManualDiscount = Money.R(amount);
        await Recalc(order);
        _tracker.Log(me.OrganizationId, me.Id, "Discount", "SaleOrder", order.Id.ToString(), amount.ToString("0.00"));
        await _db.SaveChangesAsync();
        return new ActionResultDto { Message = "Discount applied.", Order = Map.Order(await Load(order.Id, me.OrganizationId)) };
    }

    public async Task<OrderDto> Hold(int id)
    {
        var me = await _gate.Me();
        me.Require("pos.order");
        var order = await Load(id, me.OrganizationId);
        if (order.Status == "Void") throw new AppException("This order is void.");
        if (order.PaymentStatus is "Paid" or "Refunded") throw new AppException("A settled order cannot be held.");
        order.Status = "Held";
        _tracker.Log(me.OrganizationId, me.Id, "Hold", "SaleOrder", order.Id.ToString(), order.OrderNo);
        await _db.SaveChangesAsync();
        return Map.Order(order);
    }

    public async Task<OrderDto> Resume(int id)
    {
        var me = await _gate.Me();
        me.Require("pos.order");
        var order = await Load(id, me.OrganizationId);
        if (order.Status != "Held") throw new AppException("Only a held order can be resumed.");
        order.Status = "Open";
        await _db.SaveChangesAsync();
        return Map.Order(order);
    }

    public async Task<OrderDto> SendKitchen(int id)
    {
        var me = await _gate.Me();
        me.Require("pos.order", "kitchen.update");
        var order = await Load(id, me.OrganizationId);
        if (order.Status == "Void") throw new AppException("This order is void.");
        var pending = order.Lines.Where(l => !l.IsVoid && !l.SentToKitchen).ToList();
        if (pending.Count == 0) throw new AppException("There are no new items to send.");
        foreach (var group in pending.GroupBy(l => l.Course))
        {
            var ticket = new KitchenTicket
            {
                SaleOrderId = order.Id,
                TicketNo = await Sequences.Next(_db, me.OrganizationId, "kot", "KOT-"),
                Course = group.Key,
                Status = "Queued"
            };
            foreach (var line in group)
            {
                line.SentToKitchen = true;
                if (line.PrepStatus == "Pending") line.PrepStatus = "Pending";
                await _inventory.ConsumeLine(order, line, me.Id);
                ticket.Lines.Add(new KitchenTicketLine
                {
                    SourceLineId = line.Id,
                    Name = line.Name,
                    Qty = line.Qty,
                    Notes = JoinNotes(line),
                    PrepStatus = "Pending"
                });
            }
            _db.KitchenTickets.Add(ticket);
        }
        Pricing.RefreshKitchen(order);
        if (order.Status == "Held") order.Status = "Open";
        _tracker.Log(me.OrganizationId, me.Id, "KOT", "SaleOrder", order.Id.ToString(), order.OrderNo);
        await _db.SaveChangesAsync();
        return Map.Order(await Load(order.Id, me.OrganizationId));
    }

    public async Task<List<KitchenCardDto>> Kitchen(int branchId)
    {
        var (me, _) = await _gate.Branch(branchId);
        me.Require("kitchen.view", "pos.order");
        var since = DateTime.UtcNow.AddHours(-18);
        var tickets = await _db.KitchenTickets.Include(t => t.Lines).Include(t => t.SaleOrder).ThenInclude(o => o.Table)
            .Where(t => t.SaleOrder.BranchId == branchId && t.SaleOrder.Status != "Void" && t.CreatedAt >= since && t.Status != "Served")
            .OrderBy(t => t.Id).ToListAsync();
        return tickets.Select(t => new KitchenCardDto
        {
            TicketId = t.Id,
            TicketNo = t.TicketNo,
            Course = t.Course,
            Status = t.Status,
            OrderNo = t.SaleOrder.OrderNo,
            OrderType = t.SaleOrder.OrderType,
            TableName = t.SaleOrder.Table?.Name,
            CreatedAt = t.CreatedAt,
            OrderId = t.SaleOrderId,
            Lines = t.Lines.Select(l => new KitchenLineDto
            {
                SaleLineId = l.SourceLineId,
                Name = l.Name,
                Qty = l.Qty,
                Notes = l.Notes,
                PrepStatus = l.PrepStatus
            }).ToList()
        }).ToList();
    }

    public async Task<OrderDto> SetPrep(int lineId, string status)
    {
        var me = await _gate.Me();
        me.Require("kitchen.update");
        var match = Pricing.PrepStates.FirstOrDefault(s => s.Equals(status, StringComparison.OrdinalIgnoreCase))
            ?? throw new AppException("Status must be Pending, Preparing, Ready, or Served.");
        var line = await _db.SaleLines.Include(l => l.SaleOrder).FirstOrDefaultAsync(l => l.Id == lineId)
            ?? throw new AppException("Item not found.", 404);
        if (line.SaleOrder.OrganizationId != me.OrganizationId) throw new AppException("Item not found.", 404);
        line.PrepStatus = match;
        var ticketLines = await _db.KitchenTicketLines.Include(l => l.KitchenTicket).Where(l => l.SourceLineId == lineId).ToListAsync();
        foreach (var ticketLine in ticketLines) ticketLine.PrepStatus = match;
        foreach (var ticket in ticketLines.Select(l => l.KitchenTicket).Distinct())
        {
            var states = ticket.Lines.Select(l => l.Id == 0 ? match : l.PrepStatus).ToList();
            ticket.Status = states.All(s => s == "Served") ? "Served" : states.All(s => s is "Ready" or "Served") ? "Ready" : states.Any(s => s == "Preparing") ? "Preparing" : "Queued";
        }
        var order = await Load(line.SaleOrderId, me.OrganizationId);
        var live = order.Lines.First(l => l.Id == lineId);
        live.PrepStatus = match;
        Pricing.RefreshKitchen(order);
        await _db.SaveChangesAsync();
        return Map.Order(order);
    }

    public async Task<PayResultDto> Pay(int id, PayRequest req)
    {
        var me = await _gate.Me();
        me.Require("pos.pay");
        var order = await Load(id, me.OrganizationId);
        if (order.Status == "Void") throw new AppException("This order is void.");
        if (order.Status == "Held") order.Status = "Open";
        if (!order.Lines.Any(l => !l.IsVoid)) throw new AppException("Add items before taking payment.");
        var shift = await OpenShiftFor(order.BranchId, order.TerminalId);
        if (shift == null) throw new AppException("Open a cashier shift on this terminal before taking payment.");
        order.ShiftId ??= shift.Id;
        var method = Pricing.Method(req.Method);
        if (req.CustomerId is int cid) order.CustomerId = cid;
        await Recalc(order);
        var balance = Money.R(order.GrandTotal - order.PaidTotal);
        if (balance <= 0) throw new AppException("This order is already paid.");
        var apply = req.Amount <= 0 ? balance : Money.R(req.Amount);
        if (apply <= 0) throw new AppException("Enter a payment amount.");
        if (apply - balance > 0.009m) throw new AppException("Amount is more than the balance due.");
        decimal change = 0;
        var tendered = req.Tendered > 0 ? Money.R(req.Tendered) : apply;
        if (method == "Cash")
        {
            if (tendered + 0.001m < apply) throw new AppException("Cash received is less than the payment.");
            change = Money.R(tendered - apply);
        }
        if (method == "Due")
        {
            if (order.CustomerId == null) throw new AppException("Select a customer for a due payment.");
            var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == order.CustomerId && c.OrganizationId == me.OrganizationId)
                ?? throw new AppException("Customer not found.", 404);
            customer.DueBalance = Money.R(customer.DueBalance + apply);
        }
        _db.Tenders.Add(new Tender
        {
            SaleOrderId = order.Id,
            Method = method,
            Amount = apply,
            Tendered = tendered,
            ChangeDue = change,
            Reference = Clean(req.Reference),
            UserId = me.Id
        });
        order.PaidTotal = Money.R(order.PaidTotal + apply);
        order.CashierId = me.Id;
        Pricing.RefreshPayment(order);
        foreach (var line in order.Lines.Where(l => !l.IsVoid && !l.StockConsumed))
            await _inventory.ConsumeLine(order, line, me.Id);
        await EnsureReceipt(order, me.OrganizationId);
        await SyncTable(order.TableId);
        _tracker.Log(me.OrganizationId, me.Id, "Pay", "SaleOrder", order.Id.ToString(), method + " " + apply.ToString("0.00"));
        await _db.SaveChangesAsync();
        var dto = Map.Order(await Load(order.Id, me.OrganizationId));
        return new PayResultDto { Order = dto, ChangeDue = change, Message = change > 0 ? "Change due " + change.ToString("0.00") : "Payment recorded." };
    }

    public async Task<ActionResultDto> Refund(int id, RefundRequest req, bool force = false)
    {
        var me = await _gate.Me();
        if (!force) me.Require("pos.refund");
        var order = await Load(id, me.OrganizationId);
        if (order.Status == "Void") throw new AppException("This order is void.");
        if (req.Amount <= 0) throw new AppException("Enter a refund amount.");
        if (string.IsNullOrWhiteSpace(req.Reason)) throw new AppException("A refund reason is required.");
        var refundable = Money.R(order.PaidTotal - order.RefundedTotal);
        if (req.Amount - refundable > 0.009m) throw new AppException("Refund is more than the amount collected.");
        var limit = await Settings.Dec(_db, me.OrganizationId, SettingKey.RefundLimit, 300);
        if (!force && req.Amount > limit && !me.Has("pos.refund.override"))
        {
            await Queue(me, order.BranchId, "Refund", "refund:" + order.Id, req.Reason.Trim(), req.Amount,
                JsonSerializer.Serialize(new { orderId = order.Id, amount = req.Amount, reason = req.Reason.Trim(), method = req.Method, restock = req.Restock }));
            return new ActionResultDto { ApprovalRequired = true, Message = "This refund needs a manager's approval.", Order = Map.Order(order) };
        }
        var method = Pricing.Method(req.Method);
        var amount = Money.R(req.Amount);
        _db.Tenders.Add(new Tender { SaleOrderId = order.Id, Method = method, Amount = amount, Tendered = amount, IsRefund = true, Reference = req.Reason.Trim(), UserId = me.Id });
        order.RefundedTotal = Money.R(order.RefundedTotal + amount);
        if (method == "Due" && order.CustomerId is int cid)
        {
            var customer = await _db.Customers.FirstAsync(c => c.Id == cid);
            customer.DueBalance = Money.R(Math.Max(0, customer.DueBalance - amount));
        }
        if (req.Restock && order.GrandTotal > 0)
        {
            var ratio = Math.Min(1, amount / order.GrandTotal);
            foreach (var line in order.Lines.Where(l => l.StockConsumed && !l.IsVoid))
                await _inventory.RestoreLine(order, line, ratio, me.Id);
        }
        Pricing.RefreshPayment(order);
        await SyncTable(order.TableId);
        var receipt = order.Receipts.FirstOrDefault() ?? await EnsureReceipt(order, me.OrganizationId);
        _db.ReceiptEvents.Add(new ReceiptEvent { ReceiptId = receipt.Id, Action = "Refund", Note = req.Reason.Trim(), UserId = me.Id });
        _tracker.Log(me.OrganizationId, me.Id, "Refund", "SaleOrder", order.Id.ToString(), amount.ToString("0.00"));
        await _db.SaveChangesAsync();
        return new ActionResultDto { Message = "Refund recorded.", Order = Map.Order(await Load(order.Id, me.OrganizationId)) };
    }

    public async Task<ActionResultDto> Void(int id, string reason, bool force = false)
    {
        var me = await _gate.Me();
        var order = await Load(id, me.OrganizationId);
        if (order.Status == "Void") throw new AppException("This order is already void.");
        if (string.IsNullOrWhiteSpace(reason)) throw new AppException("A void reason is required.");
        if (order.PaidTotal - order.RefundedTotal > 0.009m) throw new AppException("Refund the collected amount before voiding this order.");
        if (!force && !me.Has("pos.void"))
        {
            me.Require("pos.order");
            await Queue(me, order.BranchId, "Void", "void:" + order.Id, reason.Trim(), order.GrandTotal,
                JsonSerializer.Serialize(new { orderId = order.Id, reason = reason.Trim() }));
            return new ActionResultDto { ApprovalRequired = true, Message = "Void is waiting for approval.", Order = Map.Order(order) };
        }
        foreach (var line in order.Lines.Where(l => l.StockConsumed))
            await _inventory.RestoreLine(order, line, 1, me.Id);
        order.Status = "Void";
        order.PaymentStatus = "Void";
        await SyncTable(order.TableId);
        if (order.Receipts.FirstOrDefault() is Receipt receipt)
            _db.ReceiptEvents.Add(new ReceiptEvent { ReceiptId = receipt.Id, Action = "Void", Note = reason.Trim(), UserId = me.Id });
        _tracker.Log(me.OrganizationId, me.Id, "Void", "SaleOrder", order.Id.ToString(), reason.Trim());
        await _db.SaveChangesAsync();
        return new ActionResultDto { Message = "Order voided.", Order = Map.Order(await Load(order.Id, me.OrganizationId)) };
    }

    public async Task<string> Print(int id, int width, string apiBase)
    {
        var me = await _gate.Me();
        me.Require("pos.order", "pos.pay");
        var order = await Load(id, me.OrganizationId);
        var receipt = await EnsureReceipt(order, me.OrganizationId);
        receipt.PrintCount += 1;
        _db.ReceiptEvents.Add(new ReceiptEvent
        {
            ReceiptId = receipt.Id,
            Action = receipt.PrintCount == 1 ? "Print" : "Reprint",
            Note = width + "mm",
            UserId = me.Id
        });
        _tracker.Log(me.OrganizationId, me.Id, "Print", "Receipt", receipt.ReceiptNo, width + "mm");
        await _db.SaveChangesAsync();
        return await HtmlFor(order, receipt, width, apiBase);
    }

    public async Task<string> HtmlByToken(string token, int width, string apiBase)
    {
        var receipt = await _db.Receipts.FirstOrDefaultAsync(r => r.Token == token) ?? throw new AppException("Receipt not found.", 404);
        var order = await Load(receipt.SaleOrderId, (await _db.SaleOrders.Where(o => o.Id == receipt.SaleOrderId).Select(o => o.OrganizationId).FirstAsync()));
        return await HtmlFor(order, receipt, width, apiBase);
    }

    public async Task<VerifyDto> Verify(string token)
    {
        var receipt = await _db.Receipts.Include(r => r.SaleOrder).ThenInclude(o => o.Lines).ThenInclude(l => l.Extras)
            .Include(r => r.SaleOrder).ThenInclude(o => o.Branch)
            .FirstOrDefaultAsync(r => r.Token == token);
        if (receipt == null) return new VerifyDto { Valid = false, Message = "This receipt could not be verified." };
        var order = receipt.SaleOrder;
        return new VerifyDto
        {
            Valid = order.Status != "Void",
            Message = order.Status == "Void" ? "This receipt belongs to a voided order." : "Receipt verified.",
            ReceiptNo = receipt.ReceiptNo,
            OrderNo = order.OrderNo,
            Branch = order.Branch?.Name,
            PaymentStatus = order.PaymentStatus,
            GrandTotal = order.GrandTotal,
            CreatedAt = receipt.CreatedAt,
            PrintCount = receipt.PrintCount,
            Lines = order.Lines.Where(l => !l.IsVoid).Select(l => new LineDto { Name = l.Name, Qty = l.Qty, LineTotal = l.LineTotal }).ToList()
        };
    }

    public async Task<byte[]> Qr(string token)
    {
        var exists = await _db.Receipts.AnyAsync(r => r.Token == token);
        if (!exists) throw new AppException("Receipt not found.", 404);
        var orgId = await _db.Receipts.Where(r => r.Token == token).Select(r => r.SaleOrder.OrganizationId).FirstAsync();
        var app = await Settings.Get(_db, orgId, SettingKey.PublicApp, "http://localhost:5173");
        var url = app.TrimEnd('/') + "/#/r/" + token;
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data);
        return png.GetGraphic(8);
    }

    public async Task<DispatchDto> Dispatch(int id, DispatchRequest req)
    {
        var me = await _gate.Me();
        me.Require("pos.order", "pos.pay");
        var order = await Load(id, me.OrganizationId);
        var receipt = await EnsureReceipt(order, me.OrganizationId);
        var channel = req.Channel.Equals("Sms", StringComparison.OrdinalIgnoreCase) ? "Sms" : "Email";
        var dest = (req.Destination ?? "").Trim();
        if (dest.Length < 4) throw new AppException(channel == "Email" ? "Enter an email address." : "Enter a mobile number.");
        var body = $"Saffron receipt {receipt.ReceiptNo} for order {order.OrderNo}. Total {order.GrandTotal:0.00} BDT.";
        var status = "Queued";
        string note = body;
        if (channel == "Email")
        {
            var host = await Settings.Get(_db, me.OrganizationId, "smtp.host");
            if (!string.IsNullOrWhiteSpace(host))
            {
                try
                {
                    var port = int.TryParse(await Settings.Get(_db, me.OrganizationId, "smtp.port", "587"), out var p) ? p : 587;
                    var from = await Settings.Get(_db, me.OrganizationId, "smtp.from", "receipts@saffron.local");
                    var user = await Settings.Get(_db, me.OrganizationId, "smtp.user");
                    var pass = await Settings.Get(_db, me.OrganizationId, "smtp.pass");
#pragma warning disable SYSLIB0014
                    using var message = new MailMessage(from, dest, "Your Saffron receipt " + receipt.ReceiptNo, body);
                    using var client = new SmtpClient(host, port) { EnableSsl = true };
#pragma warning restore SYSLIB0014
                    if (!string.IsNullOrWhiteSpace(user)) client.Credentials = new NetworkCredential(user, pass);
                    await client.SendMailAsync(message);
                    status = "Sent";
                    note = "Email sent.";
                }
                catch (Exception ex)
                {
                    status = "Failed";
                    note = ex.Message;
                }
            }
            else note = "Saved for delivery. Add smtp.host in settings to send automatically. " + body;
        }
        else note = "SMS content saved in the audit log. Connect an SMS gateway to deliver it. " + body;

        var row = new ReceiptDispatch { ReceiptId = receipt.Id, Channel = channel, Destination = dest, Status = status, Note = note, UserId = me.Id };
        _db.ReceiptDispatches.Add(row);
        _db.ReceiptEvents.Add(new ReceiptEvent { ReceiptId = receipt.Id, Action = channel, Note = dest + " " + status, UserId = me.Id });
        _tracker.Log(me.OrganizationId, me.Id, channel, "Receipt", receipt.ReceiptNo, dest);
        await _db.SaveChangesAsync();
        return new DispatchDto { Channel = row.Channel, Destination = row.Destination, Status = row.Status, Note = row.Note, CreatedAt = row.CreatedAt };
    }

    public async Task<ShiftDto?> CurrentShift(int branchId, int? terminalId)
    {
        var (me, _) = await _gate.Branch(branchId);
        var shift = await OpenShiftFor(branchId, terminalId);
        return shift == null ? null : Map.Shift(shift);
    }

    public async Task<ShiftDto> OpenShift(OpenShiftRequest req)
    {
        var (me, _) = await _gate.Branch(req.BranchId);
        me.Require("pos.shift");
        var terminal = await _db.Terminals.FirstOrDefaultAsync(t => t.Id == req.TerminalId && t.BranchId == req.BranchId && t.IsActive)
            ?? throw new AppException("Terminal not found.", 404);
        var existing = await _db.Shifts.FirstOrDefaultAsync(s => s.TerminalId == terminal.Id && s.Status == "Open");
        if (existing != null) throw new AppException("This terminal already has an open shift.");
        var shift = new Shift { BranchId = req.BranchId, TerminalId = terminal.Id, UserId = me.Id, OpeningCash = Money.R(req.OpeningCash), Status = "Open" };
        _db.Shifts.Add(shift);
        _tracker.Log(me.OrganizationId, me.Id, "OpenShift", "Shift", terminal.Name, req.OpeningCash.ToString("0.00"));
        await _db.SaveChangesAsync();
        shift.Terminal = terminal;
        shift.User = await _db.Users.FirstAsync(u => u.Id == me.Id);
        return Map.Shift(shift);
    }

    public async Task<ShiftDto> CloseShift(int id, CloseShiftRequest req)
    {
        var me = await _gate.Me();
        me.Require("pos.shift");
        var shift = await _db.Shifts.Include(s => s.Terminal).Include(s => s.User).FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new AppException("Shift not found.", 404);
        await _gate.Branch(shift.BranchId);
        if (shift.Status != "Open") throw new AppException("This shift is already closed.");
        var orderIds = await _db.SaleOrders.Where(o => o.ShiftId == shift.Id).Select(o => o.Id).ToListAsync();
        var tenders = await _db.Tenders.Where(t => orderIds.Contains(t.SaleOrderId) && t.Method == "Cash").ToListAsync();
        var cashIn = tenders.Where(t => !t.IsRefund).Sum(t => t.Amount);
        var cashOut = tenders.Where(t => t.IsRefund).Sum(t => t.Amount);
        shift.ExpectedCash = Money.R(shift.OpeningCash + cashIn - cashOut);
        shift.ClosingCash = Money.R(req.ClosingCash);
        shift.ClosedAt = DateTime.UtcNow;
        shift.Status = "Closed";
        shift.Note = Clean(req.Note);
        _tracker.Log(me.OrganizationId, me.Id, "CloseShift", "Shift", shift.Id.ToString(), "Expected " + shift.ExpectedCash.ToString("0.00"));
        await _db.SaveChangesAsync();
        return Map.Shift(shift);
    }

    async Task<SaleLine> BuildLine(SaleOrder order, LineRequest req)
    {
        if (req.Qty <= 0) throw new AppException("Quantity must be greater than zero.");
        var product = await _db.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == req.ProductId && p.OrganizationId == order.OrganizationId && p.IsActive)
            ?? throw new AppException("Item is not on the menu.", 404);
        var groupIds = await _db.ProductModifierGroups.Where(x => x.ProductId == product.Id).Select(x => x.ModifierGroupId).ToListAsync();
        var groups = await _db.ModifierGroups.Where(g => groupIds.Contains(g.Id)).ToListAsync();
        var selected = req.ModifierOptionIds.Count == 0
            ? new List<ModifierOption>()
            : await _db.ModifierOptions.Where(o => req.ModifierOptionIds.Contains(o.Id) && o.IsActive).ToListAsync();
        foreach (var group in groups)
        {
            var count = selected.Count(o => o.ModifierGroupId == group.Id);
            if ((group.Required || group.MinSelect > 0) && count < Math.Max(group.MinSelect, group.Required ? 1 : 0))
                throw new AppException($"Choose an option for {group.Name}.");
            if (group.MaxSelect > 0 && count > group.MaxSelect)
                throw new AppException($"Too many options for {group.Name}.");
        }
        if (selected.Any(o => !groupIds.Contains(o.ModifierGroupId)))
            throw new AppException("That modifier does not belong to this item.");
        var allowedAddons = await _db.ProductAddons.Where(x => x.ProductId == product.Id).Select(x => x.AddonId).ToListAsync();
        var addons = req.AddonIds.Count == 0
            ? new List<Addon>()
            : await _db.Addons.Where(a => req.AddonIds.Contains(a.Id) && a.IsActive).ToListAsync();
        if (addons.Any(a => !allowedAddons.Contains(a.Id))) throw new AppException("That add-on is not available for this item.");
        var course = string.IsNullOrWhiteSpace(req.Course) ? CourseFor(product.Category?.Name) : Pricing.Course(req.Course);
        var line = new SaleLine
        {
            SaleOrderId = order.Id,
            ProductId = product.Id,
            Name = product.Name,
            Qty = Money.Q(req.Qty),
            UnitPrice = product.Price,
            DiscountAmount = Money.R(Math.Max(0, req.DiscountAmount)),
            Course = course,
            Notes = Clean(req.Notes)
        };
        foreach (var option in selected)
            line.Extras.Add(new SaleLineExtra { Kind = "Modifier", Name = option.Name, Price = option.Price });
        foreach (var addon in addons)
            line.Extras.Add(new SaleLineExtra { Kind = "Addon", Name = addon.Name, Price = addon.Price });
        return line;
    }

    async Task Recalc(SaleOrder order)
    {
        Coupon? coupon = null;
        if (!string.IsNullOrWhiteSpace(order.CouponCode))
            coupon = await _db.Coupons.FirstOrDefaultAsync(c => c.OrganizationId == order.OrganizationId && c.Code == order.CouponCode);
        var branch = order.Branch ?? await _db.Branches.FirstAsync(b => b.Id == order.BranchId);
        Pricing.Apply(order, branch, coupon);
    }

    async Task<SaleOrder> Load(int id, int orgId)
    {
        var order = await _db.SaleOrders
            .Include(o => o.Lines).ThenInclude(l => l.Extras)
            .Include(o => o.Tenders)
            .Include(o => o.Receipts).ThenInclude(r => r.Events)
            .Include(o => o.Receipts).ThenInclude(r => r.Dispatches)
            .Include(o => o.Branch)
            .Include(o => o.Table)
            .Include(o => o.Customer)
            .Include(o => o.Waiter)
            .Include(o => o.Cashier)
            .Include(o => o.Rider)
            .FirstOrDefaultAsync(o => o.Id == id && o.OrganizationId == orgId)
            ?? throw new AppException("Order not found.", 404);
        return order;
    }

    async Task<Receipt> EnsureReceipt(SaleOrder order, int orgId)
    {
        var existing = order.Receipts.FirstOrDefault() ?? await _db.Receipts.FirstOrDefaultAsync(r => r.SaleOrderId == order.Id);
        if (existing != null)
        {
            if (!order.Receipts.Contains(existing)) order.Receipts.Add(existing);
            return existing;
        }
        var receipt = new Receipt
        {
            SaleOrderId = order.Id,
            ReceiptNo = await Sequences.Next(_db, orgId, "receipt", "RCP-"),
            Token = Guid.NewGuid().ToString("N")
        };
        _db.Receipts.Add(receipt);
        order.Receipts.Add(receipt);
        await _db.SaveChangesAsync();
        return receipt;
    }

    async Task<string> HtmlFor(SaleOrder order, Receipt receipt, int width, string apiBase)
    {
        var app = await Settings.Get(_db, order.OrganizationId, SettingKey.PublicApp, "http://localhost:5173");
        var footer = await Settings.Get(_db, order.OrganizationId, SettingKey.Footer, "Thank you for dining with us.");
        var verify = app.TrimEnd('/') + "/#/r/" + receipt.Token;
        var qr = apiBase.TrimEnd('/') + "/api/public/receipts/" + receipt.Token + "/qr.png";
        return ReceiptHtml.Build(order, receipt, width <= 58 ? 58 : 80, verify, qr, footer);
    }

    async Task SyncTable(int? tableId)
    {
        if (tableId == null) return;
        var table = await _db.DiningTables.FirstOrDefaultAsync(t => t.Id == tableId);
        if (table == null || table.Status == "Reserved") return;
        var busy = await _db.SaleOrders.AnyAsync(o => o.TableId == tableId && o.Status != "Void" && (o.PaymentStatus == "Unpaid" || o.PaymentStatus == "Partial"));
        table.Status = busy ? "Occupied" : "Free";
    }

    async Task<Shift?> OpenShiftFor(int branchId, int? terminalId)
    {
        var q = _db.Shifts.Include(s => s.Terminal).Include(s => s.User).Where(s => s.BranchId == branchId && s.Status == "Open");
        if (terminalId is int tid) q = q.Where(s => s.TerminalId == tid);
        return await q.OrderByDescending(s => s.Id).FirstOrDefaultAsync();
    }

    async Task Queue(UserPrincipal me, int branchId, string type, string entityKey, string reason, decimal amount, string payload)
    {
        var exists = await _db.ApprovalRequests.AnyAsync(a => a.Status == "Pending" && a.Type == type && a.EntityKey == entityKey);
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

    static void EnsureEditable(SaleOrder order)
    {
        if (order.Status == "Void") throw new AppException("This order is void.");
        if (order.PaidTotal > 0) throw new AppException("Payments have started, so the items can no longer be edited.");
    }

    static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    static string JoinNotes(SaleLine line)
    {
        var extras = string.Join(", ", line.Extras.Select(e => e.Name));
        return string.Join(" · ", new[] { extras, line.Notes }.Where(s => !string.IsNullOrWhiteSpace(s)));
    }

    static string CourseFor(string? category) => category switch
    {
        "Starters" => "Starter",
        "Dessert" => "Dessert",
        "Drinks" => "Drink",
        _ => "Main"
    };
}
