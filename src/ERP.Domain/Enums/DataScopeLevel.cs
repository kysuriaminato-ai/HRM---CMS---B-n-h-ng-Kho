namespace ERP.Domain.Enums;

/// <summary>
/// Cấp độ phạm vi dữ liệu doanh nghiệp (Enterprise Data Scope).
/// Thứ tự từ rộng đến hẹp: Company -> Branch -> Department -> Self -> None.
/// </summary>
public enum DataScopeLevel
{
    /// <summary>Không có quyền truy cập dữ liệu (chưa xác thực hoặc bị từ chối).</summary>
    None = 0,

    /// <summary>Phạm vi cá nhân: chỉ thao tác và nhìn thấy dữ liệu của chính nhân viên đăng nhập.</summary>
    Self = 1,

    /// <summary>Phạm vi phòng ban: Trưởng phòng chỉ nhìn thấy và quản lý nhân sự thuộc phòng ban mình.</summary>
    Department = 2,

    /// <summary>Phạm vi chi nhánh: Quản lý chi nhánh / kho / bán hàng chỉ thấy dữ liệu chi nhánh mình.</summary>
    Branch = 3,

    /// <summary>Phạm vi toàn công ty: SuperAdmin &amp; HR_MANAGER tổng công ty / Ban Giám đốc thấy toàn bộ.</summary>
    Company = 4
}

