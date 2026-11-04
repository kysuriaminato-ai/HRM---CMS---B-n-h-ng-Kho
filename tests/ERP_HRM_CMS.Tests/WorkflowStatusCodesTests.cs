using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Models.Enums;
using Xunit;

namespace ERP_HRM_CMS.Tests
{
    /// <summary>
    /// Kiểm tra bảng ánh xạ enum trạng thái &lt;-&gt; mã chuỗi trong database và converter EF Core.
    /// Các test này không cần database.
    /// </summary>
    public class WorkflowStatusCodesTests
    {
        [Fact]
        public void Every_Status_Has_A_Unique_Stored_Code()
        {
            AssertComplete(WorkflowStatusCodes.PurchaseOrder);
            AssertComplete(WorkflowStatusCodes.Debt);
            AssertComplete(WorkflowStatusCodes.LeaveRequest);
            AssertComplete(WorkflowStatusCodes.PayrollPeriod);
            AssertComplete(WorkflowStatusCodes.SalarySlip);
        }

        [Fact]
        public void Leave_Request_Keeps_The_Legacy_Database_Codes()
        {
            Assert.Equal("CHO_DUYET", WorkflowStatusCodes.ToCode(WorkflowStatusCodes.LeaveRequest, LeaveRequestStatus.PendingApproval));
            Assert.Equal("TRUONG_PHONG_DUYET", WorkflowStatusCodes.ToCode(WorkflowStatusCodes.LeaveRequest, LeaveRequestStatus.DepartmentApproved));
            Assert.Equal("GIAM_DOC_DUYET", WorkflowStatusCodes.ToCode(WorkflowStatusCodes.LeaveRequest, LeaveRequestStatus.DirectorApproved));
            Assert.Equal("TU_CHOI", WorkflowStatusCodes.ToCode(WorkflowStatusCodes.LeaveRequest, LeaveRequestStatus.Rejected));
        }

        [Fact]
        public void Purchase_Order_And_Debt_Keep_The_Legacy_Database_Codes()
        {
            Assert.Equal("PendingApproval", WorkflowStatusCodes.ToCode(WorkflowStatusCodes.PurchaseOrder, PurchaseOrderStatus.PendingApproval));
            Assert.Equal("Received", WorkflowStatusCodes.ToCode(WorkflowStatusCodes.PurchaseOrder, PurchaseOrderStatus.Received));
            Assert.Equal("PartiallyPaid", WorkflowStatusCodes.ToCode(WorkflowStatusCodes.Debt, DebtStatus.PartiallyPaid));
            Assert.Equal("Overdue", WorkflowStatusCodes.ToCode(WorkflowStatusCodes.Debt, DebtStatus.Overdue));
        }

        [Fact]
        public void Payroll_Period_And_Slip_Keep_The_Legacy_Database_Codes()
        {
            Assert.Equal("CALCULATED", WorkflowStatusCodes.ToCode(WorkflowStatusCodes.PayrollPeriod, PayrollPeriodStatus.Calculated));
            Assert.Equal("DRAFT", WorkflowStatusCodes.ToCode(WorkflowStatusCodes.SalarySlip, SalarySlipStatus.Draft));
            Assert.Equal("SENT", WorkflowStatusCodes.ToCode(WorkflowStatusCodes.SalarySlip, SalarySlipStatus.Sent));
            Assert.Equal("CONFIRMED", WorkflowStatusCodes.ToCode(WorkflowStatusCodes.SalarySlip, SalarySlipStatus.Confirmed));
        }

        [Fact]
        public void Converter_Round_Trips_Every_Leave_Status()
        {
            var converter = new WorkflowStatusConverter<LeaveRequestStatus>(WorkflowStatusCodes.LeaveRequest);

            foreach (var status in Enum.GetValues<LeaveRequestStatus>())
            {
                Assert.Equal(status, converter.ConvertFromProvider(converter.ConvertToProvider(status)));
            }
        }

        [Fact]
        public void Converter_Rejects_An_Unknown_Code_From_The_Database()
        {
            var converter = new WorkflowStatusConverter<DebtStatus>(WorkflowStatusCodes.Debt);

            Assert.Throws<InvalidOperationException>(() => converter.ConvertFromProvider("KHONG_TON_TAI"));
        }

        [Fact]
        public void TryParse_Ignores_Case_And_Rejects_Unknown_Or_Empty_Codes()
        {
            Assert.True(WorkflowStatusCodes.TryParse(WorkflowStatusCodes.LeaveRequest, "cho_duyet", out var status));
            Assert.Equal(LeaveRequestStatus.PendingApproval, status);

            Assert.False(WorkflowStatusCodes.TryParse(WorkflowStatusCodes.LeaveRequest, "abc", out _));
            Assert.False(WorkflowStatusCodes.TryParse(WorkflowStatusCodes.LeaveRequest, null, out _));
        }

        private static void AssertComplete<TEnum>(IReadOnlyDictionary<TEnum, string> codes)
            where TEnum : struct, Enum
        {
            var values = Enum.GetValues<TEnum>();

            Assert.Equal(values.Length, codes.Count);
            Assert.All(values, value => Assert.True(codes.ContainsKey(value), $"Thiếu mã cho {value}"));
            Assert.Equal(codes.Count, codes.Values.Distinct().Count());
        }
    }
}
