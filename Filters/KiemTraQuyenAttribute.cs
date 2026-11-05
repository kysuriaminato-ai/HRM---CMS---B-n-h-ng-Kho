using System.Security.Claims;
using ERP_HRM_CMS.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ERP_HRM_CMS.Filters
{
    /// <summary>
    /// Kiểm tra quyền RBAC/ACL trên Controller hoặc Action.
    /// Hỗ trợ truyền một mã quyền hoặc nhiều mã quyền phân tách bởi dấu phẩy (logic OR: có ít nhất 1 quyền là hợp lệ).
    /// </summary>
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
        private readonly string[] _permissionCodes;
        private readonly IPermissionService _permissionService;

        public KiemTraQuyenFilter(string permissionCode, IPermissionService permissionService)
        {
            _permissionCodes = (permissionCode ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            _permissionService = permissionService;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var http = context.HttpContext;
            var user = http.User;
            bool laAjax = LaYeuCauAjax(http.Request);

            if (!user.Identity?.IsAuthenticated ?? true)
            {
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
            if (!int.TryParse(userIdClaim, out int userId) || userId <= 0)
            {
                context.Result = laAjax
                    ? JsonLoi(401, "Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại.")
                    : new RedirectToActionResult("Login", "Auth", null);
                return;
            }

            bool hasPermission = _permissionCodes.Length == 1
                ? await _permissionService.HasPermissionAsync(userId, _permissionCodes[0])
                : await _permissionService.HasAnyPermissionAsync(userId, _permissionCodes);

            if (!hasPermission)
            {
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
