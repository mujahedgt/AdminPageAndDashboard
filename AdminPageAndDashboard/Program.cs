using AdminPageAndDashboard.Data;
using AdminPageAndDashboard.Services;
using AdminPageAndDashboard.Services.ApiClients;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql;

var builder = WebApplication.CreateBuilder(args);

// =============================================
// 1. Add services to the container
// =============================================

// MVC + Newtonsoft.Json support (for complex JSON from APIs)
builder.Services.AddControllersWithViews()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
        options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
    });

// Database - MySQL using Pomelo
builder.Services.AddDbContext<AdminDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    ));

// Session - FIX: Read timeout from configuration
var sessionTimeoutMinutes = builder.Configuration.GetValue<int>("Authentication:SessionTimeoutMinutes", 60);
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(sessionTimeoutMinutes);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Enhanced security
    options.Cookie.SameSite = SameSiteMode.Strict; // CSRF protection
});

// HTTP Clients for external services
builder.Services.AddHttpClient<ApiMiddlewareClient>();
builder.Services.AddHttpClient<IsolationForestClient>();
builder.Services.AddHttpClient<HoneypotClient>();

// Scoped services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ActivityLogService>();
builder.Services.AddScoped<SettingsService>();

var app = builder.Build();

// =============================================
// 2. Configure the HTTP request pipeline
// =============================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Session must come BEFORE UseAuthorization
app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

// Seed database with default admin
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
    await context.Database.MigrateAsync();
    await SeedData.InitializeAsync(context);
}

app.Run();