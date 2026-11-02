using ERP_HRM_CMS.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ERP_HRM_CMS.Tests
{
    public class NotificationTests : IClassFixture<TestAppFactory>
    {
        private readonly TestAppFactory _factory;

        public NotificationTests(TestAppFactory factory) => _factory = factory;

        private INotificationService NewService(IServiceScope scope) =>
            scope.ServiceProvider.GetRequiredService<INotificationService>();

        [Fact]
        public async Task PersonalNotification_IsNotVisibleToOtherUsers()
        {
            var alice = await TestHelpers.CreateUserAsync(_factory.Services);
            var bob = await TestHelpers.CreateUserAsync(_factory.Services);
            var title = "Riêng tư " + Guid.NewGuid();

            using (var scope = _factory.Services.CreateScope())
            {
                await NewService(scope).SendToUserAsync(alice, title, "nội dung");
            }

            using var check = _factory.Services.CreateScope();
            var service = NewService(check);
            var forAlice = await service.GetUserRecentNotificationsAsync(alice, null, 100);
            var forBob = await service.GetUserRecentNotificationsAsync(bob, null, 100);

            Assert.Contains(forAlice, n => n.Title == title);
            Assert.DoesNotContain(forBob, n => n.Title == title);
        }

        [Fact]
        public async Task MarkAsRead_OnAnotherUsersNotification_IsRejected()
        {
            var alice = await TestHelpers.CreateUserAsync(_factory.Services);
            var bob = await TestHelpers.CreateUserAsync(_factory.Services);

            long notifId;
            using (var scope = _factory.Services.CreateScope())
            {
                var notif = await NewService(scope).SendToUserAsync(alice, "Của Alice", "nội dung");
                notifId = notif.Id;
            }

            using (var scope = _factory.Services.CreateScope())
            {
                var ok = await NewService(scope).MarkAsReadAsync(notifId, bob, null);
                Assert.False(ok);
            }

            using var check = _factory.Services.CreateScope();
            var unreadForAlice = await NewService(check).GetUnreadCountAsync(alice, null);
            Assert.True(unreadForAlice >= 1);
            var list = await NewService(check).GetUserRecentNotificationsAsync(alice, null, 100);
            Assert.Contains(list, n => n.Id == notifId && !n.IsRead);
        }

        [Fact]
        public async Task RoleNotification_ReadState_IsPerUser()
        {
            var role = "TESTROLE" + Guid.NewGuid().ToString("N")[..8];
            var userA = await TestHelpers.CreateUserAsync(_factory.Services);
            var userB = await TestHelpers.CreateUserAsync(_factory.Services);

            long notifId;
            using (var scope = _factory.Services.CreateScope())
            {
                var notif = await NewService(scope).SendToRoleAsync(role, "Thông báo vai trò", "nội dung");
                notifId = notif.Id;
            }

            int beforeA, beforeB;
            using (var scope = _factory.Services.CreateScope())
            {
                beforeA = await NewService(scope).GetUnreadCountAsync(userA, role);
                beforeB = await NewService(scope).GetUnreadCountAsync(userB, role);
            }

            using (var scope = _factory.Services.CreateScope())
            {
                Assert.True(await NewService(scope).MarkAsReadAsync(notifId, userA, role));
            }

            using var check = _factory.Services.CreateScope();
            var afterA = await NewService(check).GetUnreadCountAsync(userA, role);
            var afterB = await NewService(check).GetUnreadCountAsync(userB, role);

            Assert.Equal(beforeA - 1, afterA);
            Assert.Equal(beforeB, afterB);
        }
    }
}
