namespace ERP_HRM_CMS.Services.Printing
{
    /// <summary>
    /// [OOP - ĐA HÌNH] Hợp đồng chung cho mọi phiếu in: phiếu lương, bảng lương, hóa đơn...
    /// Nơi gọi chỉ cần biết IPrintable, không cần biết đó là loại phiếu nào.
    /// </summary>
    public interface IPrintable
    {
        string TieuDe { get; }

        /// <summary>Phần thân phiếu (không có khung trang), dùng để ghép nhiều phiếu vào một trang in.</summary>
        string RenderBody();

        /// <summary>Trang HTML hoàn chỉnh để in riêng một phiếu.</summary>
        string RenderHtml();
    }
}
