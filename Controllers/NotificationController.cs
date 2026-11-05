using ERP.Application.Common.Interfaces;
using ERP_HRM_CMS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP_HRM_CMS.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly INotificationService _notificationService;
        private readonly ICurrentUserService _currentUser;

        public NotificationController(INotificationService notificationService, ICurrentUserService currentUser)
        {
            _notificationService = notificationService;
            _currentUser = currentUser;
        }

        private int? CurrentUserId => _currentUser.UserId;

        private string? CurrentUserRole => _currentUser.Roles.FirstOrDefault();

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
