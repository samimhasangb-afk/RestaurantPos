using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using RestaurantPos.Data;

namespace RestaurantPos.Services;

public class JwtSettings
{
    public string Key { get; set; } = "";
    public string Issuer { get; set; } = "";
    public string Audience { get; set; } = "";
    public int ExpiryMinutes { get; set; } = 720;
}

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly Tracker _tracker;
    private readonly JwtSettings _jwt;

    public AuthService(AppDbContext db, Tracker tracker, IConfiguration config)
    {
        _db = db;
        _tracker = tracker;
        _jwt = config.GetSection("JwtSettings").Get<JwtSettings>() ?? throw new InvalidOperationException("JwtSettings is missing.");
    }

    public async Task<LoginResponse> Login(LoginRequest req)
    {
        var username = (req.Username ?? "").Trim();
        var user = await _db.Users.Include(u => u.Role).ThenInclude(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .Include(u => u.Organization)
            .Include(u => u.Branches).ThenInclude(b => b.Branch)
            .FirstOrDefaultAsync(u => u.Username == username);

        var ok = user != null && user.IsActive && PasswordUtil.Verify(req.Password ?? "", user.PasswordHash);
        _db.LoginHistories.Add(new LoginHistory
        {
            UserId = user?.Id,
            Username = username,
            Success = ok,
            Ip = _tracker.Ip,
            UserAgent = _tracker.Agent
        });
        if (ok && user != null)
            _tracker.Log(user.OrganizationId, user.Id, "Login", "User", user.Id.ToString(), username);
        await _db.SaveChangesAsync();
        if (!ok || user == null) throw new AppException("Username or password is incorrect.", 401);

        var perms = user.Role.RolePermissions.Select(x => x.Permission.Key).Distinct().ToList();
        var token = WriteToken(user);
        return new LoginResponse { Token = token, User = ToDto(user, perms) };
    }

    public async Task<UserDto> Me(int userId)
    {
        var user = await _db.Users.Include(u => u.Role).ThenInclude(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .Include(u => u.Organization).Include(u => u.Branches).ThenInclude(b => b.Branch)
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive)
            ?? throw new AppException("Account not found.", 401);
        return ToDto(user, user.Role.RolePermissions.Select(x => x.Permission.Key).Distinct().ToList());
    }

    string WriteToken(AppUser user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new("role", user.Role.Name),
            new("org", user.OrganizationId.ToString())
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(_jwt.Issuer, _jwt.Audience, claims, expires: DateTime.UtcNow.AddMinutes(_jwt.ExpiryMinutes), signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    static UserDto ToDto(AppUser user, List<string> perms)
    {
        var branches = user.Branches.Select(b => b.Branch).Where(b => b != null).Select(Map.Branch).ToList();
        return new UserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Username = user.Username,
            Email = user.Email,
            Phone = user.Phone,
            Role = user.Role.Name,
            RoleId = user.RoleId,
            OrganizationId = user.OrganizationId,
            OrganizationName = user.Organization?.Name ?? "",
            BranchId = user.DefaultBranchId,
            IsActive = user.IsActive,
            BranchIds = user.Branches.Select(b => b.BranchId).ToList(),
            Branches = branches,
            Permissions = perms
        };
    }
}
