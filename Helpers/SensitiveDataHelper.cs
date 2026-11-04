using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ERP_HRM_CMS.Helpers
{
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

    /// <summary>Che số khi hiển thị: "****1234". Giữ nguyên null; giá trị quá ngắn chỉ hiện "****".</summary>
    public static class SensitiveDataMasker
    {
        public static string? Mask(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var trimmed = value.Trim();
            return trimmed.Length <= 4 ? "****" : "****" + trimmed.Substring(trimmed.Length - 4);
        }
    }
}
