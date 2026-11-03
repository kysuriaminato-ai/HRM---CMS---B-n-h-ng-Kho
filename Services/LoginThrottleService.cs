using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP_HRM_CMS.Services
{
    /// <summary>
    /// Khóa tạm thời đăng nhập theo cặp (tên đăng nhập, IP).
    /// - Sau 5 lần sai liên tiếp: khóa 1 phút; mỗi lần khóa tiếp theo thời gian tăng gấp đôi, tối đa 30 phút.
    /// - Không khóa vĩnh viễn, kể cả tài khoản quản trị.
    /// - Sau 24 giờ không sai, bộ đếm và mức khóa được đặt lại.
    /// - Đăng nhập thành công thì xóa bộ đếm của cặp đó.
    /// Thời gian được truyền vào (utcNow) để kiểm thử được mà không phụ thuộc đồng hồ hệ thống.
    /// </summary>
    public class LoginThrottleService
    {
        public const int FailuresBeforeLockout = 5;
        public static readonly TimeSpan BaseLockout = TimeSpan.FromMinutes(1);
        public static readonly TimeSpan MaxLockout = TimeSpan.FromMinutes(30);
        public static readonly TimeSpan ForgetAfter = TimeSpan.FromHours(24);

        private readonly AppDbContext _db;

        public LoginThrottleService(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>Thời gian khóa theo mức (level >= 1): 1, 2, 4, 8, 16, rồi 30 phút.</summary>
        public static TimeSpan LockoutFor(int level)
        {
            if (level <= 1) return BaseLockout;
            double minutes = BaseLockout.TotalMinutes * Math.Pow(2, Math.Min(level - 1, 10));
            return TimeSpan.FromMinutes(Math.Min(minutes, MaxLockout.TotalMinutes));
        }

        /// <summary>Trả về thời gian còn lại của khóa, hoặc null nếu cặp này không bị khóa.</summary>
        public async Task<TimeSpan?> GetRemainingLockoutAsync(string username, string ipAddress, DateTime utcNow)
        {
            var row = await _db.LoginAttempts.AsNoTracking()
                .FirstOrDefaultAsync(a => a.Username == username && a.IpAddress == ipAddress);

            if (row?.LockedUntilUtc is DateTime until && until > utcNow)
            {
                return until - utcNow;
            }
            return null;
        }

        public async Task RecordFailureAsync(string username, string ipAddress, DateTime utcNow)
        {
            var row = await _db.LoginAttempts
                .FirstOrDefaultAsync(a => a.Username == username && a.IpAddress == ipAddress);

            if (row == null)
            {
                row = new LoginAttempt { Username = username, IpAddress = ipAddress };
                _db.LoginAttempts.Add(row);
            }

            // Lâu không sai: coi như bắt đầu lại từ đầu
            if (row.LastFailedUtc < utcNow - ForgetAfter)
            {
                row.FailedCount = 0;
                row.LockoutLevel = 0;
            }

            row.LastFailedUtc = utcNow;
            row.FailedCount++;

            if (row.FailedCount >= FailuresBeforeLockout)
            {
                row.LockoutLevel++;
                row.LockedUntilUtc = utcNow + LockoutFor(row.LockoutLevel);
                row.FailedCount = 0;
            }

            await _db.SaveChangesAsync();
        }

        public async Task ClearAsync(string username, string ipAddress)
        {
            var row = await _db.LoginAttempts
                .FirstOrDefaultAsync(a => a.Username == username && a.IpAddress == ipAddress);

            if (row != null)
            {
                _db.LoginAttempts.Remove(row);
                await _db.SaveChangesAsync();
            }
        }
    }
}
