using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using ToDoList.Core.Interfaces;
using ToDoList.Data;
using ToDoList.Data.Repositories;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Kestrel hardening — cap body size and concurrent connections so a LAN
// peer can't trivially DoS the server. The largest legitimate body is a
// ReorderItemsRequest of ~10 KB or a 10 KB Notes payload.
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 256 * 1024;
    options.Limits.MaxRequestHeadersTotalSize = 16 * 1024;
    options.Limits.MaxConcurrentConnections = 200;
    options.Limits.MaxConcurrentUpgradedConnections = 50;
});

// Database
builder.Services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();

// Repositories
builder.Services.AddScoped<TodoListRepository>();
builder.Services.AddScoped<TodoItemRepository>();
builder.Services.AddScoped<CategoryRepository>();
builder.Services.AddScoped<DeletedEntityRepository>();

// Controllers
builder.Services.AddControllers();

// ProblemDetails ensures unhandled errors return RFC 7807 JSON instead of
// the Developer Exception Page (which would leak stack traces).
builder.Services.AddProblemDetails();

// Rate limiting. Two policies:
//   - "global"  — applied to every endpoint (600 req/min per IP)
//   - "debug"   — stricter, used by the debug-log controller
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 600,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.AddPolicy("debug", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

WebApplication app = builder.Build();

// Global exception handler — must be first so it wraps everything else.
// Returns ProblemDetails JSON; no stack traces.
app.UseExceptionHandler();
app.UseStatusCodePages();

// Security response headers for any HTML served from wwwroot/docs.
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-Frame-Options"] = "DENY";
    ctx.Response.Headers["Referrer-Policy"] = "no-referrer";
    await next();
});

// Serve documentation site from wwwroot/docs
app.UseStaticFiles();

app.UseRouting();
app.UseRateLimiter();

app.MapControllers();

app.Run();
