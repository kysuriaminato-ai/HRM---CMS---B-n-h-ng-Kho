using System;
using System.Collections.Generic;

namespace ERP_HRM_CMS.Models.ViewModels
{
    // ═══════════════════════════════════════════════════════════════
    // BỘ LỌC BÁO CÁO EXECUTIVE PEOPLE ANALYTICS
    // ═══════════════════════════════════════════════════════════════
    public class PeopleAnalyticsFilterDto
    {
        public int Year { get; set; } = DateTime.Today.Year;
        public int? Quarter { get; set; } // 1, 2, 3, 4, or null (cả năm)
        public int? BranchId { get; set; }
        public int? DepartmentId { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════
    // DTO TRUNG TÂM PHÂN TÍCH NHÂN SỰ CẤP ĐIỀU HÀNH
    // ═══════════════════════════════════════════════════════════════
    public class ExecutivePeopleAnalyticsDto
    {
        public PeopleAnalyticsFilterDto Filter { get; set; } = new();

        // 1. SÁU CHỈ SỐ KPI ĐIỀU HÀNH (EXECUTIVE KPI CARDS)
        // 1.1 Tổng nhân sự & Định biên
        public int TotalHeadcount { get; set; }
        public int ActiveHeadcount { get; set; }
        public int BudgetedHeadcount { get; set; } = 150;
        public decimal HeadcountGrowthRate { get; set; } // % tăng trưởng so với đầu năm/kỳ trước

        // 1.2 Tỷ lệ biến động nhân sự (Turnover Rate)
        public decimal TurnoverRate { get; set; } // (Số nghỉ việc / Số nhân sự bình quân) * 100%
        public decimal TurnoverTrend { get; set; } // Chênh lệch điểm % so với kỳ trước (-1.2% là tích cực)

        // 1.3 Tỷ lệ tuyển dụng lấp đầy (Recruitment Fill Rate)
        public decimal FillRate { get; set; } // (Số tuyển được / Định biên cần tuyển) * 100%
        public decimal FillRateTrend { get; set; }

        // 1.4 Tỷ lệ chuyên cần đi làm đúng giờ (Attendance Rate)
        public decimal AttendanceRate { get; set; } // % ngày công đúng giờ không trễ/vắng
        public decimal AttendanceRateTrend { get; set; }

        // 1.5 Tổng quỹ lương Gross & Bảo hiểm tháng hiện hành
        public decimal CurrentGrossPayroll { get; set; }
        public decimal CurrentInsuranceCost { get; set; }
        public decimal PayrollChangePercent { get; set; } // % biến động so với tháng trước

        // 1.6 Điểm KPI trung bình toàn công ty
        public decimal AverageKpiScore { get; set; }
        public decimal AverageKpiScoreTrend { get; set; }

        // 2. BIỂU ĐỒ 1: XU HƯỚNG 12 THÁNG (TUYỂN MỚI VS NGHỈ VIỆC)
        public List<MonthlyMovementDto> HeadcountMovement12Months { get; set; } = new();

        // 3. BIỂU ĐỒ 2: CƠ CẤU QUỸ LƯƠNG, PHỤ CẤP, OT & BẢO HIỂM THEO PHÒNG BAN
        public List<DepartmentCostDto> DepartmentPayrollBreakdown { get; set; } = new();

        // 4. BIỂU ĐỒ 3: PHỄU TUYỂN DỤNG (RECRUITMENT FUNNEL)
        public RecruitmentFunnelDto RecruitmentFunnel { get; set; } = new();

        // 5. BIỂU ĐỒ 4: PHÂN BỔ KPI & THÁP THÂM NIÊN NHÂN SỰ
        public Dictionary<string, int> KpiGradeDistribution { get; set; } = new();
        public SeniorityPyramidDto SeniorityPyramid { get; set; } = new();

        // 6. BẢNG CẢNH BÁO HÀNH ĐỘNG SỚM CHO HR (EARLY HR ACTION ALERTS)
        public List<ExpiringContractAlertDto> ExpiringContracts { get; set; } = new();
        public List<EndingProbationAlertDto> EndingProbations { get; set; } = new();
        public List<ExpiringCertificateAlertDto> ExpiringCertificates { get; set; } = new();
        public List<HighOtDepartmentAlertDto> HighOtDepartments { get; set; } = new();
    }

    public class MonthlyMovementDto
    {
        public int Month { get; set; }
        public string MonthLabel => $"Th{Month:D2}";
        public int NewHires { get; set; }
        public int Resignations { get; set; }
        public int NetGrowth => NewHires - Resignations;
    }

    public class DepartmentCostDto
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public decimal BaseSalary { get; set; }
        public decimal Allowances { get; set; }
        public decimal OtSalary { get; set; }
        public decimal EmployerInsurance { get; set; }
        public decimal TotalCost => BaseSalary + Allowances + OtSalary + EmployerInsurance;
        public int EmployeeCount { get; set; }
    }

    public class RecruitmentFunnelDto
    {
        public int AppliedCount { get; set; }
        public int ScreeningCount { get; set; }
        public int InterviewCount { get; set; }
        public int OfferedCount { get; set; }
        public int HiredCount { get; set; }

        public decimal ScreeningRate => AppliedCount > 0 ? Math.Round((ScreeningCount * 100m) / AppliedCount, 1) : 0;
        public decimal InterviewRate => ScreeningCount > 0 ? Math.Round((InterviewCount * 100m) / ScreeningCount, 1) : 0;
        public decimal OfferRate => InterviewCount > 0 ? Math.Round((OfferedCount * 100m) / InterviewCount, 1) : 0;
        public decimal ConversionRate => AppliedCount > 0 ? Math.Round((HiredCount * 100m) / AppliedCount, 1) : 0;
    }

    public class SeniorityPyramidDto
    {
        public int Under1Year { get; set; }
        public int From1To3Years { get; set; }
        public int From3To5Years { get; set; }
        public int From5To10Years { get; set; }
        public int Over10Years { get; set; }

        public int Total => Under1Year + From1To3Years + From3To5Years + From5To10Years + Over10Years;
    }

    public class ExpiringContractAlertDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string ContractNumber { get; set; } = string.Empty;
        public DateTime EndDate { get; set; }
        public int DaysRemaining { get; set; }
    }

    public class EndingProbationAlertDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public DateTime ProbationEndDate { get; set; }
        public int DaysRemaining { get; set; }
    }

    public class ExpiringCertificateAlertDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string CertificateName { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
        public int DaysRemaining { get; set; }
    }

    public class HighOtDepartmentAlertDto
    {
        public string DepartmentName { get; set; } = string.Empty;
        public decimal TotalOtHours { get; set; }
        public decimal AverageOtHoursPerEmployee { get; set; }
        public int HighOtEmployeeCount { get; set; }
    }
}

