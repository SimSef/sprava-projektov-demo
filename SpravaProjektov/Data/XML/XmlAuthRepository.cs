using System.Xml.Serialization;
using Microsoft.Extensions.Options;
using SpravaProjektov.Application.Auth;
using SpravaProjektov.Application.Config;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace SpravaProjektov.Data.Xml;

public sealed class XmlAuthRepository(IOptions<AppConfig> options, ILogger<XmlAuthRepository> logger, IHttpContextAccessor http) : IAuthRepository
{
    private readonly IOptions<AppConfig> _options = options;
    private readonly ILogger<XmlAuthRepository> _logger = logger;
    private readonly IHttpContextAccessor _http = http;

    public async Task<bool> SignInAsync(string username, string password, bool persistent = false, CancellationToken cancellationToken = default)
    {
        var user = FindUser(username);
        if (user is null) return false;

        var ok = string.Equals(user.Password, password, StringComparison.Ordinal);
        if (!ok) return false;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Username),
            new(ClaimTypes.Name, user.Username)
        };
        if (!string.IsNullOrWhiteSpace(user.DisplayName))
        {
            claims.Add(new("display_name", user.DisplayName!));
        }
        foreach (var role in user.Roles ?? [])
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var httpContext = _http.HttpContext;
        if (httpContext is null)
        {
            _logger.LogWarning("No HttpContext available to issue auth cookie.");
            return false;
        }

        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = persistent });
        return true;
    }

    public Task<UserAccount?> GetUserAsync(string username, CancellationToken cancellationToken = default)
    {
        var user = FindUser(username);
        if (user is null) return Task.FromResult<UserAccount?>(null);

        var roles = user.Roles?.ToArray() ?? [];
        var acc = new UserAccount(user.Username, user.DisplayName, roles);
        return Task.FromResult<UserAccount?>(acc);
    }

    public async Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        var httpContext = _http.HttpContext;
        if (httpContext is not null)
        {
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }

    private UserXml? FindUser(string username)
    {
        try
        {
            var users = LoadUsers();
            return users.Users.FirstOrDefault(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load users for authentication.");
            return null;
        }
    }

    private UsersXml LoadUsers()
    {
        var relPath = _options.Value.Storage.UsersPath;
        var fullPath = Path.IsPathRooted(relPath) ? relPath : Path.Combine(AppContext.BaseDirectory, relPath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Users XML not found at '{fullPath}'.");
        }

        var ser = new XmlSerializer(typeof(UsersXml));
        using var fs = File.OpenRead(fullPath);
        return ser.Deserialize(fs) is UsersXml data
            ? data
            : throw new InvalidOperationException("Failed to deserialize users XML.");
    }
}
