using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace ERP.Infrastructure.Logging;

/// <summary>
/// Gắn CorrelationId cho từng HTTP Request (đọc từ X-Correlation-ID hợp lệ hoặc tự sinh mới),
/// trả lại header X-Correlation-ID ở Response và đẩy vào Serilog LogContext.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";
    private const int MaxHeaderLength = 64;

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        string correlationId = ResolveCorrelationId(context);
        context.TraceIdentifier = correlationId;

        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(HeaderName))
            {
                context.Response.Headers[HeaderName] = correlationId;
            }
            return Task.CompletedTask;
        });

        string? userId = context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("UserId", userId ?? "anonymous"))
        {
            await _next(context);
        }
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out var incoming))
        {
            var candidate = incoming.ToString().Trim();
            if (IsSafeCorrelationId(candidate))
            {
                return candidate;
            }
        }

        return Guid.NewGuid().ToString("N");
    }

    private static bool IsSafeCorrelationId(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length > MaxHeaderLength)
            return false;

        foreach (char c in value)
        {
            if (!char.IsLetterOrDigit(c) && c != '-' && c != '_' && c != '.')
                return false;
        }

        return true;
    }
}

