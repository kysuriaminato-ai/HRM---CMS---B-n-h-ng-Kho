using System.IO.Compression;
using System.Net;
using System.Text.Json;
using ERP.Application.Common.Interfaces;
using ERP.Infrastructure.Caching;
using ERP.Infrastructure.HealthChecks;
using ERP.Infrastructure.Identity;
using ERP.Infrastructure.Logging;
using ERP.Infrastructure.Middleware;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;

namespace ERP.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Đăng ký toàn bộ hạ tầng kỹ thuật: DbContext (SQL Server), IMemoryCache trung tâm,
    /// CurrentUserService, PermissionService, HealthChecks, ResponseCompression (Brotli &amp; Gzip) và ForwardedHeaders.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionString)
    {
        // 1. Bộ nhớ đệm tập trung IMemoryCache
        services.AddMemoryCache();
        services.AddSingleton<IAppCacheService, MemoryAppCacheService>();

        // 2. EF Core SQL Server (chỉ định assembly chứa Migrations thuộc ERP.Infrastructure)
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sql => sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        // 3. Ngữ cảnh người dùng hiện tại & Phân quyền có Cache
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IPermissionService, PermissionService>();

        // 4. Health Checks kiểm tra kết nối SQL Server tại /health
        services.AddHealthChecks()
            .AddCheck<SqlServerDbHealthCheck>(
                name: "sqlserver",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "db", "sql", "ready" });

        // 5. Response Compression (Brotli & Gzip)
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
            options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
            {
                "application/json",
                "application/javascript",
                "text/css",
                "image/svg+xml"
            });
        });

        services.Configure<BrotliCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Fastest;
        });

        services.Configure<GzipCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Fastest;
        });

        // 6. ForwardedHeaders an toàn cho môi trường Reverse Proxy (Nginx / IIS / Cloudflare / Traefik)
        // Mặc định chỉ tin cậy Loopback (127.0.0.1 / ::1) và danh sách proxy khai báo tường minh trong ReverseProxy:KnownProxies
        // -> Chống giả mạo header X-Forwarded-For để vượt giới hạn đăng nhập sai (S8b).
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = 1;

            var configuredProxies = configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>();
            if (configuredProxies != null)
            {
                foreach (var rawIp in configuredProxies)
                {
                    if (!string.IsNullOrWhiteSpace(rawIp) && IPAddress.TryParse(rawIp.Trim(), out var ip))
                    {
                        options.KnownProxies.Add(ip);
                    }
                }
            }
        });

        return services;
    }

    /// <summary>
    /// Đưa ForwardedHeaders, CorrelationId, Serilog Request Logging và ResponseCompression vào đầu HTTP Pipeline.
    /// </summary>
    public static WebApplication UseInfrastructure(this WebApplication app)
    {
        app.UseForwardedHeaders();
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseSerilogRequestLogging(options =>
        {
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("CorrelationId", httpContext.TraceIdentifier);
                diagnosticContext.Set(
                    "UserId",
                    httpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous");
                diagnosticContext.Set("ClientIP", httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
            };
        });
        app.UseResponseCompression();
        return app;
    }

    /// <summary>
    /// Đẩy UserId thực từ Claims vào Serilog LogContext và bắt buộc người dùng có cờ MustChangePassword
    /// phải đổi mật khẩu trước khi truy cập hệ thống (đặt sau UseAuthentication/UseAuthorization).
    /// </summary>
    public static WebApplication UseMustChangePasswordEnforcement(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                using (Serilog.Context.LogContext.PushProperty("UserId", userId))
                {
                    await next();
                }
                return;
            }

            await next();
        });
        app.UseMiddleware<MustChangePasswordMiddleware>();
        return app;
    }

    /// <summary>
    /// Ánh xạ endpoint /health kiểm tra sức khỏe kết nối SQL Server trả về JSON chuẩn.
    /// </summary>
    public static WebApplication MapInfrastructureEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json; charset=utf-8";
                var payload = new
                {
                    status = report.Status.ToString(),
                    totalDurationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 2),
                    timestamp = VietnamTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    checks = report.Entries.Select(entry => new
                    {
                        name = entry.Key,
                        status = entry.Value.Status.ToString(),
                        description = entry.Value.Description,
                        durationMs = Math.Round(entry.Value.Duration.TotalMilliseconds, 2)
                    })
                };
                await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
            }
        }).AllowAnonymous();

        return app;
    }
}

