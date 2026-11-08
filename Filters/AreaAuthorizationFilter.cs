using System.Security.Claims;
using ERP_HRM_CMS.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ERP_HRM_CMS.Filters
{
    /// <summary>
    /// Phân quyền cấp Khu vực (Area) theo yêu cầu Phiên C2 (mục C1–C4):
    /// - Shop: công khai (trừ các trang tài khoản có [Authorize]).
    /// - Admin: chỉ SUPERADMIN hoặc người có quyền quản trị hệ thống; chặn tuyệt đối CUSTOMER.
    /// - Staff: phải đăng nhập và không phải CUSTOMER; quyền từng action giữ nguyên [KiemTraQuyen].
    /// </summary>
    public sealed class AreaAuthorizationFilter : IAsyncAuthorizationFilter
    {
        private static readonly string[] AdminAreaPermissions =
        {
            "auth.user.manage",
            "auth.user.resetpassword",
            "auth.matrix.view",
            "auth.matrix.manage",
            "auth.view",
            "auth.manage",
            "content.view",
            "content.create",
            "content.edit",
            "content.delete",
            "content.manage",
            "product.view",
            "product.manage",
            "menu.view",
            "menu.manage",
            "layout.manage"
        };

        private readonly IPermissionService _permissionService;

        public AreaAuthorizationFilter(IPermissionService permissionService)
        {
            _permissionService = permissionService;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var area = context.RouteData.Values["area"]?.ToString();
            if (string.IsNullOrEmpty(area) || string.Equals(area, "Shop", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (context.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any())
            {
                return;
            }

            var http = context.HttpContext;
            var user = http.User;
            bool laAjax = LaYeuCauAjax(http.Request);

            if (!user.Identity?.IsAuthenticated ?? true)
            {
                context.Result = laAjax
                    ? JsonLoi(401, "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.")
                    : new RedirectToActionResult("Login", "Auth", new { area = "Shop" });
                return;
            }

            bool isSuperAdmin = user.IsInRole("SUPERADMIN")
                || user.IsInRole("SuperAdmin")
                || string.Equals(user.FindFirst("IsSuperAdmin")?.Value, "True", StringComparison.OrdinalIgnoreCase);

            if (isSuperAdmin)
            {
                return;
            }

            var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            bool isCustomerOnly = roles.Count > 0 && roles.All(r => string.Equals(r, "CUSTOMER", StringComparison.OrdinalIgnoreCase));

            // C2 & C4: CUSTOMER tuyệt đối không vào được Admin và Staff
            if (isCustomerOnly)
            {
                context.Result = laAjax
                    ? JsonLoi(403, "Tài khoản khách hàng không được phép truy cập khu vực nội bộ.")
                    : new RedirectToActionResult("KhongCoQuyen", "Home", new { area = string.Empty });
                return;
            }

            // C1: Khu vực Admin chỉ cho phép SUPERADMIN hoặc người có quyền quản trị
            if (string.Equals(area, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out int userId) || userId <= 0)
                {
                    context.Result = laAjax
                        ? JsonLoi(401, "Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại.")
                        : new RedirectToActionResult("Login", "Auth", new { area = "Shop" });
                    return;
                }

                bool hasAdminPermission = await _permissionService.HasAnyPermissionAsync(userId, AdminAreaPermissions);
                if (!hasAdminPermission)
                {
                    context.Result = laAjax
                        ? JsonLoi(403, "Bạn không có quyền truy cập khu vực quản trị hệ thống.")
                        : new RedirectToActionResult("KhongCoQuyen", "Home", new { area = string.Empty });
                }
            }
        }

        private static IActionResult JsonLoi(int statusCode, string message)
        {
            return new JsonResult(new { success = false, message }) { StatusCode = statusCode };
        }

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

