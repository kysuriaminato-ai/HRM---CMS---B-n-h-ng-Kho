using System.Security.Claims;
using ERP.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace ERP.Infrastructure.Identity;

/// <summary>
/// Đọc danh tính và phạm vi người dùng trực tiếp từ ClaimsPrincipal đã xác thực.
/// Không bao giờ lấy từ Session hoặc rơi về UserId = 1.
/// </summary>
public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public int? UserId
    {
        get
        {
            if (!IsAuthenticated) return null;
            var raw = Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(raw, out int id) && id > 0 ? id : null;
        }
    }

    public int GetRequiredUserId()
    {
        return UserId ?? throw new UnauthorizedAccessException("Phiên đăng nhập không hợp lệ hoặc đã hết hạn.");
    }

    public string? Username => IsAuthenticated ? Principal?.Identity?.Name : null;

    public string? FullName => IsAuthenticated
        ? Principal?.FindFirstValue(ErpClaimTypes.FullName) ?? Principal?.Identity?.Name
        : null;

    public int? EmployeeId => ParseOptionalIntClaim(ErpClaimTypes.EmployeeId);

    public int? BranchId => ParseOptionalIntClaim(ErpClaimTypes.BranchId);

    public int? DepartmentId => ParseOptionalIntClaim(ErpClaimTypes.DepartmentId);

    public IReadOnlyList<string> Roles
    {
        get
        {
            if (!IsAuthenticated || Principal == null) return Array.Empty<string>();
            return Principal.FindAll(ClaimTypes.Role)
                .Select(c => c.Value)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }

    public bool IsSuperAdmin
    {
        get
        {
            if (!IsAuthenticated || Principal == null) return false;
            var claim = Principal.FindFirstValue(ErpClaimTypes.IsSuperAdmin);
            if (string.Equals(claim, "True", StringComparison.OrdinalIgnoreCase))
                return true;

            return IsInRole("SUPERADMIN");
        }
    }

    public bool MustChangePassword
    {
        get
        {
            if (!IsAuthenticated || Principal == null) return false;
            var claim = Principal.FindFirstValue(ErpClaimTypes.MustChangePassword);
            return string.Equals(claim, "True", StringComparison.OrdinalIgnoreCase);
        }
    }

    public string? IpAddress => _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();

    public bool IsInRole(string roleCode)
    {
        if (string.IsNullOrWhiteSpace(roleCode) || !IsAuthenticated || Principal == null)
            return false;

        return Principal.FindAll(ClaimTypes.Role)
            .Any(c => string.Equals(c.Value, roleCode, StringComparison.OrdinalIgnoreCase));
    }

    private int? ParseOptionalIntClaim(string claimType)
    {
        if (!IsAuthenticated || Principal == null) return null;
        var raw = Principal.FindFirstValue(claimType);
        return int.TryParse(raw, out int value) && value > 0 ? value : null;
    }
}

