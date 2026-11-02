namespace ERP_HRM_CMS.Services.Printing
{
    public interface IPrintService
    {
        /// <summary>In một phiếu bất kỳ (đa hình qua IPrintable).</summary>
        string Render(IPrintable document);

        /// <summary>In nhiều phiếu khác loại trong một trang (đa hình qua IPrintable).</summary>
        string RenderBatch(IEnumerable<IPrintable> documents);
    }
}
