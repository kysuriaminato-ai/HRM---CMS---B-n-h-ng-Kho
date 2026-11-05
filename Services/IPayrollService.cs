using System.Threading.Tasks;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Models.ViewModels;

namespace ERP_HRM_CMS.Services
{
    public interface IPayrollService
    {
        Task<PayrollPeriod> CalculatePayrollAsync(int month, int year, int standardDays, int? creatorUserId = null);
        Task<bool> SendPayslipsAsync(int payrollPeriodId, int? employeeId = null);
        byte[] GeneratePayrollExcel(PayrollPeriod period);
        byte[] GenerateEmployerInsuranceExcel(PayrollPeriod period);
        byte[] GenerateBankTransferExcel(PayrollPeriod period);
        Task<PayrollComparisonDto> GetPayrollComparisonAsync(int month, int year);
    }
}
