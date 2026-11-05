using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ERP_HRM_CMS.Helpers
{
    /// <summary>Lưu cột thời điểm (không null) dưới dạng UTC, đọc lại thành giờ Việt Nam.</summary>
    public sealed class VietnamTimeConverter : ValueConverter<DateTime, DateTime>
    {
        public VietnamTimeConverter()
            : base(
                value => VietnamTime.ToUtc(value),
                value => VietnamTime.FromUtc(value))
        {
        }
    }

    /// <summary>Lưu cột thời điểm có thể null dưới dạng UTC, đọc lại thành giờ Việt Nam.</summary>
    public sealed class NullableVietnamTimeConverter : ValueConverter<DateTime?, DateTime?>
    {
        public NullableVietnamTimeConverter()
            : base(
                value => value.HasValue ? (DateTime?)VietnamTime.ToUtc(value.Value) : null,
                value => value.HasValue ? (DateTime?)VietnamTime.FromUtc(value.Value) : null)
        {
        }
    }

    /// <summary>
    /// Mã hóa dữ liệu nhạy cảm (số CCCD, số tài khoản ngân hàng) khi ghi xuống database.
    /// Giá trị đã mã hóa có tiền tố "enc:v1:". Giá trị không có tiền tố được coi là dữ liệu cũ
    /// (chưa mã hóa) và được trả nguyên; lệnh migration sẽ mã hóa lại toàn bộ.
    /// Khóa mã hóa do ASP.NET Core Data Protection quản lý (xem DataProtection:KeysPath).
    /// </summary>
    public sealed class ProtectedStringConverter : ValueConverter<string?, string?>
    {
        public const string Prefix = "enc:v1:";
        public const string Purpose = "ERP_HRM_CMS.SensitiveData.v1";

        public ProtectedStringConverter(IDataProtector protector)
            : base(
                v => ToStore(protector, v),
                v => FromStore(protector, v))
        {
        }

        public static IDataProtector CreateProtector(IDataProtectionProvider provider) =>
            provider.CreateProtector(Purpose);

        internal static string? ToStore(IDataProtector protector, string? value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return Prefix + protector.Protect(value);
        }

        internal static string? FromStore(IDataProtector protector, string? stored)
        {
            if (stored == null || !stored.StartsWith(Prefix, StringComparison.Ordinal)) return stored;
            return protector.Unprotect(stored.Substring(Prefix.Length));
        }
    }
}

