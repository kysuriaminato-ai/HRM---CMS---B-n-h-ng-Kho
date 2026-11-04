using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ERP_HRM_CMS.Data;

namespace ERP_HRM_CMS.Services.Security
{
    /// <summary>
    /// Thu hồi phiên ngay khi tài khoản thay đổi: so SecurityStamp trong cookie với database
    /// trên mỗi request, và từ chối phiên nếu tài khoản đã bị khóa hoặc bị xóa.
    /// Cookie không có claim SecurityStamp (phát hành trước khi có tính năng này) cũng bị từ chối.
    /// </summary>
    public static class CookieSecurityStampValidator
    {
        public const string ClaimSecurityStamp = "SecurityStamp";

        public static async Task ValidateAsync(CookieValidatePrincipalContext context)
        {
            var userIdValue = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            var stampClaim = context.Principal?.FindFirstValue(ClaimSecurityStamp);

            if (!int.TryParse(userIdValue, out int userId) || string.IsNullOrEmpty(stampClaim))
            {
                await ThuHoiPhienAsync(context);
                return;
            }

            var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            var user = await db.Users.AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new { u.IsActive, u.SecurityStamp })
                .FirstOrDefaultAsync(context.HttpContext.RequestAborted);

            if (user == null
                || !user.IsActive
                || !string.Equals(user.SecurityStamp.ToString(), stampClaim, StringComparison.OrdinalIgnoreCase))
            {
                await ThuHoiPhienAsync(context);
            }
        }

        private static async Task ThuHoiPhienAsync(CookieValidatePrincipalContext context)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}
