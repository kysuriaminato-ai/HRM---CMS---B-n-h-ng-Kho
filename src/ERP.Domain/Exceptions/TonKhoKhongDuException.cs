using ERP.Domain.Exceptions;

namespace ERP_HRM_CMS.Services.Kho
{
    /// <summary>Ném khi trừ tồn thất bại vì không còn đủ hàng (đã có giao dịch khác xuất trước).</summary>
    public sealed class TonKhoKhongDuException : DomainException
    {
        public TonKhoKhongDuException(string message) : base(message, "INSUFFICIENT_STOCK") { }
    }
}

