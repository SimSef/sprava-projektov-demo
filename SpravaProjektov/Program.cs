using SpravaProjektov.Application.Config;
using System.Text;
using SpravaProjektov.Application.Auth;
using SpravaProjektov.Application.Projects;
using SpravaProjektov.Data.Xml;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Enable legacy code pages
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

// Add XML config provider and bind to AppConfig
var xmlConfigPath = Path.Combine(builder.Environment.ContentRootPath, "config", "app.config.xml");
builder.Configuration.AddXmlFile(xmlConfigPath, optional: false, reloadOnChange: true);
builder.Services
    .AddOptions<AppConfig>()
    .Bind(builder.Configuration)
    .Validate(c => !string.IsNullOrWhiteSpace(c.Storage?.UsersPath), "Storage.UsersPath required")
    .Validate(c => !string.IsNullOrWhiteSpace(c.Storage?.ProjectsPath), "Storage.ProjectsPath required")
    .ValidateOnStart();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

builder.Services.AddHttpContextAccessor();

// Register XML-based auth repository (issues cookie on sign-in)
builder.Services.AddSingleton<IAuthRepository, XmlAuthRepository>();

// Register XML-based projects repository
builder.Services.AddSingleton<IProjectRepository, XmlProjectRepository>();

// Demo over HTTP
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.HttpOnly = true;
    options.SlidingExpiration = true;
});

var app = builder.Build();

app.UseExceptionHandler("/Error", createScopeForErrors: true);

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<SpravaProjektov.Presentation.App>()
    .AddInteractiveServerRenderMode();

app.Run();
