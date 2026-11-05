using Microsoft.AspNetCore.Builder;
using Serilog;
using Serilog.Events;

namespace ERP.Infrastructure.Logging;

public static class SerilogExtensions
{
    private const string OutputTemplate =
        "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] [{CorrelationId}] [User:{UserId}] {Message:lj}{NewLine}{Exception}";

    /// <summary>
    /// Cấu hình Serilog ghi log có cấu trúc ra Console và Rolling File theo ngày kèm CorrelationId.
    /// Sử dụng shared: true để an toàn khi chạy đa luồng hoặc song song trong kiểm thử tích hợp.
    /// </summary>
    public static WebApplicationBuilder AddErpSerilog(this WebApplicationBuilder builder)
    {
        var logDirectory = Path.Combine(builder.Environment.ContentRootPath, "logs");
        Directory.CreateDirectory(logDirectory);
        var logFilePath = Path.Combine(logDirectory, "erp-.log");

        builder.Host.UseSerilog((context, services, loggerConfiguration) =>
        {
            loggerConfiguration
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "ERP_HRM_CMS")
                .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
                .WriteTo.Console(outputTemplate: OutputTemplate)
                .WriteTo.File(
                    path: logFilePath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    shared: true,
                    outputTemplate: OutputTemplate);
        });

        return builder;
    }
}

