namespace ERP.Application.Common.Interfaces;

/// <summary>
/// Cung cấp thông tin người dùng hiện hành đọc trực tiếp từ Claims xác thực (Cookie/Principal).
/// Tuyệt đối không dùng fallback giả mạo kiểu Session.GetInt32("UserId") ?? 1.
/// </summary>
public interface ICurrentUserService
{
    bool IsAuthenticated { get; }
    int? UserId { get; }
    int GetRequiredUserId();
    string? Username { get; }
    string? FullName { get; }
    int? EmployeeId { get; }
    int? BranchId { get; }
    int? DepartmentId { get; }
    IReadOnlyList<string> Roles { get; }
    bool IsSuperAdmin { get; }
    bool MustChangePassword { get; }
    string? IpAddress { get; }
    bool IsInRole(string roleCode);
}

/// <summary>
/// Tên chuẩn của các Custom Claims dùng xuyên suốt giữa AuthController, CookieSecurityStampValidator và CurrentUserService.
/// </summary>
public static class ErpClaimTypes
{
    public const string FullName = "FullName";
    public const string IsSuperAdmin = "IsSuperAdmin";
    public const string SecurityStamp = "SecurityStamp";
    public const string EmployeeId = "EmployeeId";
    public const string BranchId = "BranchId";
    public const string DepartmentId = "DepartmentId";
    public const string MustChangePassword = "MustChangePassword";
}

