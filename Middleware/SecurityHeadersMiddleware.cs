namespace ERP_HRM_CMS.Middleware
{
    /// <summary>
    /// Gắn các header bảo mật cho mọi phản hồi: CSP (whitelist các CDN đang dùng),
    /// chống MIME sniffing, chống nhúng iframe và giới hạn Referrer.
    /// </summary>
    public class SecurityHeadersMiddleware
    {
        // 'unsafe-inline' cần thiết vì các view hiện còn script/style inline. Khi chuyển sang nonce hoặc
        // tách hết script inline thì bỏ 'unsafe-inline' để CSP chặt hơn.
        private const string ContentSecurityPolicy =
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com; " +
            "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://fonts.googleapis.com; " +
            "font-src 'self' data: https://cdn.jsdelivr.net https://fonts.gstatic.com; " +
            "img-src 'self' data: https:; " +
            "connect-src 'self' ws: wss:; " +
            "frame-ancestors 'none'; " +
            "base-uri 'self'; " +
            "form-action 'self'";

        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var headers = context.Response.Headers;
            headers["Content-Security-Policy"] = ContentSecurityPolicy;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            await _next(context);
        }
    }
}
