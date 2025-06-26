using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WebThoiTrang.Models.Entity;
using WebThoiTrang.Repositories.EF;
using WebThoiTrang.Repositories.Interface;
using WebThoiTrang.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Hosting;

var builder = WebApplication.CreateBuilder(args);

// --- Logging ---
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

// --- DbContext ---
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// --- Identity ---
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Có thể thêm tùy chọn như Password.RequiredLength = 6,...
})
.AddDefaultTokenProviders()
.AddDefaultUI()
.AddEntityFrameworkStores<ApplicationDbContext>();

// --- Cookie cấu hình cho Identity ---
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

// --- HttpClient cho ImageProcessingService (1 phút timeout) ---
builder.Services.AddHttpClient<ImageProcessingService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["AIService:ApiBaseUrl"]
        ?? throw new InvalidOperationException("AIService:ApiBaseUrl not found in configuration."));
    client.DefaultRequestHeaders.Add("X-RapidAPI-Host", "try-on-diffusion.p.rapidapi.com");
})
.SetHandlerLifetime(TimeSpan.FromMinutes(5))
.ConfigureHttpClient(client => client.Timeout = TimeSpan.FromMinutes(1)); // ✅ 60 giây timeout

// --- Services ---
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddScoped<IProductRepository, EFProductRepository>();
builder.Services.AddScoped<ICategoryRepository, EFCategoryRepository>();

// --- Hangfire ---
var hangfireConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(hangfireConnectionString, new SqlServerStorageOptions
    {
        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
        QueuePollInterval = TimeSpan.FromSeconds(15),
        UseRecommendedIsolationLevel = true,
        DisableGlobalLocks = true
    }));
builder.Services.AddHangfireServer();

// --- MVC, Razor, Session ---
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// --- Tùy chỉnh ModelState ---
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

var app = builder.Build();

// --- Middlewares ---
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();

// ⚠️ Phục vụ tệp tĩnh từ wwwroot
app.UseStaticFiles();

// ⚠️ Phục vụ tệp từ thư mục TryOn_Images (nằm ngang hàng wwwroot)
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "TryOn_Images")),
    RequestPath = "/TryOn_Images"
});

app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// --- Hangfire Dashboard ---
app.UseHangfireDashboard("/hangfire");

// --- Endpoint Routing ---
app.MapControllerRoute(
    name: "Admin",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();
app.Run();
