using FixHub.Web.Services;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// ForwardedHeaders: para detectar HTTPS cuando va detrás de nginx
if (!builder.Environment.IsDevelopment())
{
    builder.Services.Configure<ForwardedHeadersOptions>(o =>
    {
        o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        o.KnownNetworks.Clear();
        o.KnownProxies.Clear();
    });
}

// ─────────────────────────────────────────────────────────────────────────────
// 1. Razor Pages
// ─────────────────────────────────────────────────────────────────────────────
builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();

// ─────────────────────────────────────────────────────────────────────────────
// 2. HttpClient typed hacia la API — Web NO contiene lógica de negocio
//    BearerTokenHandler inyecta automáticamente el JWT desde la cookie de sesión.
// ─────────────────────────────────────────────────────────────────────────────
var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"]
    ?? "http://localhost:5100";

builder.Services.AddTransient<BearerTokenHandler>();
builder.Services.AddHttpClient<IFixHubApiClient, FixHubApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
}).AddHttpMessageHandler<BearerTokenHandler>();

// ─────────────────────────────────────────────────────────────────────────────
// 3. Cookie Auth (token JWT almacenado en cookie HttpOnly)
// ─────────────────────────────────────────────────────────────────────────────
builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", options =>
    {
        options.Cookie.Name = "fixhub_token";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        // HTTPS: Secure cuando hay X-Forwarded-Proto (detectado con ForwardedHeaders)
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();
builder.Logging.AddConsole();

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("[FixHub.Web] API BaseUrl = {ApiBaseUrl}", apiBaseUrl);

// Desarrollo: si la API no responde, intentar iniciarla automáticamente
if (app.Environment.IsDevelopment())
{
    await EnsureApiRunningAsync(apiBaseUrl, logger);
}

static async Task EnsureApiRunningAsync(string apiBaseUrl, ILogger logger)
{
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
    try
    {
        using var client = new HttpClient();
        var resp = await client.GetAsync($"{apiBaseUrl.TrimEnd('/')}/api/v1/health", cts.Token);
        if (resp.IsSuccessStatusCode)
        {
            logger.LogInformation("[FixHub.Web] API ya está respondiendo en {Url}", apiBaseUrl);
            return;
        }
    }
    catch { /* API no responde */ }

    var apiProj = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "FixHub.API", "FixHub.API.csproj");
    if (!File.Exists(apiProj))
    {
        apiProj = Path.Combine(Directory.GetCurrentDirectory(), "..", "FixHub.API", "FixHub.API.csproj");
    }
    if (!File.Exists(apiProj))
    {
        logger.LogWarning("[FixHub.Web] No se encontró FixHub.API.csproj. Inicia la API manualmente: dotnet run --project src/FixHub.API");
        return;
    }

    logger.LogInformation("[FixHub.Web] Iniciando FixHub.API... (puede tardar unos segundos)");
    var pi = new System.Diagnostics.ProcessStartInfo
    {
        FileName = "dotnet",
        Arguments = $"run --project \"{Path.GetFullPath(apiProj)}\"",
        UseShellExecute = true,
        CreateNoWindow = false
    };
    System.Diagnostics.Process.Start(pi);

    for (var i = 0; i < 20; i++)
    {
        await Task.Delay(1500);
        try
        {
            using var client = new HttpClient();
            var resp = await client.GetAsync($"{apiBaseUrl.TrimEnd('/')}/api/v1/health");
            if (resp.IsSuccessStatusCode)
            {
                logger.LogInformation("[FixHub.Web] API lista en {Url} tras ~{Sec}s", apiBaseUrl, (i + 1) * 2);
                return;
            }
        }
        catch { }
    }
    logger.LogWarning("[FixHub.Web] La API no respondió a tiempo. El login puede fallar hasta que arranque.");
}

// ─────────────────────────────────────────────────────────────────────────────
// 4. Pipeline HTTP
// ─────────────────────────────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseForwardedHeaders();
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

app.Run();
