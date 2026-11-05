using ERP.Application.Common.Models;

namespace ERP.Application.Common.Security;

/// <summary>
/// Chính sách kiểm soát trần giảm giá hợp lệ phía Server tại POS (Khắc phục lỗ hổng S6 / P0-03).
/// - Không bao giờ chấp nhận số tiền giảm giá âm hoặc vượt quá tạm tính (SubTotal).
/// - Áp trần phần trăm chiết khấu tối đa theo cấp thẩm quyền:
///   + Thu ngân thông thường (order.pos): tối đa 10%
///   + Quản lý đơn hàng / Giám đốc kinh doanh (order.manage / SALES_DIRECTOR / ADMIN_DONHANG): tối đa 30%
///   + SuperAdmin: tối đa 50% (chống xuất đơn 0 đồng làm thất thoát tài sản)
/// - Khi giảm giá vượt 5% bắt buộc phải nhập lý do (Note) để lưu vết AuditLog.
/// </summary>
public static class PosDiscountPolicy
{
    public const decimal StandardCashierMaxRate = 0.10m;   // 10%
    public const decimal SalesManagerMaxRate = 0.30m;      // 30%
    public const decimal SuperAdminMaxRate = 0.50m;        // 50%
    public const decimal ReasonRequiredRateThreshold = 0.05m; // > 5% phải có lý do

    public static decimal GetMaxDiscountRate(bool isSuperAdmin, bool canManageOrders)
    {
        if (isSuperAdmin) return SuperAdminMaxRate;
        if (canManageOrders) return SalesManagerMaxRate;
        return StandardCashierMaxRate;
    }

    public static Result<decimal> Validate(
        decimal subTotal,
        decimal discountAmount,
        string? note,
        bool isSuperAdmin,
        bool canManageOrders)
    {
        if (subTotal <= 0)
        {
            return Result<decimal>.Failure("Tổng tiền hàng không hợp lệ.", "INVALID_SUBTOTAL");
        }

        if (discountAmount < 0)
        {
            return Result<decimal>.Failure("Số tiền giảm giá không được nhỏ hơn 0.", "NEGATIVE_DISCOUNT");
        }

        if (discountAmount == 0)
        {
            return Result<decimal>.Success(0m);
        }

        if (discountAmount > subTotal)
        {
            return Result<decimal>.Failure("Số tiền giảm giá không được vượt quá tổng tiền hàng.", "DISCOUNT_EXCEEDS_SUBTOTAL");
        }

        decimal maxRate = GetMaxDiscountRate(isSuperAdmin, canManageOrders);
        decimal maxAllowedAmount = Math.Round(subTotal * maxRate, 0, MidpointRounding.AwayFromZero);

        if (discountAmount > maxAllowedAmount)
        {
            int percentDisplay = (int)(maxRate * 100);
            return Result<decimal>.Failure(
                $"Mức giảm giá vượt trần cho phép ({percentDisplay}% tương đương {maxAllowedAmount:N0} ₫). Vui lòng liên hệ cấp quản lý có thẩm quyền cao hơn.",
                "DISCOUNT_CEILING_EXCEEDED");
        }

        decimal effectiveRate = discountAmount / subTotal;
        if (effectiveRate > ReasonRequiredRateThreshold && string.IsNullOrWhiteSpace(note))
        {
            return Result<decimal>.Failure(
                "Đơn hàng giảm giá trên 5% bắt buộc phải ghi rõ lý do chiết khấu để đối soát kiểm toán.",
                "DISCOUNT_REASON_REQUIRED");
        }

        return Result<decimal>.Success(discountAmount);
    }
}

