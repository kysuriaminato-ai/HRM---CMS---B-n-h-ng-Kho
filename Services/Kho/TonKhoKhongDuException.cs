namespace ERP_HRM_CMS.Services.Kho
{
    /// <summary>Ném khi trừ tồn thất bại vì không còn đủ hàng (đã có giao dịch khác xuất trước).</summary>
    public sealed class TonKhoKhongDuException : Exception
    {
        public TonKhoKhongDuException(string message) : base(message) { }
    }
}
