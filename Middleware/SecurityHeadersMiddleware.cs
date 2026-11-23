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
            // 'self' thay cho 'none': vẫn chặn trang web khác nhúng site này (chống clickjacking),
            // nhưng cho phép chính site tự nhúng mình, phục vụ khung xem thử giao diện điện thoại.
            "frame-ancestors 'self'; " +
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
            // SAMEORIGIN thay cho DENY, cùng lý do với frame-ancestors 'self' ở trên.
            headers["X-Frame-Options"] = "SAMEORIGIN";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            await _next(context);
        }
    }
}
