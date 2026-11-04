using ERP_HRM_CMS.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ERP_HRM_CMS.Tests
{
    public class LoginThrottleTests : IClassFixture<TestAppFactory>
    {
        private static readonly DateTime T0 = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private const string Ip = "10.0.0.1";

        private readonly TestAppFactory _factory;

        public LoginThrottleTests(TestAppFactory factory) => _factory = factory;

        private static string NewUsername() => "t_" + Guid.NewGuid().ToString("N")[..12];

        [Fact]
        public async Task Five_Failures_Lock_The_Pair_For_One_Minute()
        {
            using var scope = _factory.Services.CreateScope();
            var throttle = scope.ServiceProvider.GetRequiredService<LoginThrottleService>();
            var user = NewUsername();

            for (int i = 0; i < 4; i++) await throttle.RecordFailureAsync(user, Ip, T0);
            Assert.Null(await throttle.GetRemainingLockoutAsync(user, Ip, T0));

            await throttle.RecordFailureAsync(user, Ip, T0);
            Assert.Equal(TimeSpan.FromMinutes(1), await throttle.GetRemainingLockoutAsync(user, Ip, T0));
        }

        [Fact]
        public async Task Lockout_Expires_And_Is_Not_Permanent()
        {
            using var scope = _factory.Services.CreateScope();
            var throttle = scope.ServiceProvider.GetRequiredService<LoginThrottleService>();
            var user = NewUsername();

            for (int i = 0; i < 5; i++) await throttle.RecordFailureAsync(user, Ip, T0);

            Assert.NotNull(await throttle.GetRemainingLockoutAsync(user, Ip, T0.AddSeconds(30)));
            Assert.Null(await throttle.GetRemainingLockoutAsync(user, Ip, T0.AddMinutes(1).AddSeconds(1)));
        }

        [Fact]
        public async Task Repeated_Lockouts_Double_The_Duration_Up_To_The_Cap()
        {
            using var scope = _factory.Services.CreateScope();
            var throttle = scope.ServiceProvider.GetRequiredService<LoginThrottleService>();
            var user = NewUsername();

            // Lần khóa thứ nhất: 1 phút
            for (int i = 0; i < 5; i++) await throttle.RecordFailureAsync(user, Ip, T0);
            var t1 = T0.AddMinutes(2);

            // Lần khóa thứ hai: 2 phút
            for (int i = 0; i < 5; i++) await throttle.RecordFailureAsync(user, Ip, t1);
            Assert.Equal(TimeSpan.FromMinutes(2), await throttle.GetRemainingLockoutAsync(user, Ip, t1));
        }

        [Fact]
        public void LockoutFor_Grows_Exponentially_And_Is_Capped_At_Thirty_Minutes()
        {
            Assert.Equal(TimeSpan.FromMinutes(1), LoginThrottleService.LockoutFor(1));
            Assert.Equal(TimeSpan.FromMinutes(2), LoginThrottleService.LockoutFor(2));
            Assert.Equal(TimeSpan.FromMinutes(4), LoginThrottleService.LockoutFor(3));
            Assert.Equal(TimeSpan.FromMinutes(30), LoginThrottleService.LockoutFor(10));
        }

        [Fact]
        public async Task Lockout_Is_Per_Ip_So_Other_Addresses_Are_Not_Blocked()
        {
            using var scope = _factory.Services.CreateScope();
            var throttle = scope.ServiceProvider.GetRequiredService<LoginThrottleService>();
            var user = NewUsername();

            for (int i = 0; i < 5; i++) await throttle.RecordFailureAsync(user, Ip, T0);

            Assert.NotNull(await throttle.GetRemainingLockoutAsync(user, Ip, T0));
            Assert.Null(await throttle.GetRemainingLockoutAsync(user, "10.0.0.2", T0));
        }

        [Fact]
        public async Task Successful_Login_Clears_The_Counter()
        {
            using var scope = _factory.Services.CreateScope();
            var throttle = scope.ServiceProvider.GetRequiredService<LoginThrottleService>();
            var user = NewUsername();

            for (int i = 0; i < 4; i++) await throttle.RecordFailureAsync(user, Ip, T0);
            await throttle.ClearAsync(user, Ip);

            // Sau khi xóa, bốn lần sai nữa vẫn chưa đủ ngưỡng
            for (int i = 0; i < 4; i++) await throttle.RecordFailureAsync(user, Ip, T0);
            Assert.Null(await throttle.GetRemainingLockoutAsync(user, Ip, T0));
        }

        [Fact]
        public async Task Counter_Resets_After_A_Quiet_Period()
        {
            using var scope = _factory.Services.CreateScope();
            var throttle = scope.ServiceProvider.GetRequiredService<LoginThrottleService>();
            var user = NewUsername();

            for (int i = 0; i < 4; i++) await throttle.RecordFailureAsync(user, Ip, T0);

            // Sau hơn 24 giờ không sai, bộ đếm bắt đầu lại từ đầu
            var later = T0.AddHours(25);
            await throttle.RecordFailureAsync(user, Ip, later);
            Assert.Null(await throttle.GetRemainingLockoutAsync(user, Ip, later));
        }
    }
}
