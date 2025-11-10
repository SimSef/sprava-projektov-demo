using Microsoft.AspNetCore.Components.Authorization;
using SpravaProjektov.Components;
using SpravaProjektov.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SpravaProjektov.Application.Config;
using System.Text;
using Microsoft.Extensions.Configuration;
using SpravaProjektov.Application.Auth;
using SpravaProjektov.Data.Xml;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Enable legacy code pages
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

// Add XML config provider and bind to AppConfig (Options)
builder.Configuration.AddXmlFile(Path.Combine("config", "app.config.xml"), optional: false, reloadOnChange: true);
builder.Services.Configure<AppConfig>(builder.Configuration.GetSection("appConfig"));

// Add services to the container.
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

// Access HttpContext in services
builder.Services.AddHttpContextAccessor();

// Register XML-based auth repository (issues cookie on sign-in)
builder.Services.AddSingleton<IAuthRepository, XmlAuthRepository>();

// Demo over HTTP: allow cookies over HTTP (no proxy TLS)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.HttpOnly = true;
    options.SlidingExpiration = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // no EF migrations in this demo
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // HSTS disabled for demo; no reverse proxy/TLS here.
}

// HTTPS redirection disabled (demo runs over HTTP).


app.UseAntiforgery();

// No global cookie policy enforcing Secure=Always; demo uses HTTP.

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// No Identity endpoints; custom cookie auth handles login/logout.

app.Run();
