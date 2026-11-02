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
            var user = context.HttpContext.User;
            if (!user.Identity?.IsAuthenticated ?? true)
            {
                context.Result = new RedirectToActionResult("Login", "Auth", null);
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
                context.Result = new RedirectToActionResult("Login", "Auth", null);
                return;
            }

            bool hasPermission = await _permissionService.HasPermissionAsync(userId, _permissionCode);
            if (!hasPermission)
            {
                // Điều hướng về trang báo lỗi không có quyền
                context.Result = new RedirectToActionResult("KhongCoQuyen", "Home", null);
            }
        }
    }
}
