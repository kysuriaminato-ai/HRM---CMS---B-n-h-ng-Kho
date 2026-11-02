using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ERP_HRM_CMS.Services;

namespace ERP_HRM_CMS.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        private int? CurrentUserId
        {
            get
            {
                var val = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(val, out int id)) return id;
                return HttpContext.Session.GetInt32("UserId");
            }
        }

        private string? CurrentUserRole
        {
            get
            {
                return User.FindFirst(ClaimTypes.Role)?.Value ?? HttpContext.Session.GetString("Role");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var list = await _notificationService.GetUserRecentNotificationsAsync(CurrentUserId, CurrentUserRole, 50);
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> GetRecentNotifications()
        {
            var list = await _notificationService.GetUserRecentNotificationsAsync(CurrentUserId, CurrentUserRole, 8);
            var unreadCount = await _notificationService.GetUnreadCountAsync(CurrentUserId, CurrentUserRole);

            return Json(new
            {
                success = true,
                unreadCount,
                items = list.Select(n => new
                {
                    id = n.Id,
                    title = n.Title,
                    content = n.Content,
                    linkUrl = n.LinkUrl,
                    type = n.Type,
                    isRead = n.IsRead,
                    createdAt = n.CreatedAt.ToString("HH:mm dd/MM/yyyy")
                })
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(long id)
        {
            var success = await _notificationService.MarkAsReadAsync(id, CurrentUserId, CurrentUserRole);
            return Json(new { success });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var success = await _notificationService.MarkAllAsReadAsync(CurrentUserId, CurrentUserRole);
            return Json(new { success });
        }
    }
}
