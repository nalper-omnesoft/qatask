using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using ApiService.Data;
using ApiService.Endpoints;
using dotenv.net;
using Serilog;

// Load .env from repo root
var envDir = new DirectoryInfo(AppContext.BaseDirectory);
while (envDir != null && !File.Exists(Path.Combine(envDir.FullName, ".env")))
    envDir = envDir.Parent;
if (envDir != null)
    DotEnv.Load(options: new DotEnvOptions(envFilePaths: [Path.Combine(envDir.FullName, ".env")]));

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Information)
    .WriteTo.Console()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

builder.Services.AddHealthChecks();
builder.Services.AddOpenApi();

// Authentication with Keycloak
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.Cookie.Name = "QaTask.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
})
.AddOpenIdConnect(options =>
{
    options.Authority = builder.Configuration["Keycloak:Authority"]
        ?? $"http://localhost:6101/realms/qa-task";
    var metadataAddress = builder.Configuration["Keycloak:MetadataAddress"];
    if (!string.IsNullOrEmpty(metadataAddress))
        options.MetadataAddress = metadataAddress;
    options.ClientId = builder.Configuration["Keycloak:ClientId"] ?? "qa-task-web";
    options.ClientSecret = builder.Configuration["Keycloak:ClientSecret"];
    options.ResponseType = "code";
    options.UsePkce = true;
    options.SaveTokens = true;
    options.GetClaimsFromUserInfoEndpoint = true;
    options.RequireHttpsMetadata = false;
    options.MapInboundClaims = false;
    options.PushedAuthorizationBehavior = PushedAuthorizationBehavior.Disable;
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");
    options.TokenValidationParameters.NameClaimType = "preferred_username";
    options.TokenValidationParameters.RoleClaimType = "roles";
    options.Scope.Add("roles");
    options.Events = new OpenIdConnectEvents
    {
        OnRedirectToIdentityProvider = ctx =>
        {
            ctx.ProtocolMessage.Prompt = "login";
            return Task.CompletedTask;
        },
        OnRemoteFailure = ctx =>
        {
            ctx.HandleResponse();
            ctx.Response.Redirect("/");
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// Database
builder.Services.AddDbContext<QaTaskDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("QaTaskDb")));

var app = builder.Build();

// Auto-migrate database on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<QaTaskDbContext>();
    db.Database.Migrate();
}

app.UseSerilogRequestLogging();
app.MapHealthChecks("/health");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

// Login endpoint
app.MapGet("/login", async (HttpContext context) =>
{
    var frontendUrl = app.Configuration["Frontend:BaseUrl"] ?? "/";
    await context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties
    {
        RedirectUri = frontendUrl
    });
});

// Logout endpoint
app.MapGet("/logout", async (HttpContext context) =>
{
    var frontendUrl = app.Configuration["Frontend:BaseUrl"] ?? "/";
    var idToken = await context.GetTokenAsync("id_token");
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

    var authority = app.Configuration["Keycloak:Authority"]
        ?? "http://localhost:6101/realms/qa-task";
    var postLogoutUri = frontendUrl.TrimEnd('/') + "/";
    var logoutUrl = $"{authority}/protocol/openid-connect/logout?post_logout_redirect_uri={Uri.EscapeDataString(postLogoutUri)}&client_id={app.Configuration["Keycloak:ClientId"] ?? "qa-task-web"}";
    if (!string.IsNullOrEmpty(idToken))
    {
        logoutUrl += $"&id_token_hint={idToken}";
    }
    context.Response.Redirect(logoutUrl);
});

// User info endpoint (no auth required so frontend can check login status)
app.MapGet("/auth/user", (HttpContext context) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var displayName = context.User.FindFirst("name")?.Value
            ?? context.User.FindFirst("given_name")?.Value
            ?? context.User.Identity.Name
            ?? context.User.FindFirst("preferred_username")?.Value
            ?? context.User.FindFirst("email")?.Value;
        var email = context.User.FindFirst("email")?.Value;
        var roles = context.User.FindAll("roles").Select(c => c.Value).ToList();

        return Results.Ok(new
        {
            isAuthenticated = true,
            name = displayName,
            email,
            roles
        });
    }
    return Results.Ok(new { isAuthenticated = false });
});

// API endpoint groups
app.MapTodoEndpoints();

// Serve static files (for production)
app.UseFileServer();

// Fallback: redirect to frontend in dev, serve index.html in prod
app.MapFallback(async (HttpContext context) =>
{
    var requestPath = context.Request.Path.Value ?? "";
    var frontendUrl = app.Configuration["Frontend:BaseUrl"];

    if (app.Environment.IsDevelopment() && !string.IsNullOrEmpty(frontendUrl))
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties
            {
                RedirectUri = frontendUrl + requestPath
            });
            return;
        }
        context.Response.Redirect(frontendUrl + requestPath);
        return;
    }

    if (context.User.Identity?.IsAuthenticated != true)
    {
        await context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties
        {
            RedirectUri = requestPath
        });
        return;
    }

    var webRoot = app.Environment.WebRootPath ?? "wwwroot";
    var indexPath = Path.Combine(webRoot, "index.html");
    if (File.Exists(indexPath))
    {
        context.Response.ContentType = "text/html";
        await context.Response.SendFileAsync(indexPath);
    }
    else
    {
        context.Response.StatusCode = 404;
    }
});

app.Run();

// Make Program accessible to integration tests
public partial class Program { }
