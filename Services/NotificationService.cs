using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Hubs;
using ERP_HRM_CMS.Models.Entities;

namespace ERP_HRM_CMS.Services
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(AppDbContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task<Notification> SendToUserAsync(int userId, string title, string content, string? linkUrl = null, string type = "SYSTEM")
        {
            var notif = new Notification
            {
                ReceiverUserId = userId,
                Title = title,
                Content = content,
                LinkUrl = linkUrl,
                Type = type,
                IsRead = false,
                CreatedAt = DateTime.Now
            };

            _context.Notifications.Add(notif);
            await _context.SaveChangesAsync();

            await _hubContext.Clients.Group($"USER_{userId}").SendAsync("ReceiveNotification", new
            {
                id = notif.Id,
                title = notif.Title,
                content = notif.Content,
                linkUrl = notif.LinkUrl,
                type = notif.Type,
                createdAt = notif.CreatedAt.ToString("HH:mm dd/MM/yyyy"),
                isRead = notif.IsRead
            });

            return notif;
        }

        public async Task<Notification> SendToRoleAsync(string roleName, string title, string content, string? linkUrl = null, string type = "SYSTEM")
        {
            string normalizedRole = roleName.ToUpper().Trim();
            var notif = new Notification
            {
                TargetRole = normalizedRole,
                Title = title,
                Content = content,
                LinkUrl = linkUrl,
                Type = type,
                IsRead = false,
                CreatedAt = DateTime.Now
            };

            _context.Notifications.Add(notif);
            await _context.SaveChangesAsync();

            // Gửi tới group của role đó và SUPERADMIN (luôn nhận cảnh báo)
            await _hubContext.Clients.Groups(new[] { $"ROLE_{normalizedRole}", "ROLE_SUPERADMIN" }).SendAsync("ReceiveNotification", new
            {
                id = notif.Id,
                title = notif.Title,
                content = notif.Content,
                linkUrl = notif.LinkUrl,
                type = notif.Type,
                createdAt = notif.CreatedAt.ToString("HH:mm dd/MM/yyyy"),
                isRead = notif.IsRead
            });

            return notif;
        }

        public async Task<Notification> BroadcastAsync(string title, string content, string? linkUrl = null, string type = "SYSTEM")
        {
            var notif = new Notification
            {
                Title = title,
                Content = content,
                LinkUrl = linkUrl,
                Type = type,
                IsRead = false,
                CreatedAt = DateTime.Now
            };

            _context.Notifications.Add(notif);
            await _context.SaveChangesAsync();

            await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
            {
                id = notif.Id,
                title = notif.Title,
                content = notif.Content,
                linkUrl = notif.LinkUrl,
                type = notif.Type,
                createdAt = notif.CreatedAt.ToString("HH:mm dd/MM/yyyy"),
                isRead = notif.IsRead
            });

            return notif;
        }

        /// <summary>
        /// Quy tắc hiển thị thông báo (dùng chung cho danh sách, số chưa đọc, đánh dấu đã đọc):
        /// - Thông báo cá nhân (ReceiverUserId khác null): chỉ người nhận được xem.
        /// - Thông báo theo vai trò (TargetRole, không có người nhận): xem được nếu đúng vai trò, hoặc là SUPERADMIN.
        /// - Thông báo toàn hệ thống (không có người nhận, không có vai trò): ai cũng xem được.
        /// Không còn điều kiện "TargetRole == null" cho thông báo cá nhân, nên không rò rỉ thông báo giữa các người dùng.
        /// </summary>
        private static IQueryable<Notification> ApplyVisibility(IQueryable<Notification> query, int? userId, string? normalizedRole)
        {
            bool hasRole = !string.IsNullOrEmpty(normalizedRole);
            bool isSuperAdmin = normalizedRole == "SUPERADMIN";

            if (userId.HasValue)
            {
                int uid = userId.Value;
                return query.Where(n => n.ReceiverUserId == uid
                    || (n.ReceiverUserId == null
                        && (n.TargetRole == null
                            || (hasRole && (n.TargetRole == normalizedRole || isSuperAdmin)))));
            }

            if (hasRole)
            {
                return query.Where(n => n.ReceiverUserId == null
                    && (n.TargetRole == null || n.TargetRole == normalizedRole || isSuperAdmin));
            }

            return query.Where(n => n.ReceiverUserId == null && n.TargetRole == null);
        }

        /// <summary>
        /// Chưa đọc theo từng người dùng: thông báo cá nhân dùng cột IsRead;
        /// thông báo chung (vai trò hoặc toàn hệ thống) dùng bảng NotificationReads để mỗi người đọc riêng.
        /// </summary>
        private IQueryable<Notification> UnreadFor(int? userId, string? normalizedRole)
        {
            int uid = userId ?? int.MinValue;
            return ApplyVisibility(_context.Notifications, userId, normalizedRole).Where(n =>
                (n.ReceiverUserId != null && !n.IsRead) ||
                (n.ReceiverUserId == null && !_context.NotificationReads.Any(r => r.NotificationId == n.Id && r.UserId == uid)));
        }

        public async Task<List<Notification>> GetUserRecentNotificationsAsync(int? userId, string? roleName, int take = 10)
        {
            string? normalizedRole = roleName?.ToUpper().Trim();

            var list = await ApplyVisibility(_context.Notifications.AsNoTracking(), userId, normalizedRole)
                .OrderByDescending(n => n.CreatedAt)
                .Take(take)
                .ToListAsync();

            // Gắn trạng thái đọc riêng của người dùng cho thông báo chung (chỉ trong bộ nhớ, không ghi vào entity)
            if (userId.HasValue)
            {
                var sharedIds = list.Where(n => n.ReceiverUserId == null).Select(n => n.Id).ToList();
                if (sharedIds.Count > 0)
                {
                    int uid = userId.Value;
                    var readIds = await _context.NotificationReads.AsNoTracking()
                        .Where(r => r.UserId == uid && sharedIds.Contains(r.NotificationId))
                        .Select(r => r.NotificationId)
                        .ToListAsync();

                    foreach (var n in list.Where(n => n.ReceiverUserId == null && readIds.Contains(n.Id)))
                    {
                        n.IsRead = true;
                    }
                }
            }

            return list;
        }

        public async Task<int> GetUnreadCountAsync(int? userId, string? roleName)
        {
            string? normalizedRole = roleName?.ToUpper().Trim();
            return await UnreadFor(userId, normalizedRole).CountAsync();
        }

        public async Task<bool> MarkAsReadAsync(long notificationId, int? userId, string? roleName)
        {
            string? normalizedRole = roleName?.ToUpper().Trim();

            // Chỉ đánh dấu khi thông báo nằm trong phạm vi người dùng được xem (chống IDOR)
            var notif = await ApplyVisibility(_context.Notifications.Where(n => n.Id == notificationId), userId, normalizedRole)
                .FirstOrDefaultAsync();
            if (notif == null) return false;

            if (notif.ReceiverUserId != null)
            {
                notif.IsRead = true;
            }
            else
            {
                await MarkSharedAsReadAsync(new[] { notif.Id }, userId);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkAllAsReadAsync(int? userId, string? roleName)
        {
            string? normalizedRole = roleName?.ToUpper().Trim();

            var unread = await UnreadFor(userId, normalizedRole).ToListAsync();
            foreach (var n in unread.Where(n => n.ReceiverUserId != null))
            {
                n.IsRead = true;
            }
            await MarkSharedAsReadAsync(unread.Where(n => n.ReceiverUserId == null).Select(n => n.Id), userId);

            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>Ghi trạng thái đã đọc riêng cho người dùng với các thông báo chung (bỏ qua bản ghi đã có).</summary>
        private async Task MarkSharedAsReadAsync(IEnumerable<long> notificationIds, int? userId)
        {
            if (!userId.HasValue) return;

            int uid = userId.Value;
            var ids = notificationIds.Distinct().ToList();
            if (ids.Count == 0) return;

            var alreadyRead = await _context.NotificationReads
                .Where(r => r.UserId == uid && ids.Contains(r.NotificationId))
                .Select(r => r.NotificationId)
                .ToListAsync();

            foreach (var id in ids.Except(alreadyRead))
            {
                _context.NotificationReads.Add(new NotificationRead
                {
                    NotificationId = id,
                    UserId = uid,
                    ReadAt = DateTime.Now
                });
            }
        }
    }
}
