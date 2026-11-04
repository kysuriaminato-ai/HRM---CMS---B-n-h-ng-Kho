using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Hubs;
using ERP_HRM_CMS.Services;
using ERP_HRM_CMS.Services.Printing;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 0. Cấu hình bắt buộc: không có giá trị mặc định ngoài môi trường Development
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Thiếu ConnectionStrings:DefaultConnection. Ngoài Development, hãy đặt biến môi trường ConnectionStrings__DefaultConnection.");
}

// Khóa mã hóa dữ liệu nhạy cảm (CCCD, số tài khoản). Mất khóa là mất khả năng đọc dữ liệu đã mã hóa,
// nên ngoài Development phải chỉ định thư mục khóa bền (có sao lưu), không nằm trong mã nguồn.
var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"];
if (string.IsNullOrWhiteSpace(dataProtectionKeysPath))
{
    if (!builder.Environment.IsDevelopment())
    {
        throw new InvalidOperationException(
            "Thiếu DataProtection:KeysPath. Ngoài Development, hãy đặt biến môi trường DataProtection__KeysPath trỏ tới thư mục khóa bền.");
    }
    dataProtectionKeysPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtection-Keys");
}

// 1. Đăng ký MVC Controllers & Views
builder.Services.AddControllersWithViews();

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
    .SetApplicationName("ERP_HRM_CMS");

// 2. Đăng ký Entity Framework Core 8 với SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// 3. Đăng ký HttpContextAccessor và Dependency Injection cho các Services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IPayrollService, PayrollService>();
builder.Services.AddScoped<LoginThrottleService>();
// [OOP - ĐA HÌNH] Đăng ký interface IPrintService; nơi dùng chỉ phụ thuộc vào interface
builder.Services.AddScoped<IPrintService, PrintService>();

// Đăng ký SignalR Real-time
builder.Services.AddSignalR();

// 4. Phân quyền mặc định: mọi endpoint đều yêu cầu đăng nhập, trừ khi đánh dấu [AllowAnonymous] rõ ràng
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// 5. Cấu hình Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "ERP_HRM_CMS_AuthCookie";
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Home/KhongCoQuyen";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        // Development chạy HTTP (cổng 5129) nên giữ SameAsRequest; production bắt buộc HTTPS.
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
    });

// 6. Cấu hình Session State
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "ERP_HRM_CMS_Session";
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
});

var app = builder.Build();

// 7. Khởi tạo Cơ sở Dữ liệu
// SeedCore (quyền, tài khoản quản trị, cấu hình site) chạy ở mọi môi trường.
// SeedDemo (dữ liệu mẫu) chỉ chạy khi Development và Seed:Demo=true.
var seedDemo = app.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("Seed:Demo");
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        await DbInitializer.SeedAsync(context, builder.Configuration["SeedAdmin:Password"], seedDemo);
        app.Logger.LogInformation("Đã khởi tạo cơ sở dữ liệu. Dữ liệu mẫu: {SeedDemo}", seedDemo);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Đã xảy ra lỗi trong quá trình khởi tạo dữ liệu (DbInitializer).");
        // Dừng khởi động: không chạy ứng dụng khi schema/dữ liệu nền bị lỗi
        throw;
    }
}

// Lệnh một lần: mã hóa lại CCCD và số tài khoản còn ở dạng rõ, rồi thoát (không chạy web server).
// Ví dụ: dotnet run --project ERP_HRM_CMS.csproj -- --migrate-sensitive-data
if (args.Contains("--migrate-sensitive-data"))
{
    using var migrationScope = app.Services.CreateScope();
    var db = migrationScope.ServiceProvider.GetRequiredService<AppDbContext>();
    var protection = migrationScope.ServiceProvider.GetRequiredService<IDataProtectionProvider>();
    int count = await SensitiveDataMigrationService.EncryptLegacyValuesAsync(db, protection);
    app.Logger.LogInformation("Đã mã hóa {Count} giá trị CCCD/số tài khoản.", count);
    return;
}

// Pipeline xử lý Request
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapHub<NotificationHub>("/hub/notifications");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();

// Cho phép project test (WebApplicationFactory<Program>) truy cập điểm vào ứng dụng
public partial class Program { }
