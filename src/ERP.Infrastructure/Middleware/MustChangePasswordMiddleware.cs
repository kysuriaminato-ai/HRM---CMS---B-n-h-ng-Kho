using ERP.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace ERP.Infrastructure.Middleware;

/// <summary>
/// Cưỡng chế tài khoản có cờ MustChangePassword = true bắt buộc đổi mật khẩu ở lần đăng nhập đầu tiên
/// trước khi truy cập bất kỳ chức năng nghiệp vụ nào khác.
/// </summary>
public sealed class MustChangePasswordMiddleware
{
    private readonly RequestDelegate _next;

    public MustChangePasswordMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var user = context.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            var mustChange = user.FindFirst(ErpClaimTypes.MustChangePassword)?.Value;
            if (string.Equals(mustChange, "True", StringComparison.OrdinalIgnoreCase)
                && !IsAllowedPathDuringForcedPasswordChange(context.Request.Path))
            {
                if (IsAjaxRequest(context.Request))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json; charset=utf-8";
                    await context.Response.WriteAsJsonAsync(new
                    {
                        success = false,
                        mustChangePassword = true,
                        redirectUrl = "/Auth/ChangePassword",
                        message = "Bạn bắt buộc phải đổi mật khẩu tạm thời trước khi sử dụng hệ thống."
                    });
                    return;
                }

                context.Response.Redirect("/Auth/ChangePassword");
                return;
            }
        }

        await _next(context);
    }

    private static bool IsAllowedPathDuringForcedPasswordChange(PathString path)
    {
        var p = path.Value ?? string.Empty;
        return p.StartsWith("/Auth/ChangePassword", StringComparison.OrdinalIgnoreCase)
            || p.StartsWith("/Auth/Logout", StringComparison.OrdinalIgnoreCase)
            || p.StartsWith("/Auth/Login", StringComparison.OrdinalIgnoreCase)
            || p.StartsWith("/Home/Error", StringComparison.OrdinalIgnoreCase)
            || p.StartsWith("/health", StringComparison.OrdinalIgnoreCase)
            || p.StartsWith("/css/", StringComparison.OrdinalIgnoreCase)
            || p.StartsWith("/js/", StringComparison.OrdinalIgnoreCase)
            || p.StartsWith("/lib/", StringComparison.OrdinalIgnoreCase)
            || p.StartsWith("/images/", StringComparison.OrdinalIgnoreCase)
            || p.Equals("/favicon.ico", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAjaxRequest(HttpRequest request)
    {
        if (string.Equals(request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase))
            return true;

        if (request.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase))
            return true;

        return (request.ContentType ?? string.Empty).StartsWith("application/json", StringComparison.OrdinalIgnoreCase);
    }
}

