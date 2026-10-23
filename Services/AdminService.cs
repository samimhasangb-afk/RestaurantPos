using System.Text.Json;
using RestaurantPos.Data;

namespace RestaurantPos.Services;

public class AdminService
{
    private readonly AppDbContext _db;
    private readonly Gate _gate;
    private readonly Tracker _tracker;
    private readonly SalesService _sales;
    private readonly InventoryService _inventory;
    private readonly IWebHostEnvironment _env;

    public AdminService(AppDbContext db, Gate gate, Tracker tracker, SalesService sales, InventoryService inventory, IWebHostEnvironment env)
    {
        _db = db;
        _gate = gate;
        _tracker = tracker;
        _sales = sales;
        _inventory = inventory;
        _env = env;
    }

    public async Task<List<RoleDto>> Roles()
    {
        var me = await _gate.Me();
        me.Require("admin.users", "admin.settings");
        var roles = await _db.Roles.Include(r => r.RolePermissions).ThenInclude(p => p.Permission)
            .Where(r => r.OrganizationId == me.OrganizationId).OrderBy(r => r.Name).ToListAsync();
        return roles.Select(r => new RoleDto { Id = r.Id, Name = r.Name, Permissions = r.RolePermissions.Select(p => p.Permission.Key).OrderBy(x => x).ToList() }).ToList();
    }

    public async Task<List<UserDto>> Users()
    {
        var me = await _gate.Me();
        me.Require("admin.users");
        var users = await _db.Users.Include(u => u.Role).Include(u => u.Organization).Include(u => u.Branches).ThenInclude(b => b.Branch)
            .Where(u => u.OrganizationId == me.OrganizationId).OrderBy(u => u.FullName).ToListAsync();
        return users.Select(u => new UserDto
        {
            Id = u.Id, FullName = u.FullName, Username = u.Username, Email = u.Email, Phone = u.Phone,
            Role = u.Role.Name, RoleId = u.RoleId, OrganizationId = u.OrganizationId, OrganizationName = u.Organization.Name,
            BranchId = u.DefaultBranchId, IsActive = u.IsActive,
            BranchIds = u.Branches.Select(b => b.BranchId).ToList(),
            Branches = u.Branches.Where(b => b.Branch != null).Select(b => Map.Branch(b.Branch)).ToList()
        }).ToList();
    }

    public async Task<UserDto> SaveUser(SaveUserRequest req)
    {
        var me = await _gate.Me();
        me.Require("admin.users");
        if (string.IsNullOrWhiteSpace(req.FullName) || string.IsNullOrWhiteSpace(req.Username))
            throw new AppException("Name and username are required.");
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == req.RoleId && r.OrganizationId == me.OrganizationId)
            ?? throw new AppException("Role not found.", 404);
        AppUser user;
        if (req.Id is int id)
        {
            user = await _db.Users.Include(u => u.Branches).FirstOrDefaultAsync(u => u.Id == id && u.OrganizationId == me.OrganizationId)
                ?? throw new AppException("User not found.", 404);
            if (!string.IsNullOrWhiteSpace(req.Password)) user.PasswordHash = PasswordUtil.Hash(req.Password);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(req.Password) || req.Password.Length < 6)
                throw new AppException("A new user needs a password of at least 6 characters.");
            user = new AppUser { OrganizationId = me.OrganizationId, PasswordHash = PasswordUtil.Hash(req.Password) };
            _db.Users.Add(user);
        }
        var username = req.Username.Trim();
        var taken = await _db.Users.AnyAsync(u => u.Username == username && u.Id != user.Id);
        if (taken) throw new AppException("That username is already in use.");
        user.FullName = req.FullName.Trim();
        user.Username = username;
        user.Email = string.IsNullOrWhiteSpace(req.Email) ? null : req.Email.Trim();
        user.Phone = string.IsNullOrWhiteSpace(req.Phone) ? null : req.Phone.Trim();
        user.RoleId = role.Id;
        user.DefaultBranchId = req.DefaultBranchId;
        user.IsActive = req.IsActive;
        await _db.SaveChangesAsync();
        var existing = await _db.UserBranches.Where(x => x.UserId == user.Id).ToListAsync();
        _db.UserBranches.RemoveRange(existing);
        foreach (var branchId in req.BranchIds.Distinct())
        {
            var ok = await _db.Branches.AnyAsync(b => b.Id == branchId && b.OrganizationId == me.OrganizationId);
            if (ok) _db.UserBranches.Add(new UserBranch { UserId = user.Id, BranchId = branchId });
        }
        _tracker.Log(me.OrganizationId, me.Id, "SaveUser", "User", user.Id.ToString(), user.Username);
        await _db.SaveChangesAsync();
        return (await Users()).First(u => u.Id == user.Id);
    }

    public async Task<List<BranchDto>> Branches()
    {
        var me = await _gate.Me();
        return await _db.Branches.Where(b => b.OrganizationId == me.OrganizationId).OrderBy(b => b.Name).Select(b => new BranchDto
        {
            Id = b.Id, Name = b.Name, Code = b.Code, Address = b.Address, Phone = b.Phone,
            TaxPercent = b.TaxPercent, ServiceChargePercent = b.ServiceChargePercent, IsActive = b.IsActive
        }).ToListAsync();
    }

    public async Task<BranchDto> SaveBranch(SaveBranchRequest req)
    {
        var me = await _gate.Me();
        me.Require("admin.settings");
        if (string.IsNullOrWhiteSpace(req.Name) || string.IsNullOrWhiteSpace(req.Code))
            throw new AppException("Branch name and code are required.");
        Branch row;
        if (req.Id is int id) row = await _db.Branches.FirstOrDefaultAsync(b => b.Id == id && b.OrganizationId == me.OrganizationId) ?? throw new AppException("Branch not found.", 404);
        else { row = new Branch { OrganizationId = me.OrganizationId }; _db.Branches.Add(row); }
        row.Name = req.Name.Trim();
        row.Code = req.Code.Trim().ToUpperInvariant();
        row.Address = string.IsNullOrWhiteSpace(req.Address) ? null : req.Address.Trim();
        row.Phone = string.IsNullOrWhiteSpace(req.Phone) ? null : req.Phone.Trim();
        row.TaxPercent = req.TaxPercent;
        row.ServiceChargePercent = req.ServiceChargePercent;
        row.IsActive = req.IsActive;
        _tracker.Log(me.OrganizationId, me.Id, "SaveBranch", "Branch", row.Id.ToString(), row.Name);
        await _db.SaveChangesAsync();
        return Map.Branch(row);
    }

    public async Task<List<TerminalDto>> Terminals(int? branchId)
    {
        var me = await _gate.Me();
        var ids = await _gate.BranchIds();
        var q = _db.Terminals.Where(t => ids.Contains(t.BranchId));
        if (branchId is int b) q = q.Where(t => t.BranchId == b);
        return await q.OrderBy(t => t.Name).Select(t => new TerminalDto { Id = t.Id, BranchId = t.BranchId, Name = t.Name, IsActive = t.IsActive }).ToListAsync();
    }

    public async Task<TerminalDto> SaveTerminal(SaveTerminalRequest req)
    {
        var (me, _) = await _gate.Branch(req.BranchId);
        me.Require("admin.settings");
        if (string.IsNullOrWhiteSpace(req.Name)) throw new AppException("Terminal name is required.");
        Terminal row;
        if (req.Id is int id) row = await _db.Terminals.FirstOrDefaultAsync(t => t.Id == id && t.BranchId == req.BranchId) ?? throw new AppException("Terminal not found.", 404);
        else { row = new Terminal { BranchId = req.BranchId }; _db.Terminals.Add(row); }
        row.Name = req.Name.Trim();
        row.IsActive = req.IsActive;
        await _db.SaveChangesAsync();
        return new TerminalDto { Id = row.Id, BranchId = row.BranchId, Name = row.Name, IsActive = row.IsActive };
    }

    public async Task<List<AuditDto>> Audit()
    {
        var me = await _gate.Me();
        me.Require("admin.settings", "admin.users");
        var rows = await _db.AuditLogs.Where(a => a.OrganizationId == me.OrganizationId).OrderByDescending(a => a.Id).Take(250).ToListAsync();
        var ids = rows.Where(r => r.UserId != null).Select(r => r.UserId!.Value).Distinct().ToList();
        var names = await _db.Users.Where(u => ids.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName);
        return rows.Select(r => new AuditDto
        {
            Id = r.Id,
            UserName = r.UserId != null && names.TryGetValue(r.UserId.Value, out var name) ? name : null,
            Action = r.Action, Entity = r.Entity, EntityId = r.EntityId, Detail = r.Detail, Ip = r.Ip, CreatedAt = r.CreatedAt
        }).ToList();
    }

    public async Task<List<LoginHistoryDto>> Logins()
    {
        var me = await _gate.Me();
        me.Require("admin.settings", "admin.users");
        var usernames = await _db.Users.Where(u => u.OrganizationId == me.OrganizationId).Select(u => u.Username).ToListAsync();
        return await _db.LoginHistories.Where(l => usernames.Contains(l.Username)).OrderByDescending(l => l.Id).Take(200)
            .Select(l => new LoginHistoryDto { Id = l.Id, Username = l.Username, Success = l.Success, Ip = l.Ip, CreatedAt = l.CreatedAt }).ToListAsync();
    }

    public async Task<List<SettingDto>> GetSettings()
    {
        var me = await _gate.Me();
        me.Require("admin.settings");
        return await _db.SystemSettings.Where(s => s.OrganizationId == me.OrganizationId).OrderBy(s => s.Key)
            .Select(s => new SettingDto { Key = s.Key, Value = s.Value }).ToListAsync();
    }

    public async Task<List<SettingDto>> SaveSettings(List<SettingDto> rows)
    {
        var me = await _gate.Me();
        me.Require("admin.settings");
        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.Key)) continue;
            if (row.Key.Contains("pass", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(row.Value)) continue;
            var existing = await _db.SystemSettings.FirstOrDefaultAsync(s => s.OrganizationId == me.OrganizationId && s.Key == row.Key);
            if (existing == null) _db.SystemSettings.Add(new SystemSetting { OrganizationId = me.OrganizationId, Key = row.Key.Trim(), Value = row.Value ?? "" });
            else existing.Value = row.Value ?? "";
        }
        _tracker.Log(me.OrganizationId, me.Id, "SaveSettings", "SystemSetting", null, rows.Count + " keys");
        await _db.SaveChangesAsync();
        return await GetSettings();
    }

    public async Task<List<ApprovalDto>> Approvals()
    {
        var me = await _gate.Me();
        me.Require("approval.review", "pos.order");
        var q = _db.ApprovalRequests.Include(a => a.RequestedBy).Where(a => a.OrganizationId == me.OrganizationId);
        if (!me.Has("approval.review")) q = q.Where(a => a.RequestedById == me.Id);
        var rows = await q.OrderByDescending(a => a.Id).Take(100).ToListAsync();
        return rows.Select(a => new ApprovalDto
        {
            Id = a.Id, Type = a.Type, Status = a.Status, Reason = a.Reason, Amount = a.Amount,
            RequestedBy = a.RequestedBy.FullName, CreatedAt = a.CreatedAt, ReviewNote = a.ReviewNote
        }).ToList();
    }

    public async Task<ApprovalDto> Review(int id, bool approve, string? note)
    {
        var me = await _gate.Me();
        me.Require("approval.review");
        var row = await _db.ApprovalRequests.Include(a => a.RequestedBy).FirstOrDefaultAsync(a => a.Id == id && a.OrganizationId == me.OrganizationId)
            ?? throw new AppException("Approval not found.", 404);
        if (row.Status != "Pending") throw new AppException("This request was already reviewed.");
        if (approve)
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(row.PayloadJson) ? "{}" : row.PayloadJson);
            var root = doc.RootElement;
            if (row.Type == "Discount")
                await _sales.Discount(root.GetProperty("orderId").GetInt32(), root.GetProperty("amount").GetDecimal(), true);
            else if (row.Type == "Refund")
                await _sales.Refund(root.GetProperty("orderId").GetInt32(), new RefundRequest
                {
                    Amount = root.GetProperty("amount").GetDecimal(),
                    Reason = root.GetProperty("reason").GetString() ?? "Approved refund",
                    Method = root.GetProperty("method").GetString() ?? "Cash",
                    Restock = root.TryGetProperty("restock", out var restock) && restock.GetBoolean()
                }, true);
            else if (row.Type == "Void")
                await _sales.Void(root.GetProperty("orderId").GetInt32(), root.GetProperty("reason").GetString() ?? "Approved void", true);
            else if (row.Type == "StockAdjustment")
                await _inventory.PostApprovedAdjustment(root.GetProperty("adjustmentId").GetInt32(), me.Id);
            else throw new AppException("This approval type cannot be applied.");
        }
        else if (row.Type == "StockAdjustment")
        {
            using var doc = JsonDocument.Parse(row.PayloadJson);
            var adjId = doc.RootElement.GetProperty("adjustmentId").GetInt32();
            var adj = await _db.StockAdjustments.FirstOrDefaultAsync(a => a.Id == adjId);
            if (adj != null && adj.Status == "Pending") adj.Status = "Rejected";
        }
        row.Status = approve ? "Approved" : "Rejected";
        row.ReviewedById = me.Id;
        row.ReviewedAt = DateTime.UtcNow;
        row.ReviewNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        _tracker.Log(me.OrganizationId, me.Id, row.Status, "Approval", row.Id.ToString(), row.Type);
        await _db.SaveChangesAsync();
        return new ApprovalDto
        {
            Id = row.Id, Type = row.Type, Status = row.Status, Reason = row.Reason, Amount = row.Amount,
            RequestedBy = row.RequestedBy.FullName, CreatedAt = row.CreatedAt, ReviewNote = row.ReviewNote
        };
    }

    public async Task<BackupDto> Backup()
    {
        var me = await _gate.Me();
        me.Require("admin.backup");
        var dir = Path.Combine(_env.ContentRootPath, "backups");
        Directory.CreateDirectory(dir);
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var jsonName = $"snapshot-{stamp}.json";
        var snapshot = new
        {
            CreatedAt = DateTime.UtcNow,
            Organization = await _db.Organizations.Where(o => o.Id == me.OrganizationId).Select(o => o.Name).FirstAsync(),
            Branches = await _db.Branches.CountAsync(b => b.OrganizationId == me.OrganizationId),
            Products = await _db.Products.CountAsync(p => p.OrganizationId == me.OrganizationId),
            Orders = await _db.SaleOrders.CountAsync(o => o.OrganizationId == me.OrganizationId),
            Settings = await _db.SystemSettings.Where(s => s.OrganizationId == me.OrganizationId).Select(s => new { s.Key, s.Value }).ToListAsync()
        };
        await File.WriteAllTextAsync(Path.Combine(dir, jsonName), JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true }));
        var sqlName = $"RestaurantPosDb-{stamp}.bak";
        var sqlPath = Path.Combine(dir, sqlName);
        var message = "A JSON snapshot was saved.";
        try
        {
            var safe = sqlPath.Replace("'", "''");
            var backupSql = "BACKUP DATABASE [RestaurantPosDb] TO DISK = N'" + safe + "' WITH FORMAT, INIT, NAME = N'Saffron POS'";
            await _db.Database.ExecuteSqlRawAsync(backupSql);
            message = "SQL backup and JSON snapshot were saved.";
        }
        catch (Exception ex)
        {
            sqlName = "";
            message = "JSON snapshot was saved. SQL backup was not created: " + ex.GetBaseException().Message;
        }
        _tracker.Log(me.OrganizationId, me.Id, "Backup", "Database", null, message);
        await _db.SaveChangesAsync();
        return new BackupDto { JsonFile = jsonName, SqlFile = string.IsNullOrEmpty(sqlName) ? null : sqlName, Message = message };
    }

    public async Task<List<string>> BackupFiles()
    {
        var me = await _gate.Me();
        me.Require("admin.backup", "admin.settings");
        return ListBackupFiles();
    }

    public async Task<MonitorDto> Monitor()
    {
        var me = await _gate.Me();
        me.Require("admin.settings", "reports.view");
        var ids = await _gate.BranchIds();
        var low = 0;
        var ingredients = await _db.Ingredients.Where(i => i.OrganizationId == me.OrganizationId && i.IsActive).Select(i => new { i.Id, i.ReorderLevel }).ToListAsync();
        var balances = await _db.StockBalances.Where(b => ids.Contains(b.BranchId)).ToListAsync();
        foreach (var branch in ids)
            foreach (var ingredient in ingredients)
                if ((balances.FirstOrDefault(b => b.BranchId == branch && b.IngredientId == ingredient.Id)?.Quantity ?? 0) <= ingredient.ReorderLevel)
                    low++;
        return new MonitorDto
        {
            DatabaseOnline = await _db.Database.CanConnectAsync(),
            ServerTimeUtc = DateTime.UtcNow,
            Today = Clock.TodayBd().ToString("yyyy-MM-dd"),
            Users = await _db.Users.CountAsync(u => u.OrganizationId == me.OrganizationId && u.IsActive),
            OpenOrders = await _db.SaleOrders.CountAsync(o => ids.Contains(o.BranchId) && o.Status != "Void" && (o.PaymentStatus == "Unpaid" || o.PaymentStatus == "Partial")),
            PendingApprovals = await _db.ApprovalRequests.CountAsync(a => a.OrganizationId == me.OrganizationId && a.Status == "Pending"),
            LowStock = low,
            Backups = ListBackupFiles()
        };
    }

    List<string> ListBackupFiles()
    {
        var dir = Path.Combine(_env.ContentRootPath, "backups");
        if (!Directory.Exists(dir)) return new List<string>();
        return Directory.GetFiles(dir).Select(Path.GetFileName).Where(n => n != null).Select(n => n!).OrderByDescending(n => n).ToList();
    }
}
