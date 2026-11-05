namespace ERP_HRM_CMS.Helpers
{
    /// <summary>
    /// Thời gian nghiệp vụ theo múi giờ Asia/Ho_Chi_Minh.
    /// Trong mã nguồn mọi thời điểm là giờ Việt Nam (không phụ thuộc múi giờ của máy chủ).
    /// Database lưu UTC: việc chuyển đổi được thực hiện ở ranh giới EF Core (xem VietnamTimeConverter ở ERP.Infrastructure).
    /// Không dùng DateTime.Now / DateTime.Today: giá trị đó phụ thuộc máy chủ.
    /// </summary>
    public static class VietnamTime
    {
        public const string TimeZoneId = "Asia/Ho_Chi_Minh";

        private static readonly Lazy<TimeZoneInfo> Zone =
            new(() => TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId));

        public static TimeZoneInfo TimeZone => Zone.Value;

        /// <summary>Thời điểm hiện tại theo giờ Việt Nam (Kind = Unspecified).</summary>
        public static DateTime Now => FromUtc(DateTime.UtcNow);

        /// <summary>Ngày hiện tại theo giờ Việt Nam, lúc 00:00.</summary>
        public static DateTime Today => Now.Date;

        /// <summary>Đổi thời điểm UTC sang giờ Việt Nam. Giá trị MinValue/MaxValue được giữ nguyên.</summary>
        public static DateTime FromUtc(DateTime utc)
        {
            if (utc == DateTime.MinValue || utc == DateTime.MaxValue) return utc;
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), TimeZone);
        }

        /// <summary>Đổi giờ Việt Nam sang UTC để lưu. Giá trị MinValue/MaxValue được giữ nguyên.</summary>
        public static DateTime ToUtc(DateTime vietnamTime)
        {
            if (vietnamTime == DateTime.MinValue || vietnamTime == DateTime.MaxValue) return vietnamTime;
            return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(vietnamTime, DateTimeKind.Unspecified), TimeZone);
        }
    }
}

