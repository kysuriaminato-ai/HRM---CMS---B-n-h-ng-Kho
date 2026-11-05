using ERP.Application.Common.Interfaces;
using ERP.Domain.Enums;
using ERP_HRM_CMS.Models.Entities;

namespace ERP.Application.Common.Security;

/// <summary>
/// Triển khai bộ lọc phạm vi dữ liệu tự động ở tầng truy vấn IQueryable (dịch trực tiếp sang SQL).
/// </summary>
public sealed class DataScopeService : IDataScopeService
{
    private readonly ICurrentUserService _currentUser;

    public DataScopeService(ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    public DataScopeLevel GetScopeLevel()
    {
        if (!_currentUser.IsAuthenticated)
            return DataScopeLevel.None;

        var hrm = GetHrmScopeLevel();
        var ops = GetOperationsScopeLevel();
        return hrm >= ops ? hrm : ops;
    }

    public DataScopeLevel GetHrmScopeLevel()
    {
        if (!_currentUser.IsAuthenticated)
            return DataScopeLevel.None;

        // 1. Toàn công ty: SuperAdmin, HR_MANAGER tổng công ty, Quản trị lương toàn công ty
        if (_currentUser.IsSuperAdmin ||
            _currentUser.IsInRole("SUPERADMIN") ||
            _currentUser.IsInRole("HR_MANAGER") ||
            _currentUser.IsInRole("ADMIN_BANGLUONG") ||
            _currentUser.IsInRole("DIRECTOR") ||
            _currentUser.IsInRole("CEO"))
        {
            return DataScopeLevel.Company;
        }

        // Quản trị nhóm nhân viên không gắn phòng ban cụ thể -> cấp công ty; có gắn phòng ban -> cấp phòng ban
        if (_currentUser.IsInRole("ADMIN_NHANVIEN"))
        {
            if (_currentUser.DepartmentId.HasValue)
                return DataScopeLevel.Department;
            if (_currentUser.BranchId.HasValue)
                return DataScopeLevel.Branch;
            return DataScopeLevel.Company;
        }

        // 2. Quản lý chi nhánh: chỉ thấy nhân sự thuộc chi nhánh mình
        if (_currentUser.IsInRole("BRANCH_MANAGER"))
        {
            return DataScopeLevel.Branch;
        }

        // 3. Trưởng phòng: chỉ thấy nhân sự thuộc phòng mình
        if (_currentUser.IsInRole("DEPARTMENT_HEAD") || _currentUser.IsInRole("TRUONG_PHONG"))
        {
            return DataScopeLevel.Department;
        }

        // 4. Mặc định (bao gồm EMPLOYEE): chỉ thao tác trên dữ liệu cá nhân
        return DataScopeLevel.Self;
    }

    public DataScopeLevel GetOperationsScopeLevel()
    {
        if (!_currentUser.IsAuthenticated)
            return DataScopeLevel.None;

        // 1. Toàn công ty: SuperAdmin, Giám đốc kinh doanh, HR_MANAGER (nếu được cấp quyền báo cáo)
        if (_currentUser.IsSuperAdmin ||
            _currentUser.IsInRole("SUPERADMIN") ||
            _currentUser.IsInRole("SALES_DIRECTOR") ||
            _currentUser.IsInRole("HR_MANAGER") ||
            _currentUser.IsInRole("DIRECTOR") ||
            _currentUser.IsInRole("CEO"))
        {
            return DataScopeLevel.Company;
        }

        // Quản lý chi nhánh luôn giới hạn theo chi nhánh của mình
        if (_currentUser.IsInRole("BRANCH_MANAGER"))
        {
            return DataScopeLevel.Branch;
        }

        // Quản trị kho / đơn hàng / sản phẩm: nếu tài khoản gắn chi nhánh thì khóa theo chi nhánh; nếu tài khoản tổng công ty (không gắn chi nhánh) thì xem toàn công ty
        if (_currentUser.IsInRole("ADMIN_KHO") ||
            _currentUser.IsInRole("ADMIN_DONHANG") ||
            _currentUser.IsInRole("ADMIN_SANPHAM"))
        {
            return _currentUser.BranchId.HasValue ? DataScopeLevel.Branch : DataScopeLevel.Company;
        }

        // Nhân viên bán hàng / Thủ kho chuyên trách: khóa theo chi nhánh nếu có gán BranchId
        if (_currentUser.IsInRole("SALES_STAFF") || _currentUser.IsInRole("WAREHOUSE_STAFF"))
        {
            return _currentUser.BranchId.HasValue ? DataScopeLevel.Branch : DataScopeLevel.Company;
        }

        if (_currentUser.IsInRole("DEPARTMENT_HEAD") || _currentUser.IsInRole("TRUONG_PHONG"))
        {
            return DataScopeLevel.Department;
        }

        return DataScopeLevel.Self;
    }

    public IQueryable<Employee> ApplyEmployeeScope(IQueryable<Employee> query)
    {
        switch (GetHrmScopeLevel())
        {
            case DataScopeLevel.Company:
                return query;

            case DataScopeLevel.Branch:
                if (!_currentUser.BranchId.HasValue)
                    return query.Where(_ => false);
                int branchId = _currentUser.BranchId.Value;
                return query.Where(e => e.Department.BranchId == branchId);

            case DataScopeLevel.Department:
                if (!_currentUser.DepartmentId.HasValue)
                    return query.Where(_ => false);
                int deptId = _currentUser.DepartmentId.Value;
                return query.Where(e => e.DepartmentId == deptId);

            case DataScopeLevel.Self:
                if (!_currentUser.EmployeeId.HasValue)
                    return query.Where(_ => false);
                int empId = _currentUser.EmployeeId.Value;
                return query.Where(e => e.Id == empId);

            default:
                return query.Where(_ => false);
        }
    }

    public IQueryable<LeaveRequest> ApplyLeaveRequestScope(IQueryable<LeaveRequest> query)
    {
        switch (GetHrmScopeLevel())
        {
            case DataScopeLevel.Company:
                return query;

            case DataScopeLevel.Branch:
                if (!_currentUser.BranchId.HasValue)
                    return query.Where(_ => false);
                int branchId = _currentUser.BranchId.Value;
                return query.Where(r => r.Employee.Department.BranchId == branchId);

            case DataScopeLevel.Department:
                if (!_currentUser.DepartmentId.HasValue)
                    return query.Where(_ => false);
                int deptId = _currentUser.DepartmentId.Value;
                return query.Where(r => r.Employee.DepartmentId == deptId);

            case DataScopeLevel.Self:
                int? empId = _currentUser.EmployeeId;
                int? userId = _currentUser.UserId;
                if (empId.HasValue && userId.HasValue)
                    return query.Where(r => r.EmployeeId == empId.Value || r.CreatedByUserId == userId.Value);
                if (empId.HasValue)
                    return query.Where(r => r.EmployeeId == empId.Value);
                if (userId.HasValue)
                    return query.Where(r => r.CreatedByUserId == userId.Value);
                return query.Where(_ => false);

            default:
                return query.Where(_ => false);
        }
    }

    public IQueryable<AttendanceDaily> ApplyAttendanceScope(IQueryable<AttendanceDaily> query)
    {
        switch (GetHrmScopeLevel())
        {
            case DataScopeLevel.Company:
                return query;

            case DataScopeLevel.Branch:
                if (!_currentUser.BranchId.HasValue)
                    return query.Where(_ => false);
                int branchId = _currentUser.BranchId.Value;
                return query.Where(a => a.Employee.Department.BranchId == branchId);

            case DataScopeLevel.Department:
                if (!_currentUser.DepartmentId.HasValue)
                    return query.Where(_ => false);
                int deptId = _currentUser.DepartmentId.Value;
                return query.Where(a => a.Employee.DepartmentId == deptId);

            case DataScopeLevel.Self:
                if (!_currentUser.EmployeeId.HasValue)
                    return query.Where(_ => false);
                int empId = _currentUser.EmployeeId.Value;
                return query.Where(a => a.EmployeeId == empId);

            default:
                return query.Where(_ => false);
        }
    }

    public IQueryable<Contract> ApplyContractScope(IQueryable<Contract> query)
    {
        switch (GetHrmScopeLevel())
        {
            case DataScopeLevel.Company:
                return query;

            case DataScopeLevel.Branch:
                if (!_currentUser.BranchId.HasValue)
                    return query.Where(_ => false);
                int branchId = _currentUser.BranchId.Value;
                return query.Where(c => c.Employee.Department.BranchId == branchId);

            case DataScopeLevel.Department:
                if (!_currentUser.DepartmentId.HasValue)
                    return query.Where(_ => false);
                int deptId = _currentUser.DepartmentId.Value;
                return query.Where(c => c.Employee.DepartmentId == deptId);

            case DataScopeLevel.Self:
                if (!_currentUser.EmployeeId.HasValue)
                    return query.Where(_ => false);
                int empId = _currentUser.EmployeeId.Value;
                return query.Where(c => c.EmployeeId == empId);

            default:
                return query.Where(_ => false);
        }
    }

    public IQueryable<SalarySlip> ApplySalarySlipScope(IQueryable<SalarySlip> query)
    {
        switch (GetHrmScopeLevel())
        {
            case DataScopeLevel.Company:
                return query;

            case DataScopeLevel.Branch:
                if (!_currentUser.BranchId.HasValue)
                    return query.Where(_ => false);
                int branchId = _currentUser.BranchId.Value;
                return query.Where(s => s.Employee.Department.BranchId == branchId);

            case DataScopeLevel.Department:
                if (!_currentUser.DepartmentId.HasValue)
                    return query.Where(_ => false);
                int deptId = _currentUser.DepartmentId.Value;
                return query.Where(s => s.Employee.DepartmentId == deptId);

            case DataScopeLevel.Self:
                if (!_currentUser.EmployeeId.HasValue)
                    return query.Where(_ => false);
                int empId = _currentUser.EmployeeId.Value;
                return query.Where(s => s.EmployeeId == empId);

            default:
                return query.Where(_ => false);
        }
    }

    public IQueryable<Order> ApplyOrderScope(IQueryable<Order> query)
    {
        switch (GetOperationsScopeLevel())
        {
            case DataScopeLevel.Company:
                return query;

            case DataScopeLevel.Branch:
            case DataScopeLevel.Department:
                if (!_currentUser.BranchId.HasValue)
                    return query.Where(_ => false);
                int branchId = _currentUser.BranchId.Value;
                return query.Where(o => o.BranchId == branchId);

            case DataScopeLevel.Self:
                if (!_currentUser.UserId.HasValue)
                    return query.Where(_ => false);
                int userId = _currentUser.UserId.Value;
                return query.Where(o => o.CreatedBy == userId);

            default:
                return query.Where(_ => false);
        }
    }

    public IQueryable<Inventory> ApplyInventoryScope(IQueryable<Inventory> query)
    {
        switch (GetOperationsScopeLevel())
        {
            case DataScopeLevel.Company:
                return query;

            case DataScopeLevel.Branch:
            case DataScopeLevel.Department:
                if (!_currentUser.BranchId.HasValue)
                    return query.Where(_ => false);
                int branchId = _currentUser.BranchId.Value;
                return query.Where(i => i.BranchId == branchId);

            default:
                return query.Where(_ => false);
        }
    }

    public IQueryable<InventoryTransaction> ApplyInventoryTransactionScope(IQueryable<InventoryTransaction> query)
    {
        switch (GetOperationsScopeLevel())
        {
            case DataScopeLevel.Company:
                return query;

            case DataScopeLevel.Branch:
            case DataScopeLevel.Department:
                if (!_currentUser.BranchId.HasValue)
                    return query.Where(_ => false);
                int branchId = _currentUser.BranchId.Value;
                return query.Where(t => t.BranchId == branchId);

            case DataScopeLevel.Self:
                if (!_currentUser.UserId.HasValue)
                    return query.Where(_ => false);
                int userId = _currentUser.UserId.Value;
                return query.Where(t => t.CreatedBy == userId);

            default:
                return query.Where(_ => false);
        }
    }

    public IQueryable<PurchaseOrder> ApplyPurchaseOrderScope(IQueryable<PurchaseOrder> query)
    {
        switch (GetOperationsScopeLevel())
        {
            case DataScopeLevel.Company:
                return query;

            case DataScopeLevel.Branch:
            case DataScopeLevel.Department:
                if (!_currentUser.BranchId.HasValue)
                    return query.Where(_ => false);
                int branchId = _currentUser.BranchId.Value;
                return query.Where(po => po.BranchId == branchId);

            case DataScopeLevel.Self:
                if (!_currentUser.UserId.HasValue)
                    return query.Where(_ => false);
                int userId = _currentUser.UserId.Value;
                return query.Where(po => po.CreatedBy == userId);

            default:
                return query.Where(_ => false);
        }
    }

    public bool CanAccessEmployee(int employeeId, int departmentId, int branchId)
    {
        return GetHrmScopeLevel() switch
        {
            DataScopeLevel.Company => true,
            DataScopeLevel.Branch => _currentUser.BranchId.HasValue && _currentUser.BranchId.Value == branchId,
            DataScopeLevel.Department => _currentUser.DepartmentId.HasValue && _currentUser.DepartmentId.Value == departmentId,
            DataScopeLevel.Self => _currentUser.EmployeeId.HasValue && _currentUser.EmployeeId.Value == employeeId,
            _ => false
        };
    }

    public bool CanAccessDepartment(int departmentId, int branchId)
    {
        return GetHrmScopeLevel() switch
        {
            DataScopeLevel.Company => true,
            DataScopeLevel.Branch => _currentUser.BranchId.HasValue && _currentUser.BranchId.Value == branchId,
            DataScopeLevel.Department => _currentUser.DepartmentId.HasValue && _currentUser.DepartmentId.Value == departmentId,
            _ => false
        };
    }

    public bool CanAccessBranch(int branchId)
    {
        return GetOperationsScopeLevel() switch
        {
            DataScopeLevel.Company => true,
            DataScopeLevel.Branch or DataScopeLevel.Department => _currentUser.BranchId.HasValue && _currentUser.BranchId.Value == branchId,
            _ => false
        };
    }

    public int? ResolveAllowedBranchId(int? requestedBranchId)
    {
        var level = GetOperationsScopeLevel();
        if (level == DataScopeLevel.Company)
            return requestedBranchId;

        if (level is DataScopeLevel.Branch or DataScopeLevel.Department)
            return _currentUser.BranchId;

        return _currentUser.BranchId;
    }
}

