using System;
using System.Collections.Generic;

namespace ERP_HRM_CMS.Models.ViewModels
{
    public class ExecutiveDashboardViewModel
    {
        // Cờ phân quyền hiển thị từng khối dữ liệu (Zero-Trust Widget Gating)
        public bool CanViewSalesKpi { get; set; }
        public bool CanViewHrKpi { get; set; }
        public bool CanViewApprovalsKpi { get; set; }
        public bool CanViewInventoryKpi { get; set; }
        public bool CanViewDebtKpi { get; set; }
        public bool CanViewAuditFeed { get; set; }
        public bool CanUsePos { get; set; }
        public bool IsSelfServiceView { get; set; }
        public string ScopeBadgeLabel { get; set; } = "Cá nhân";

        // Dữ liệu tự phục vụ dành cho vai trò EMPLOYEE
        public int MyWorkDaysThisMonth { get; set; }
        public int MyPendingLeavesCount { get; set; }
        public decimal MyApprovedLeaveDaysThisYear { get; set; }

        // KPI Cards Data
        public decimal MonthlyRevenue { get; set; }
        public double RevenueGrowthPercent { get; set; }
        public int TodayOrdersCount { get; set; }
        public decimal TodayRevenue { get; set; }
        public int ActiveEmployeesCount { get; set; }
        public int TotalEmployeesCount { get; set; }
        public int PendingApprovalsCount { get; set; }
        public int LowStockCount { get; set; }
        public decimal OverdueDebtTotal { get; set; }

        // Biểu đồ Doanh thu 12 tháng
        public List<string> Revenue12MonthsLabels { get; set; } = new List<string>();
        public List<decimal> Revenue12MonthsData { get; set; } = new List<decimal>();

        // Biểu đồ tỷ trọng Doanh thu theo Chi Nhánh
        public List<string> RevenueByBranchLabels { get; set; } = new List<string>();
        public List<decimal> RevenueByBranchData { get; set; } = new List<decimal>();

        // Top 10 Sản phẩm bán chạy
        public List<TopProductViewModel> TopProducts { get; set; } = new List<TopProductViewModel>();

        // Activity Feed Realtime (Audit Logs & Events)
        public List<DashboardActivityItemViewModel> RecentActivities { get; set; } = new List<DashboardActivityItemViewModel>();
    }

    public class TopProductViewModel
    {
        public string SKU { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class DashboardActivityItemViewModel
    {
        public string Action { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string BadgeClass { get; set; } = "bg-primary";
        public string IconClass { get; set; } = "bi-activity";
    }
}
