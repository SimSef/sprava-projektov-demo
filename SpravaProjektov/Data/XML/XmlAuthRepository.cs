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

    public async Task<SignInResult> SignInAsync(string username, string password, bool persistent = false, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sign-in attempt for {Username}", username);
        var user = FindUser(username);
        if (user is null)
        {
            _logger.LogWarning("User not found: {Username}", username);
            return SignInResult.Fail("Používateľ neexistuje.");
        }

        var ok = string.Equals(user.Password, password, StringComparison.Ordinal);
        if (!ok)
        {
            _logger.LogWarning("Invalid credentials for {Username}", username);
            return SignInResult.Fail("Nesprávne heslo.");
        }

        _logger.LogDebug("Creating claims for {Username} with {RoleCount} roles", user.Username, user.Roles.Count);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Username),
            new(ClaimTypes.Name, user.Username)
        };
        if (!string.IsNullOrWhiteSpace(user.DisplayName))
        {
            claims.Add(new("display_name", user.DisplayName!));
        }
        foreach (var role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var httpContext = _http.HttpContext;
        if (httpContext is null)
        {
            _logger.LogWarning("No HttpContext available to issue auth cookie.");
            return SignInResult.Fail("Interná chyba.");
        }

        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = persistent });
        _logger.LogInformation("Sign-in succeeded for {Username}", username);
        return SignInResult.Success();
    }



    public async Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Signing out current user");
        var httpContext = _http.HttpContext;
        if (httpContext is not null)
        {
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _logger.LogInformation("Signed out");
        }
    }

    private UserXml? FindUser(string username)
    {
        try
        {
            _logger.LogDebug("Searching for user {Username}", username);
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
        var storage = _options.Value?.Storage;
        if (storage is null || string.IsNullOrWhiteSpace(storage.UsersPath))
        {
            _logger.LogError("App configuration missing Storage.UsersPath. Check config/app.config.xml binding.");
            throw new InvalidOperationException("Storage.UsersPath is not configured.");
        }
        var relPath = storage.UsersPath;
        var fullPath = Path.IsPathRooted(relPath) ? relPath : Path.Combine(AppContext.BaseDirectory, relPath);
        if (!File.Exists(fullPath))
        {
            _logger.LogError("Users XML not found at {Path}", fullPath);
            throw new FileNotFoundException($"Users XML not found at '{fullPath}'.");
        }

        var ser = new XmlSerializer(typeof(UsersXml));
        using var fs = File.OpenRead(fullPath);
        var data = ser.Deserialize(fs) as UsersXml;
        if (data is null)
        {
            _logger.LogError("Failed to deserialize users XML at {Path}", fullPath);
            throw new InvalidOperationException("Failed to deserialize users XML.");
        }
        _logger.LogDebug("Loaded {Count} users from {Path}", data.Users?.Count ?? 0, fullPath);
        return data;
    }
}
