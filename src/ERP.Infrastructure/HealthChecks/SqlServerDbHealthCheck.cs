using ERP_HRM_CMS.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ERP.Infrastructure.HealthChecks;

/// <summary>
/// Kiểm tra sức khỏe kết nối thực tế tới SQL Server thông qua thực thi truy vấn nhẹ SELECT 1.
/// </summary>
public sealed class SqlServerDbHealthCheck : IHealthCheck
{
    private readonly AppDbContext _dbContext;

    public SqlServerDbHealthCheck(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            bool canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
            if (!canConnect)
            {
                return HealthCheckResult.Unhealthy("Không thể mở kết nối tới SQL Server.");
            }

            await _dbContext.Database.ExecuteSqlRawAsync("SELECT 1;", cancellationToken);

            return HealthCheckResult.Healthy("SQL Server đang hoạt động ổn định.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Lỗi kết nối SQL Server.", ex);
        }
    }
}

