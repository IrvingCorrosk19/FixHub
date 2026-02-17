using FixHub.Web.Services;

var builder = WebApplication.CreateBuilder(args);

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
        // FASE 5.2: Secure solo en Production para que local http no falle
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();
builder.Logging.AddConsole();

var app = builder.Build();

// ─────────────────────────────────────────────────────────────────────────────
// 4. Pipeline HTTP
// ─────────────────────────────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
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
