using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace RestaurantPos.Infrastructure;

public class AppException : Exception
{
    public int Status { get; }
    public AppException(string message, int status = 400) : base(message) => Status = status;
}

public static class PasswordUtil
{
    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        return Convert.ToBase64String(salt) + "." + Convert.ToBase64String(hash);
    }

    public static bool Verify(string password, string stored)
    {
        var parts = stored.Split('.');
        if (parts.Length != 2) return false;
        try
        {
            var salt = Convert.FromBase64String(parts[0]);
            var expected = Convert.FromBase64String(parts[1]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch
        {
            return false;
        }
    }
}

public static class Clock
{
    public static readonly TimeZoneInfo Zone = Load();

    static TimeZoneInfo Load()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Bangladesh Standard Time"); }
        catch { return TimeZoneInfo.CreateCustomTimeZone("BDT", TimeSpan.FromHours(6), "BDT", "BDT"); }
    }

    public static DateOnly TodayBd() => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zone));

    public static DateTime BdDateStartUtc(DateOnly date)
    {
        var local = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(local, Zone);
    }

    public static DateTime ToUtcFromBd(DateTime bdLocal)
    {
        var local = DateTime.SpecifyKind(bdLocal, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(local, Zone);
    }
}

public static class Money
{
    public static decimal R(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
    public static decimal Q(decimal value) => Math.Round(value, 3, MidpointRounding.AwayFromZero);
}

public sealed class UserPrincipal
{
    public int Id { get; init; }
    public int OrganizationId { get; init; }
    public string FullName { get; init; } = "";
    public string Role { get; init; } = "";
    public int? BranchId { get; init; }
    public HashSet<string> Permissions { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public bool IsAdmin => Role.Equals("Admin", StringComparison.OrdinalIgnoreCase);
    public bool Has(string key) => IsAdmin || Permissions.Contains(key);
    public void Require(params string[] keys)
    {
        if (keys.Length == 0 || keys.Any(Has)) return;
        throw new AppException("You do not have permission for this action.", 403);
    }
}

public class CurrentUser
{
    private readonly IHttpContextAccessor _http;
    private readonly Data.AppDbContext _db;
    private UserPrincipal? _cached;

    public CurrentUser(IHttpContextAccessor http, Data.AppDbContext db)
    {
        _http = http;
        _db = db;
    }

    public async Task<UserPrincipal> GetAsync()
    {
        if (_cached != null) return _cached;
        var claim = _http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? _http.HttpContext?.User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var id))
            throw new AppException("Please sign in again.", 401);

        var user = await _db.Users.Include(u => u.Role).ThenInclude(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Id == id && u.IsActive)
            ?? throw new AppException("This account is not active.", 401);

        _cached = new UserPrincipal
        {
            Id = user.Id,
            OrganizationId = user.OrganizationId,
            FullName = user.FullName,
            Role = user.Role.Name,
            BranchId = user.DefaultBranchId,
            Permissions = user.Role.RolePermissions.Select(x => x.Permission.Key).ToHashSet(StringComparer.OrdinalIgnoreCase)
        };
        return _cached;
    }
}

public class Gate
{
    private readonly Data.AppDbContext _db;
    private readonly CurrentUser _current;
    public Gate(Data.AppDbContext db, CurrentUser current) { _db = db; _current = current; }

    public Task<UserPrincipal> Me() => _current.GetAsync();

    public async Task<List<int>> BranchIds()
    {
        var me = await Me();
        if (me.Has("branch.all"))
            return await _db.Branches.Where(b => b.OrganizationId == me.OrganizationId && b.IsActive).Select(b => b.Id).ToListAsync();
        return await _db.UserBranches.Where(x => x.UserId == me.Id).Select(x => x.BranchId).ToListAsync();
    }

    public async Task<(UserPrincipal Me, Branch Branch)> Branch(int branchId)
    {
        var me = await Me();
        var branch = await _db.Branches.FirstOrDefaultAsync(b => b.Id == branchId && b.OrganizationId == me.OrganizationId && b.IsActive)
            ?? throw new AppException("Branch not found.", 404);
        if (!me.Has("branch.all"))
        {
            var ok = await _db.UserBranches.AnyAsync(x => x.UserId == me.Id && x.BranchId == branchId);
            if (!ok) throw new AppException("You cannot use this branch.", 403);
        }
        return (me, branch);
    }
}

public class Tracker
{
    private readonly Data.AppDbContext _db;
    private readonly IHttpContextAccessor _http;
    public Tracker(Data.AppDbContext db, IHttpContextAccessor http) { _db = db; _http = http; }

    public void Log(int orgId, int? userId, string action, string entity, string? entityId, string? detail)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            OrganizationId = orgId,
            UserId = userId,
            Action = action,
            Entity = entity,
            EntityId = entityId,
            Detail = detail,
            Ip = _http.HttpContext?.Connection.RemoteIpAddress?.ToString(),
            CreatedAt = DateTime.UtcNow
        });
    }

    public string? Ip => _http.HttpContext?.Connection.RemoteIpAddress?.ToString();
    public string? Agent => _http.HttpContext?.Request.Headers.UserAgent.ToString();
}

public static class Sequences
{
    public static async Task<string> Next(Data.AppDbContext db, int orgId, string key, string prefix, int pad = 5)
    {
        var seq = await db.NumberSequences.FirstOrDefaultAsync(s => s.OrganizationId == orgId && s.Key == key);
        if (seq == null)
        {
            seq = new NumberSequence { OrganizationId = orgId, Key = key, LastValue = 0 };
            db.NumberSequences.Add(seq);
        }
        seq.LastValue += 1;
        await db.SaveChangesAsync();
        return prefix + seq.LastValue.ToString().PadLeft(pad, '0');
    }
}

public static class Settings
{
    public static async Task<string> Get(Data.AppDbContext db, int orgId, string key, string fallback = "")
    {
        var row = await db.SystemSettings.AsNoTracking().FirstOrDefaultAsync(x => x.OrganizationId == orgId && x.Key == key);
        return string.IsNullOrWhiteSpace(row?.Value) ? fallback : row.Value;
    }

    public static async Task<decimal> Dec(Data.AppDbContext db, int orgId, string key, decimal fallback)
    {
        var raw = await Get(db, orgId, key, "");
        return decimal.TryParse(raw, out var value) ? value : fallback;
    }
}

public static class SettingKey
{
    public const string DiscountLimit = "discount.limit";
    public const string RefundLimit = "refund.limit";
    public const string AdjustmentLimit = "adjustment.limit";
    public const string Footer = "receipt.footer";
    public const string PublicApp = "public.app";
    public const string Thermal = "thermal.width";
}
