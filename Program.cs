using ERP.Application;
using ERP.Infrastructure;
using ERP.Infrastructure.Logging;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Hubs;
using ERP_HRM_CMS.Middleware;
using ERP_HRM_CMS.Services;
using ERP_HRM_CMS.Services.HRM;
using ERP_HRM_CMS.Services.Payroll;
using ERP_HRM_CMS.Services.Printing;
using ERP_HRM_CMS.Services.Security;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// 0. Cấu hình Serilog ghi log có cấu trúc (Console + Rolling File theo ngày kèm CorrelationId)
builder.AddErpSerilog();

// 0a. Cấu hình bắt buộc: không có giá trị mặc định ngoài môi trường Development
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

// 0b. AllowedHosts: ngoài Development không được để "*" (chấp nhận mọi Host header)
var allowedHosts = builder.Configuration["AllowedHosts"];
if (!builder.Environment.IsDevelopment() && (string.IsNullOrWhiteSpace(allowedHosts) || allowedHosts.Trim() == "*"))
{
    throw new InvalidOperationException(
        "Thiếu hoặc không hợp lệ AllowedHosts. Ngoài Development, đặt biến môi trường AllowedHosts bằng danh sách tên miền cụ thể (không dùng '*').");
}

// 1. Đăng ký MVC Controllers & Views
builder.Services.AddControllersWithViews();

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
    .SetApplicationName("ERP_HRM_CMS");

// 2. Đăng ký Clean Architecture: Application Layer & Infrastructure Layer
// (Bao gồm EF Core SQL Server, IMemoryCache tập trung, CurrentUserService, DataScopeService,
//  PermissionService, HealthChecks /health, ResponseCompression Brotli/Gzip và ForwardedHeaders)
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, connectionString);

// 3. Đăng ký các Domain/Web Services nghiệp vụ
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IPayrollService, PayrollService>();
builder.Services.Configure<PayrollOptions>(builder.Configuration.GetSection(PayrollOptions.SectionName));
builder.Services.AddScoped<LoginThrottleService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<IContractService, ContractService>();
builder.Services.AddScoped<IHrDecisionService, HrDecisionService>();
builder.Services.AddScoped<IAssetManagementService, AssetManagementService>();
builder.Services.AddScoped<IShiftScheduleService, ShiftScheduleService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<ILeaveManagementService, LeaveManagementService>();
builder.Services.AddScoped<ISelfServicePortalService, SelfServicePortalService>();
builder.Services.AddScoped<IRecruitmentService, RecruitmentService>();
builder.Services.AddScoped<IOnboardingService, OnboardingService>();
builder.Services.AddScoped<IPerformanceReviewService, PerformanceReviewService>();
builder.Services.AddScoped<ITrainingService, TrainingService>();
builder.Services.AddScoped<IPeopleAnalyticsService, PeopleAnalyticsService>();
builder.Services.AddScoped<IPrintService, PrintService>();
builder.Services.AddScoped<ISuperAdminService, SuperAdminService>();

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
        // Thu hồi phiên ngay khi tài khoản bị khóa, đổi vai trò hoặc đổi mật khẩu (so SecurityStamp với DB)
        options.Events.OnValidatePrincipal = CookieSecurityStampValidator.ValidateAsync;
        // Development chạy HTTP (cổng 5129) nên giữ SameAsRequest; production bắt buộc HTTPS.
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
    });

// 6. Cấu hình Session State (chỉ phục vụ giỏ hàng khách vãng lai; danh tính người dùng lấy từ Claims)
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
        throw;
    }
}

// Lệnh một lần: mã hóa lại CCCD và số tài khoản còn ở dạng rõ, rồi thoát (không chạy web server).
if (args.Contains("--migrate-sensitive-data"))
{
    using var migrationScope = app.Services.CreateScope();
    var db = migrationScope.ServiceProvider.GetRequiredService<AppDbContext>();
    var protection = migrationScope.ServiceProvider.GetRequiredService<IDataProtectionProvider>();
    int count = await SensitiveDataMigrationService.EncryptLegacyValuesAsync(db, protection);
    app.Logger.LogInformation("Đã mã hóa {Count} giá trị CCCD/số tài khoản.", count);
    return;
}

// Pipeline xử lý Request: ForwardedHeaders -> CorrelationId -> Serilog -> ResponseCompression -> SecurityHeaders
app.UseInfrastructure();
app.UseMiddleware<SecurityHeadersMiddleware>();

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
app.UseMustChangePasswordEnforcement();

app.MapInfrastructureEndpoints();
app.MapHub<NotificationHub>("/hub/notifications");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();

// Cho phép project test (WebApplicationFactory<Program>) truy cập điểm vào ứng dụng
public partial class Program { }
