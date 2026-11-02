using ERP_HRM_CMS.Models.Entities;

namespace ERP_HRM_CMS.Services
{
    public interface IPayrollService
    {
        Task<PayrollPeriod> CalculatePayrollAsync(int month, int year, int standardDays, int? creatorUserId = null);
        Task<bool> SendPayslipsAsync(int payrollPeriodId, int? employeeId = null);
        byte[] GeneratePayrollExcel(PayrollPeriod period);
    }
}
