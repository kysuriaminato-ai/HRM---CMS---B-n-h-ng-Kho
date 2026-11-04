using ERP_HRM_CMS.Models.Enums;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ERP_HRM_CMS.Helpers
{
    /// <summary>
    /// Lưu enum trạng thái nghiệp vụ dưới dạng mã chuỗi (giữ nguyên mã cũ trong database)
    /// và đọc ngược lại thành enum. Mã lạ trong database được báo lỗi rõ ràng, không bị bỏ qua âm thầm.
    /// </summary>
    public sealed class WorkflowStatusConverter<TEnum> : ValueConverter<TEnum, string>
        where TEnum : struct, Enum
    {
        public WorkflowStatusConverter(IReadOnlyDictionary<TEnum, string> codes)
            : base(
                status => WorkflowStatusCodes.ToCode(codes, status),
                code => ReadCode(codes, code))
        {
        }

        internal static TEnum ReadCode(IReadOnlyDictionary<TEnum, string> codes, string code)
        {
            if (WorkflowStatusCodes.TryParse(codes, code, out var status)) return status;
            throw new InvalidOperationException($"Mã trạng thái không hợp lệ trong database: '{code}'.");
        }
    }
}
