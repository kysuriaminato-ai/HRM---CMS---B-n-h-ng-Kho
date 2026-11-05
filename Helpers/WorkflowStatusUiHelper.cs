using ERP_HRM_CMS.Models.Enums;
using ERP_HRM_CMS.Models.ViewModels.UI;

namespace ERP_HRM_CMS.Helpers
{
    /// <summary>
    /// Ánh xạ tập trung các trạng thái trong WorkflowStatuses (cả kiểu Enum và chuỗi mã DB)
    /// sang nhãn tiếng Việt, biến thể màu token và icon Bootstrap Icons.
    /// </summary>
    public static class WorkflowStatusUiHelper
    {
        public static WorkflowStatusBadgeInfo Resolve(PurchaseOrderStatus status) => status switch
        {
            PurchaseOrderStatus.PendingApproval => new("Chờ phê duyệt", "warning", "bi-hourglass-split"),
            PurchaseOrderStatus.Approved        => new("Đã duyệt PO", "info", "bi-patch-check"),
            PurchaseOrderStatus.Received        => new("Đã nhập kho", "success", "bi-box-seam-fill"),
            PurchaseOrderStatus.Cancelled       => new("Đã hủy", "danger", "bi-x-circle"),
            _                                   => new(status.ToString(), "neutral", "bi-circle")
        };

        public static WorkflowStatusBadgeInfo Resolve(DebtStatus status) => status switch
        {
            DebtStatus.Unpaid        => new("Chưa thanh toán", "warning", "bi-clock-history"),
            DebtStatus.PartiallyPaid => new("Thanh toán một phần", "info", "bi-pie-chart"),
            DebtStatus.Paid          => new("Đã tất toán", "success", "bi-check-circle"),
            DebtStatus.Overdue       => new("Quá hạn", "danger", "bi-exclamation-octagon"),
            _                        => new(status.ToString(), "neutral", "bi-circle")
        };

        public static WorkflowStatusBadgeInfo Resolve(LeaveRequestStatus status) => status switch
        {
            LeaveRequestStatus.PendingApproval    => new("Chờ trưởng bộ phận", "warning", "bi-hourglass-split"),
            LeaveRequestStatus.DepartmentApproved => new("Trưởng phòng đã duyệt", "info", "bi-person-check"),
            LeaveRequestStatus.DirectorApproved   => new("Giám đốc phê duyệt", "success", "bi-check2-all"),
            LeaveRequestStatus.Rejected           => new("Từ chối", "danger", "bi-x-octagon"),
            _                                     => new(status.ToString(), "neutral", "bi-circle")
        };

        public static WorkflowStatusBadgeInfo Resolve(PayrollPeriodStatus status) => status switch
        {
            PayrollPeriodStatus.Draft      => new("Bản nháp", "neutral", "bi-pencil-square"),
            PayrollPeriodStatus.Calculated => new("Đã tính lương", "info", "bi-calculator"),
            PayrollPeriodStatus.Locked     => new("Đã khóa sổ", "warning", "bi-lock-fill"),
            PayrollPeriodStatus.Paid       => new("Đã chi trả", "success", "bi-cash-coin"),
            _                              => new(status.ToString(), "neutral", "bi-circle")
        };

        public static WorkflowStatusBadgeInfo Resolve(SalarySlipStatus status) => status switch
        {
            SalarySlipStatus.Draft     => new("Phiếu nháp", "neutral", "bi-file-earmark"),
            SalarySlipStatus.Sent      => new("Đã gửi nhân viên", "info", "bi-send-check"),
            SalarySlipStatus.Confirmed => new("Đã xác nhận", "success", "bi-patch-check-fill"),
            _                          => new(status.ToString(), "neutral", "bi-circle")
        };

        public static WorkflowStatusBadgeInfo ResolveObject(object? value, string? workflow = null)
        {
            return value switch
            {
                null                    => new("Không xác định", "neutral", "bi-dash-circle"),
                PurchaseOrderStatus po  => Resolve(po),
                DebtStatus debt         => Resolve(debt),
                LeaveRequestStatus lr   => Resolve(lr),
                PayrollPeriodStatus pp  => Resolve(pp),
                SalarySlipStatus ss     => Resolve(ss),
                string code             => ResolveCode(code, workflow),
                _                       => ResolveCode(value.ToString(), workflow)
            };
        }

        public static WorkflowStatusBadgeInfo ResolveCode(string? code, string? workflow = null)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return new WorkflowStatusBadgeInfo("Không xác định", "neutral", "bi-dash-circle");
            }

            string normalizedWf = (workflow ?? string.Empty).Trim().ToLowerInvariant();
            string trimmed = code.Trim();

            // 1. Ưu tiên theo workflow được chỉ định rõ
            switch (normalizedWf)
            {
                case "purchaseorder" or "po":
                    if (WorkflowStatusCodes.TryParse(WorkflowStatusCodes.PurchaseOrder, trimmed, out var po))
                        return Resolve(po);
                    break;
                case "debt":
                    if (WorkflowStatusCodes.TryParse(WorkflowStatusCodes.Debt, trimmed, out var debt))
                        return Resolve(debt);
                    break;
                case "leaverequest" or "leave":
                    if (WorkflowStatusCodes.TryParse(WorkflowStatusCodes.LeaveRequest, trimmed, out var lr))
                        return Resolve(lr);
                    break;
                case "payrollperiod" or "payroll":
                    if (WorkflowStatusCodes.TryParse(WorkflowStatusCodes.PayrollPeriod, trimmed, out var pp))
                        return Resolve(pp);
                    break;
                case "salaryslip" or "slip":
                    if (WorkflowStatusCodes.TryParse(WorkflowStatusCodes.SalarySlip, trimmed, out var ss))
                        return Resolve(ss);
                    break;
            }

            // 2. Tự động nhận diện trên toàn bộ bảng mã WorkflowStatusCodes
            if (WorkflowStatusCodes.TryParse(WorkflowStatusCodes.LeaveRequest, trimmed, out var autoLr))
                return Resolve(autoLr);
            if (WorkflowStatusCodes.TryParse(WorkflowStatusCodes.PurchaseOrder, trimmed, out var autoPo))
                return Resolve(autoPo);
            if (WorkflowStatusCodes.TryParse(WorkflowStatusCodes.Debt, trimmed, out var autoDebt))
                return Resolve(autoDebt);
            if (WorkflowStatusCodes.TryParse(WorkflowStatusCodes.SalarySlip, trimmed, out var autoSs)
                && (trimmed.Equals("SENT", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("CONFIRMED", StringComparison.OrdinalIgnoreCase)))
                return Resolve(autoSs);
            if (WorkflowStatusCodes.TryParse(WorkflowStatusCodes.PayrollPeriod, trimmed, out var autoPp))
                return Resolve(autoPp);

            // 3. Hỗ trợ thêm các mã trạng thái phổ biến trong hệ thống (nhân viên, đơn hàng)
            return trimmed.ToUpperInvariant() switch
            {
                "ACTIVE" or "COMPLETED" or "PUBLISHED" => new("Hoạt động", "success", "bi-check-circle"),
                "PROBATION"                            => new("Thử việc", "info", "bi-person-workspace"),
                "ONLEAVE" or "PENDING"                 => new("Đang chờ", "warning", "bi-clock"),
                "RESIGNED" or "INACTIVE"               => new("Ngừng hoạt động", "danger", "bi-slash-circle"),
                _                                      => new(trimmed, "neutral", "bi-tag")
            };
        }
    }
}

