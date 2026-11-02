using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace ERP_HRM_CMS.Hubs
{
    // Chỉ người đã đăng nhập mới được kết nối. Nhóm USER_/ROLE_ được server gán dựa trên claim,
    // client không tự chọn nhóm (xem bỏ JoinRoleGroup bên dưới).
    [Authorize]
    public class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var user = Context.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"USER_{userId}");
                }

                // Thêm vào các nhóm Role
                var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value.ToUpper()).ToList();
                foreach (var role in roles)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"ROLE_{role}");
                }

                // Nhóm toàn thể nhân viên công ty
                await Groups.AddToGroupAsync(Context.ConnectionId, "COMPANY_ALL");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}
