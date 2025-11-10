using SpravaProjektov.Presentation;
using SpravaProjektov.Application.Config;
using System.Text;
using SpravaProjektov.Application.Auth;
using SpravaProjektov.Data.Xml;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Enable legacy code pages
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

// Add XML config provider and bind to AppConfig (Options)
builder.Configuration.AddXmlFile(Path.Combine("config", "app.config.xml"), optional: false, reloadOnChange: true);
builder.Services.Configure<AppConfig>(builder.Configuration.GetSection("appConfig"));

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

// Access HttpContext in services
builder.Services.AddHttpContextAccessor();

// Register XML-based auth repository (issues cookie on sign-in)
builder.Services.AddSingleton<IAuthRepository, XmlAuthRepository>();

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
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
