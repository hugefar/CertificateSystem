
using CertificateSystem.BLL;
using CertificateSystem.BLL.DataScope;
using CertificateSystem.DAL;
using CertificateSystem.Web.Authorization;
using CertificateSystem.Web.Data;
using CertificateSystem.Web.Identity;
using CertificateSystem.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Session (used for captcha storage)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
});

// Initialize SqlHelper with connection string from configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
CertificateSystem.DBUtility.SqlHelper.Initialize(connectionString ?? throw new InvalidOperationException("DefaultConnection not found."));

// Identity: configure ApplicationDbContext and Identity services
var identityConnection = builder.Configuration.GetConnectionString("IdentityConnection") ?? builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(identityConnection))
    throw new InvalidOperationException("Identity connection string not found. Add 'IdentityConnection' to appsettings.json.");

builder.Services.AddDbContext<CertificateSystem.Web.Data.ApplicationDbContext>(options =>
    options.UseSqlServer(identityConnection));

builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    // configure identity options as needed
    options.Password.RequireDigit = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
    .AddEntityFrameworkStores<CertificateSystem.Web.Data.ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.AddAuthorization();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IStudentCertificateRepository, StudentCertificateRepository>();
builder.Services.AddScoped<ISecurityLogRepository, SecurityLogRepository>();
builder.Services.AddScoped<IDataScopeService, DataScopeService>();
builder.Services.AddScoped<ICertificateService, CertificateService>();
builder.Services.AddScoped<ILogService, LogService>();
builder.Services.AddScoped<ICertificateGenerator, CertificateGenerator>();
builder.Services.AddSingleton<IBatchPrintQueue, MemoryBatchPrintQueue>();
builder.Services.AddHostedService<BatchPrintBackgroundService>();
builder.Services.AddScoped<IPrintRecordService, PrintRecordService>();

var app = builder.Build();

// Seed identity test data
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        await IdentitySeedData.SeedAsync(services);
    }
}


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
// Serve static files (AdminLTE assets are placed in wwwroot)
// 👇 替换原有 app.UseStaticFiles()，添加 .ftl MIME 类型支持
var provider = new FileExtensionContentTypeProvider();
// 添加 PDF.js 语言包 .ftl 文件的 MIME 类型
provider.Mappings[".ftl"] = "text/plain";
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = provider
});

app.UseRouting();

// Session must be enabled before authentication if captcha stored in session is used during login
app.UseSession();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
