namespace ERP_HRM_CMS.Models.Enums
{
    /// <summary>Trạng thái đơn mua hàng (PO). Mã lưu trong database: xem WorkflowStatusCodes.PurchaseOrder.</summary>
    public enum PurchaseOrderStatus
    {
        PendingApproval,
        Approved,
        Received,
        Cancelled
    }

    /// <summary>Trạng thái công nợ. Mã lưu trong database: xem WorkflowStatusCodes.Debt.</summary>
    public enum DebtStatus
    {
        Unpaid,
        PartiallyPaid,
        Paid,
        Overdue
    }

    /// <summary>Trạng thái đơn từ (nghỉ phép, công tác). Mã lưu trong database: xem WorkflowStatusCodes.LeaveRequest.</summary>
    public enum LeaveRequestStatus
    {
        /// <summary>CHO_DUYET: chờ Trưởng bộ phận duyệt.</summary>
        PendingApproval,
        /// <summary>TRUONG_PHONG_DUYET: Trưởng bộ phận đã duyệt, chờ Giám đốc.</summary>
        DepartmentApproved,
        /// <summary>GIAM_DOC_DUYET: đã duyệt hoàn tất.</summary>
        DirectorApproved,
        /// <summary>TU_CHOI: bị từ chối.</summary>
        Rejected
    }

    /// <summary>Trạng thái kỳ lương. Mã lưu trong database: xem WorkflowStatusCodes.PayrollPeriod.</summary>
    public enum PayrollPeriodStatus
    {
        Draft,
        Calculated,
        Locked,
        Paid
    }

    /// <summary>Trạng thái phiếu lương. Mã lưu trong database: xem WorkflowStatusCodes.SalarySlip.</summary>
    public enum SalarySlipStatus
    {
        Draft,
        Sent,
        Confirmed
    }

    /// <summary>
    /// Bảng ánh xạ enum trạng thái &lt;-&gt; mã chuỗi đang lưu trong database.
    /// Giữ nguyên mã cũ để dữ liệu hiện có đọc được mà không cần đổi giá trị.
    /// </summary>
    public static class WorkflowStatusCodes
    {
        public static readonly IReadOnlyDictionary<PurchaseOrderStatus, string> PurchaseOrder = Build(
            (PurchaseOrderStatus.PendingApproval, "PendingApproval"),
            (PurchaseOrderStatus.Approved, "Approved"),
            (PurchaseOrderStatus.Received, "Received"),
            (PurchaseOrderStatus.Cancelled, "Cancelled"));

        public static readonly IReadOnlyDictionary<DebtStatus, string> Debt = Build(
            (DebtStatus.Unpaid, "Unpaid"),
            (DebtStatus.PartiallyPaid, "PartiallyPaid"),
            (DebtStatus.Paid, "Paid"),
            (DebtStatus.Overdue, "Overdue"));

        public static readonly IReadOnlyDictionary<LeaveRequestStatus, string> LeaveRequest = Build(
            (LeaveRequestStatus.PendingApproval, "CHO_DUYET"),
            (LeaveRequestStatus.DepartmentApproved, "TRUONG_PHONG_DUYET"),
            (LeaveRequestStatus.DirectorApproved, "GIAM_DOC_DUYET"),
            (LeaveRequestStatus.Rejected, "TU_CHOI"));

        public static readonly IReadOnlyDictionary<PayrollPeriodStatus, string> PayrollPeriod = Build(
            (PayrollPeriodStatus.Draft, "DRAFT"),
            (PayrollPeriodStatus.Calculated, "CALCULATED"),
            (PayrollPeriodStatus.Locked, "LOCKED"),
            (PayrollPeriodStatus.Paid, "PAID"));

        public static readonly IReadOnlyDictionary<SalarySlipStatus, string> SalarySlip = Build(
            (SalarySlipStatus.Draft, "DRAFT"),
            (SalarySlipStatus.Sent, "SENT"),
            (SalarySlipStatus.Confirmed, "CONFIRMED"));

        /// <summary>Trả về mã lưu trong database của một trạng thái. Ném lỗi nếu chưa khai báo mã.</summary>
        public static string ToCode<TEnum>(IReadOnlyDictionary<TEnum, string> codes, TEnum status)
            where TEnum : struct, Enum
        {
            if (codes.TryGetValue(status, out var code)) return code;
            throw new InvalidOperationException($"Chưa khai báo mã lưu cho trạng thái {status}.");
        }

        /// <summary>Đọc mã từ database (hoặc từ tham số URL) thành enum. Không phân biệt hoa thường.</summary>
        public static bool TryParse<TEnum>(IReadOnlyDictionary<TEnum, string> codes, string? code, out TEnum status)
            where TEnum : struct, Enum
        {
            if (code != null)
            {
                foreach (var pair in codes)
                {
                    if (string.Equals(pair.Value, code, StringComparison.OrdinalIgnoreCase))
                    {
                        status = pair.Key;
                        return true;
                    }
                }
            }

            status = default;
            return false;
        }

        private static IReadOnlyDictionary<TEnum, string> Build<TEnum>(params (TEnum Status, string Code)[] pairs)
            where TEnum : struct, Enum
        {
            return pairs.ToDictionary(p => p.Status, p => p.Code);
        }
    }
}
