using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Models.Entities;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ERP_HRM_CMS.Tests
{
    /// <summary>
    /// Kiểm tra quy đổi múi giờ Asia/Ho_Chi_Minh (UTC+7) và phạm vi cột được đổi múi giờ.
    /// Các test không cần kết nối database.
    /// </summary>
    public class VietnamTimeTests
    {
        [Fact]
        public void Utc_Midnight_Is_Seven_Am_In_Vietnam()
        {
            var local = VietnamTime.FromUtc(new DateTime(2026, 11, 3, 0, 0, 0, DateTimeKind.Utc));

            Assert.Equal(new DateTime(2026, 11, 3, 7, 0, 0), local);
        }

        [Fact]
        public void Vietnam_Midnight_Is_Previous_Day_17h_In_Utc()
        {
            var utc = VietnamTime.ToUtc(new DateTime(2026, 11, 3, 0, 0, 0));

            Assert.Equal(new DateTime(2026, 11, 2, 17, 0, 0), utc);
        }

        [Fact]
        public void Round_Trip_Keeps_The_Same_Instant()
        {
            var original = new DateTime(2026, 11, 3, 23, 59, 59);

            Assert.Equal(original, VietnamTime.FromUtc(VietnamTime.ToUtc(original)));
        }

        [Fact]
        public void Min_And_Max_Values_Are_Not_Shifted()
        {
            Assert.Equal(DateTime.MinValue, VietnamTime.ToUtc(DateTime.MinValue));
            Assert.Equal(DateTime.MaxValue, VietnamTime.FromUtc(DateTime.MaxValue));
        }

        [Fact]
        public void Now_Is_Utc_Plus_Seven_Hours()
        {
            var difference = VietnamTime.Now - DateTime.UtcNow.AddHours(7);

            Assert.True(Math.Abs(difference.TotalSeconds) < 5, $"Lệch {difference.TotalSeconds} giây");
        }

        [Fact]
        public void Converter_Stores_Utc_And_Reads_Vietnam_Time()
        {
            var converter = new VietnamTimeConverter();
            var vietnamTime = new DateTime(2026, 11, 3, 8, 30, 0);

            var stored = converter.ConvertToProvider(vietnamTime);

            Assert.Equal(new DateTime(2026, 11, 3, 1, 30, 0), stored);
            Assert.Equal(vietnamTime, converter.ConvertFromProvider(stored));
        }

        [Fact]
        public void Nullable_Converter_Keeps_Null()
        {
            var converter = new NullableVietnamTimeConverter();

            Assert.Null(converter.ConvertToProvider(null));
            Assert.Null(converter.ConvertFromProvider(null));
        }

        [Theory]
        [InlineData("CreatedAt", true)]
        [InlineData("ReadAt", true)]
        [InlineData("ReceivedDate", true)]
        [InlineData("PaymentDate", true)]
        [InlineData("CheckDate", true)]
        [InlineData("DueDate", false)]
        [InlineData("FromDate", false)]
        [InlineData("WorkDate", false)]
        [InlineData("OrderDate", false)]
        [InlineData("LockedUntilUtc", false)]
        [InlineData("LastFailedUtc", false)]
        public void Only_Instant_Columns_Are_Converted(string propertyName, bool expected)
        {
            Assert.Equal(expected, AppDbContext.IsInstantProperty(propertyName));
        }

        [Fact]
        public void Model_Converts_Instant_Columns_But_Not_Calendar_Dates()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer("Server=localhost;Database=ERP_HRM_CMS_MODEL_ONLY;Trusted_Connection=True;TrustServerCertificate=True;")
                .Options;
            using var db = new AppDbContext(options, new EphemeralDataProtectionProvider());

            var order = db.Model.FindEntityType(typeof(Order))!;
            Assert.NotNull(order.FindProperty(nameof(Order.CreatedAt))!.GetValueConverter());

            var debt = db.Model.FindEntityType(typeof(DebtRecord))!;
            Assert.Null(debt.FindProperty(nameof(DebtRecord.DueDate))!.GetValueConverter());

            var leave = db.Model.FindEntityType(typeof(LeaveRequest))!;
            Assert.Null(leave.FindProperty(nameof(LeaveRequest.FromDate))!.GetValueConverter());
            Assert.NotNull(leave.FindProperty(nameof(LeaveRequest.CreatedAt))!.GetValueConverter());
        }
    }
}
