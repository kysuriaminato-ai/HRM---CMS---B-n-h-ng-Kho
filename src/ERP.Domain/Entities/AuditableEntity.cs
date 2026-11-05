namespace ERP_HRM_CMS.Models.Entities
{
    /// <summary>
    /// Interface kiểm toán — tất cả entity nghiệp vụ phải implement.
    /// Cung cấp cột: IsDeleted, UpdatedAt, UpdatedBy, CreatedBy.
    /// CreatedAt đã có sẵn trên các entity hiện tại.
    /// </summary>
    public interface IAuditable
    {
        bool IsDeleted { get; set; }
        DateTime? UpdatedAt { get; set; }
        string? UpdatedBy { get; set; }
        string? CreatedBy { get; set; }
    }
}
