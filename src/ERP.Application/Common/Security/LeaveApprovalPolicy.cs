using ERP.Application.Common.Models;

namespace ERP.Application.Common.Security;

public sealed record LeaveApprovalDecision(bool IsSelfApprovalOverrideBySuperAdmin);

/// <summary>
/// Chính sách phê duyệt đơn từ (Khắc phục lỗ hổng S7):
/// - Người tạo đơn hoặc chính nhân viên đứng tên trên đơn tuyệt đối không được tự bấm duyệt / từ chối đơn của chính mình.
/// - Ngoại lệ duy nhất: tài khoản SuperAdmin được phép phê duyệt nhưng bắt buộc đánh dấu <see cref="LeaveApprovalDecision.IsSelfApprovalOverrideBySuperAdmin"/> = true để ghi rõ vào AuditLog.
/// </summary>
public static class LeaveApprovalPolicy
{
    public static Result<LeaveApprovalDecision> ValidateApprover(
        int currentUserId,
        int? currentUserEmployeeId,
        bool isSuperAdmin,
        int requestEmployeeId,
        int? requestCreatedByUserId,
        int? requestEmployeeLinkedUserId = null)
    {
        bool isOwnEmployeeRecord = currentUserEmployeeId.HasValue && currentUserEmployeeId.Value == requestEmployeeId;
        bool isCreatorAccount = requestCreatedByUserId.HasValue && requestCreatedByUserId.Value == currentUserId;
        bool isLinkedAccount = requestEmployeeLinkedUserId.HasValue && requestEmployeeLinkedUserId.Value == currentUserId;

        bool isSelfAction = isOwnEmployeeRecord || isCreatorAccount || isLinkedAccount;

        if (isSelfAction)
        {
            if (!isSuperAdmin)
            {
                return Result<LeaveApprovalDecision>.Failure(
                    "Vi phạm nguyên tắc kiểm soát nội bộ: Bạn không được phép tự phê duyệt hoặc từ chối đơn từ của chính mình.",
                    "SELF_APPROVAL_FORBIDDEN");
            }

            return Result<LeaveApprovalDecision>.Success(new LeaveApprovalDecision(IsSelfApprovalOverrideBySuperAdmin: true));
        }

        return Result<LeaveApprovalDecision>.Success(new LeaveApprovalDecision(IsSelfApprovalOverrideBySuperAdmin: false));
    }
}

