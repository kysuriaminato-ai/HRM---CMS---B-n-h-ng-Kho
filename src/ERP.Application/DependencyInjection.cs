using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Security;
using Microsoft.Extensions.DependencyInjection;

namespace ERP.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Đăng ký các dịch vụ cốt lõi thuộc tầng Application (Data Scope, Business Policies...).
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IDataScopeService, DataScopeService>();
        return services;
    }
}

