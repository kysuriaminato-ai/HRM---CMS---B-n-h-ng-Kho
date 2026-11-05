using ERP.Domain.Enums;
using ERP_HRM_CMS.Models.Entities;

namespace ERP.Application.Common.Interfaces;

/// <summary>
/// Phân quyền theo phạm vi dữ liệu doanh nghiệp (Enterprise Data Scope: Company / Branch / Department / Self).
/// - Company: SuperAdmin, HR_MANAGER tổng công ty, Ban Giám đốc thấy toàn bộ dữ liệu.
/// - Branch: Quản lý chi nhánh / kho / bán hàng chỉ thấy dữ liệu thuộc chi nhánh mình.
/// - Department: Trưởng phòng chỉ thấy và quản lý nhân sự thuộc phòng ban mình.
/// - Self: Nhân viên (EMPLOYEE) chỉ xem và thao tác trên dữ liệu cá nhân của chính mình.
/// </summary>
public interface IDataScopeService
{
    DataScopeLevel GetScopeLevel();
    DataScopeLevel GetHrmScopeLevel();
    DataScopeLevel GetOperationsScopeLevel();

    IQueryable<Employee> ApplyEmployeeScope(IQueryable<Employee> query);
    IQueryable<LeaveRequest> ApplyLeaveRequestScope(IQueryable<LeaveRequest> query);
    IQueryable<AttendanceDaily> ApplyAttendanceScope(IQueryable<AttendanceDaily> query);
    IQueryable<Contract> ApplyContractScope(IQueryable<Contract> query);
    IQueryable<SalarySlip> ApplySalarySlipScope(IQueryable<SalarySlip> query);

    IQueryable<Order> ApplyOrderScope(IQueryable<Order> query);
    IQueryable<Inventory> ApplyInventoryScope(IQueryable<Inventory> query);
    IQueryable<InventoryTransaction> ApplyInventoryTransactionScope(IQueryable<InventoryTransaction> query);
    IQueryable<PurchaseOrder> ApplyPurchaseOrderScope(IQueryable<PurchaseOrder> query);

    bool CanAccessEmployee(int employeeId, int departmentId, int branchId);
    bool CanAccessDepartment(int departmentId, int branchId);
    bool CanAccessBranch(int branchId);

    /// <summary>
    /// Chuẩn hóa BranchId đầu vào từ client theo phạm vi dữ liệu của người dùng hiện tại.
    /// Nếu người dùng bị khóa ở cấp Branch, luôn ép về BranchId của chính họ.
    /// </summary>
    int? ResolveAllowedBranchId(int? requestedBranchId);
}

