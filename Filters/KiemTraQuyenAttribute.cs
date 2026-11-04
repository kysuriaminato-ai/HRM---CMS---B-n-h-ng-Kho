using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ERP_HRM_CMS.Helpers;
using System.Security.Claims;

namespace ERP_HRM_CMS.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class KiemTraQuyenAttribute : TypeFilterAttribute
    {
        public KiemTraQuyenAttribute(string permissionCode) : base(typeof(KiemTraQuyenFilter))
        {
            Arguments = new object[] { permissionCode };
        }
    }

    public class KiemTraQuyenFilter : IAsyncAuthorizationFilter
    {
        private readonly string _permissionCode;
        private readonly IPermissionService _permissionService;

        public KiemTraQuyenFilter(string permissionCode, IPermissionService permissionService)
        {
            _permissionCode = permissionCode;
            _permissionService = permissionService;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var http = context.HttpContext;
            var user = http.User;
            bool laAjax = LaYeuCauAjax(http.Request);

            if (!user.Identity?.IsAuthenticated ?? true)
            {
                // Request AJAX nhận JSON 401 thay vì chuyển hướng HTML tới trang đăng nhập
                context.Result = laAjax
                    ? JsonLoi(401, "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.")
                    : new RedirectToActionResult("Login", "Auth", null);
                return;
            }

            // Super Admin luôn có toàn quyền (Bypass kiểm tra ma trận)
            if (user.IsInRole("SUPERADMIN") || user.IsInRole("SuperAdmin") ||
                user.FindFirst("IsSuperAdmin")?.Value?.Equals("True", StringComparison.OrdinalIgnoreCase) == true)
            {
                return;
            }

            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
            {
                context.Result = laAjax
                    ? JsonLoi(401, "Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại.")
                    : new RedirectToActionResult("Login", "Auth", null);
                return;
            }

            bool hasPermission = await _permissionService.HasPermissionAsync(userId, _permissionCode);
            if (!hasPermission)
            {
                // AJAX: JSON 403. Trang thường: chuyển tới trang báo lỗi không có quyền
                context.Result = laAjax
                    ? JsonLoi(403, "Bạn không có quyền thực hiện thao tác này.")
                    : new RedirectToActionResult("KhongCoQuyen", "Home", null);
            }
        }

        private static IActionResult JsonLoi(int statusCode, string message)
        {
            return new JsonResult(new { success = false, message }) { StatusCode = statusCode };
        }

        /// <summary>Nhận diện request AJAX/fetch: header XHR, Accept JSON hoặc Content-Type JSON.</summary>
        private static bool LaYeuCauAjax(HttpRequest request)
        {
            if (string.Equals(request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (request.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return (request.ContentType ?? string.Empty).StartsWith("application/json", StringComparison.OrdinalIgnoreCase);
        }
    }
}
