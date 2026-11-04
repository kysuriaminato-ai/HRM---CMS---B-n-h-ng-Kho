using Microsoft.EntityFrameworkCore;
using ERP_HRM_CMS.Data;

namespace ERP_HRM_CMS.Services.Security
{
    /// <summary>
    /// Kiểm tra quyền SuperAdmin trực tiếp từ database. Không dựa vào claim của cookie,
    /// vì claim có thể cũ nếu tài khoản vừa bị đổi quyền.
    /// </summary>
    public interface ISuperAdminService
    {
        /// <summary>Tài khoản đang hoạt động và có cờ IsSuperAdmin.</summary>
        Task<bool> LaSuperAdminAsync(int userId, CancellationToken cancellationToken = default);

        /// <summary>Tài khoản thuộc nhóm quản trị tổng: cờ IsSuperAdmin hoặc vai trò SUPERADMIN.</summary>
        Task<bool> LaTaiKhoanQuanTriTongAsync(int userId, CancellationToken cancellationToken = default);
    }

    public class SuperAdminService : ISuperAdminService
    {
        public const string MaVaiTroSuperAdmin = "SUPERADMIN";

        private readonly AppDbContext _context;

        public SuperAdminService(AppDbContext context)
        {
            _context = context;
        }

        public Task<bool> LaSuperAdminAsync(int userId, CancellationToken cancellationToken = default)
        {
            return _context.Users.AsNoTracking()
                .AnyAsync(u => u.Id == userId && u.IsActive && u.IsSuperAdmin, cancellationToken);
        }

        public Task<bool> LaTaiKhoanQuanTriTongAsync(int userId, CancellationToken cancellationToken = default)
        {
            return _context.Users.AsNoTracking()
                .AnyAsync(u => u.Id == userId
                    && (u.IsSuperAdmin || u.UserRoles.Any(ur => ur.Role.Code == MaVaiTroSuperAdmin)),
                    cancellationToken);
        }
    }
}
