using KongPortal.Data;
using KongPortal.Security;
using KongPortal.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((ctx, lc) => lc
    .WriteTo.Console()
    .ReadFrom.Configuration(ctx.Configuration));

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 12;
    options.Password.RequireNonAlphanumeric = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// Cookie security
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
    options.LoginPath = "/Account/Login";
});

// Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.CanView,    p => p.RequireRole(Roles.Viewer, Roles.Operator, Roles.Admin));
    options.AddPolicy(Policies.CanOperate, p => p.RequireRole(Roles.Operator, Roles.Admin));
    options.AddPolicy(Policies.CanAdmin,   p => p.RequireRole(Roles.Admin));
});

// CSRF
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// Health checks
builder.Services.AddHealthChecks()
    .AddSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")!,
        name: "sqlserver",
        tags: new[] { "db" })
    .AddUrlGroup(
        new Uri((builder.Configuration["Kong:AdminUrl"] ?? "http://kong:8001") + "/status"),
        name: "kong-admin",
        tags: new[] { "kong" });

// Kong Admin HTTP Client
builder.Services.AddHttpClient<KongAdminClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Kong:AdminUrl"]
        ?? throw new InvalidOperationException("Kong:AdminUrl not configured"));
    client.Timeout = TimeSpan.FromSeconds(30);
});

// App services
builder.Services.AddScoped<RotationService>();
builder.Services.AddScoped<AuditLogService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<ConsumerProfileRepository>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Startup: Migrate + Seed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    await DbSeeder.SeedRolesAsync(scope.ServiceProvider);
    await DbSeeder.SeedAdminUserAsync(scope.ServiceProvider);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();

// Health endpoint - no auth required
app.MapHealthChecks("/health");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
