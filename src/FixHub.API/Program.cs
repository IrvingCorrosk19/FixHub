using System.Security.Claims;
using System.Text;
using FixHub.API.Middleware;
using FixHub.Application;
using FixHub.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using System.Threading.RateLimiting;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.HttpOverrides;
using FixHub.API.Services;
using FixHub.Application.Common.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// ForwardedHeaders (FASE 5.1): solo en Production cuando la app va detrás de proxy
if (!builder.Environment.IsDevelopment())
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });
}

// ─────────────────────────────────────────────────────────────────────────────
// 1. Capas Clean Architecture
// ─────────────────────────────────────────────────────────────────────────────
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICorrelationIdAccessor, CorrelationIdAccessor>();

// ─────────────────────────────────────────────────────────────────────────────
// 2. Controllers + JSON (enums como string)
// ─────────────────────────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddProblemDetails();

// ─────────────────────────────────────────────────────────────────────────────
// 2b. Rate Limiting (FASE 5.1): global por IP, auth más restrictivo
// ─────────────────────────────────────────────────────────────────────────────
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, _) =>
    {
        context.HttpContext.Response.ContentType = "application/problem+json";
        var problem = new ProblemDetails
        {
            Title = "Too Many Requests",
            Status = StatusCodes.Status429TooManyRequests,
            Instance = context.HttpContext.Request.Path,
            Extensions = { ["errorCode"] = "RATE_LIMITED" }
        };
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
        await context.HttpContext.Response.WriteAsJsonAsync(problem);
    };

    // Global: 60 req/min por IP (FixedWindow). Aplica a todos los endpoints.
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1)
            }));

    // Auth: 10 req/min por IP; se aplica con [EnableRateLimiting("AuthPolicy")] en AuthController.
    options.AddPolicy("AuthPolicy", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1)
            }));
});

// ─────────────────────────────────────────────────────────────────────────────
// 3. JWT Authentication
// ─────────────────────────────────────────────────────────────────────────────
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"]
    ?? throw new InvalidOperationException("JwtSettings:SecretKey must be set.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.FromSeconds(30),
            // Mapear el claim "role" del JWT al ClaimTypes.Role de .NET
            RoleClaimType = ClaimTypes.Role
        };
    });

// ─────────────────────────────────────────────────────────────────────────────
// 4. Authorization Policies por rol
// ─────────────────────────────────────────────────────────────────────────────
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CustomerOnly", policy =>
        policy.RequireAuthenticatedUser()
              .RequireRole("Customer"));

    options.AddPolicy("TechnicianOnly", policy =>
        policy.RequireAuthenticatedUser()
              .RequireRole("Technician"));

    options.AddPolicy("AdminOnly", policy =>
        policy.RequireAuthenticatedUser()
              .RequireRole("Admin"));

    // Admin puede hacer todo lo que Customer hace
    options.AddPolicy("CustomerOrAdmin", policy =>
        policy.RequireAuthenticatedUser()
              .RequireRole("Customer", "Admin"));
});

// ─────────────────────────────────────────────────────────────────────────────
// 5. Swagger / OpenAPI con JWT
// ─────────────────────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FixHub API",
        Version = "v1",
        Description = "Marketplace de servicios del hogar — Powered by AutonomousFlow"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT token. Example: Bearer {token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ─────────────────────────────────────────────────────────────────────────────
// 6. CORS (FASE 5.2): en Development solo http://localhost:5200; resto desde config
// ─────────────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("WebPolicy", policy =>
    {
        var allowedOrigin = builder.Configuration["WebOrigin"]
            ?? (builder.Environment.IsDevelopment() ? "http://localhost:5200" : "https://localhost:7200");
        policy.WithOrigins(allowedOrigin)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Logging.AddConsole();

var app = builder.Build();

// ─────────────────────────────────────────────────────────────────────────────
// 7. Pipeline HTTP (orden importa)
// ─────────────────────────────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
    app.UseForwardedHeaders();

app.UseMiddleware<CorrelationIdMiddleware>(); // FASE 5.3: CorrelationId en scope y respuesta
app.UseMiddleware<RequestLoggingMiddleware>(); // FASE 5.3: Path, StatusCode, elapsedMs
app.UseMiddleware<ExceptionMiddleware>(); // Convierte excepciones a ProblemDetails
app.UseMiddleware<SecurityHeadersMiddleware>(); // FASE 5.2: headers de seguridad

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FixHub API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseCors("WebPolicy");
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<RequestContextLoggingMiddleware>(); // FASE 14: UserId, JobId en scope
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
