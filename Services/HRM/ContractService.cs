using ERP.Application.Common.Interfaces;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ERP_HRM_CMS.Services.HRM
{
    public class ContractService : IContractService
    {
        private readonly AppDbContext _context;
        private readonly IDataScopeService? _dataScope;

        public ContractService(AppDbContext context, IDataScopeService? dataScope = null)
        {
            _context = context;
            _dataScope = dataScope;
        }

        public async Task<PaginatedList<ContractListItemDto>> GetPagedAsync(ContractFilterDto filter)
        {
            var baseQuery = _context.Contracts.AsNoTracking();
            var query = _context.Contracts
                .Include(c => c.Employee)
                    .ThenInclude(e => e.Department)
                .Include(c => c.Employee)
                    .ThenInclude(e => e.Position)
                .AsNoTracking()
                .AsQueryable();

            if (_dataScope != null)
            {
                var scopedEmps = _dataScope.ApplyEmployeeScope(_context.Employees.AsNoTracking());
                var scopedEmpIds = scopedEmps.Select(e => e.Id);
                query = query.Where(c => scopedEmpIds.Contains(c.EmployeeId));
            }

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var kw = filter.Keyword.Trim();
                query = query.Where(c => c.ContractNumber.Contains(kw) ||
                                         c.Employee.FullName.Contains(kw) ||
                                         c.Employee.EmployeeCode.Contains(kw));
            }

            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                query = query.Where(c => c.Status == filter.Status);
            }

            if (filter.DepartmentId.HasValue && filter.DepartmentId > 0)
            {
                query = query.Where(c => c.Employee.DepartmentId == filter.DepartmentId.Value);
            }

            if (filter.ExpiringWithinDays.HasValue && filter.ExpiringWithinDays > 0)
            {
                var today = VietnamTime.Today;
                var maxDate = today.AddDays(filter.ExpiringWithinDays.Value);
                query = query.Where(c => c.Status == "Active" && c.EndDate.HasValue && c.EndDate.Value >= today && c.EndDate.Value <= maxDate);
            }

            var projected = query
                .OrderByDescending(c => c.StartDate)
                .Select(c => new ContractListItemDto
                {
                    Id = c.Id,
                    EmployeeId = c.EmployeeId,
                    EmployeeCode = c.Employee.EmployeeCode,
                    FullName = c.Employee.FullName,
                    DepartmentName = c.Employee.Department.Name,
                    PositionName = c.Employee.Position.Name,
                    ContractNumber = c.ContractNumber,
                    ContractType = c.ContractType,
                    StartDate = c.StartDate,
                    EndDate = c.EndDate,
                    BasicSalary = c.BasicSalary,
                    InsuranceSalary = c.InsuranceSalary,
                    Status = c.Status,
                    SignDate = c.SignDate
                });

            return await PaginatedList<ContractListItemDto>.CreateAsync(projected, filter.Page, filter.PageSize);
        }

        public async Task<List<ContractListItemDto>> GetExpiringContractsAsync(int days = 30)
        {
            var today = VietnamTime.Today;
            var maxDate = today.AddDays(days);

            var query = _context.Contracts
                .Include(c => c.Employee)
                    .ThenInclude(e => e.Department)
                .Include(c => c.Employee)
                    .ThenInclude(e => e.Position)
                .AsNoTracking()
                .Where(c => c.Status == "Active" && c.EndDate.HasValue && c.EndDate.Value >= today && c.EndDate.Value <= maxDate);

            if (_dataScope != null)
            {
                var scopedEmps = _dataScope.ApplyEmployeeScope(_context.Employees.AsNoTracking());
                var scopedEmpIds = scopedEmps.Select(e => e.Id);
                query = query.Where(c => scopedEmpIds.Contains(c.EmployeeId));
            }

            return await query
                .OrderBy(c => c.EndDate)
                .Select(c => new ContractListItemDto
                {
                    Id = c.Id,
                    EmployeeId = c.EmployeeId,
                    EmployeeCode = c.Employee.EmployeeCode,
                    FullName = c.Employee.FullName,
                    DepartmentName = c.Employee.Department.Name,
                    PositionName = c.Employee.Position.Name,
                    ContractNumber = c.ContractNumber,
                    ContractType = c.ContractType,
                    StartDate = c.StartDate,
                    EndDate = c.EndDate,
                    BasicSalary = c.BasicSalary,
                    InsuranceSalary = c.InsuranceSalary,
                    Status = c.Status,
                    SignDate = c.SignDate
                })
                .ToListAsync();
        }

        public async Task<Contract?> GetByIdAsync(int id)
        {
            return await _context.Contracts
                .Include(c => c.Employee)
                    .ThenInclude(e => e.Department)
                .Include(c => c.Employee)
                    .ThenInclude(e => e.Position)
                .Include(c => c.Employee)
                    .ThenInclude(e => e.EmployeeDetail)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<ContractPrintModel?> GetPrintModelAsync(int id)
        {
            var c = await GetByIdAsync(id);
            if (c == null) return null;

            var comp = await _context.Companies.AsNoTracking().FirstOrDefaultAsync();

            return new ContractPrintModel
            {
                ContractId = c.Id,
                ContractNumber = c.ContractNumber,
                CompanyName = comp?.Name ?? "CÔNG TY CỔ PHẦN CÔNG NGHỆ & THƯƠNG MẠI VIỆT NAM",
                CompanyAddress = comp?.Address ?? "Tầng 12, Tòa nhà Enterprise Plaza, Hà Nội",
                CompanyPhone = comp?.Phone ?? "024.3999.8888",
                RepresentativeName = "Nguyễn Văn Giám Đốc",
                RepresentativePosition = "Tổng Giám đốc",
                EmployeeCode = c.Employee.EmployeeCode,
                FullName = c.Employee.FullName,
                DateOfBirth = c.Employee.DateOfBirth,
                Gender = c.Employee.Gender,
                IdentityCard = c.Employee.IdentityCard,
                PermanentAddress = c.Employee.EmployeeDetail?.PermanentAddress ?? c.Employee.EmployeeDetail?.CurrentAddress,
                Phone = c.Employee.Phone,
                DepartmentName = c.Employee.Department?.Name ?? "",
                PositionName = c.Employee.Position?.Name ?? "",
                ContractType = c.ContractType,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                BasicSalary = c.BasicSalary,
                InsuranceSalary = c.InsuranceSalary,
                SignDate = c.SignDate
            };
        }

        public async Task<(bool Success, string? Error, int Id)> CreateAsync(Contract contract, int? currentUserId)
        {
            if (await _context.Contracts.AnyAsync(c => c.ContractNumber == contract.ContractNumber))
                return (false, "Số hợp đồng này đã tồn tại trên hệ thống.", 0);

            if (contract.EndDate.HasValue && contract.EndDate <= contract.StartDate)
                return (false, "Ngày kết thúc phải lớn hơn ngày bắt đầu hợp đồng.", 0);

            contract.CreatedAt = VietnamTime.Now;
            _context.Contracts.Add(contract);
            await _context.SaveChangesAsync();

            return (true, null, contract.Id);
        }

        public async Task<(bool Success, string? Error)> TerminateAsync(int id, int? currentUserId)
        {
            var contract = await _context.Contracts.FindAsync(id);
            if (contract == null) return (false, "Không tìm thấy hợp đồng.");

            contract.Status = "Terminated";
            contract.EndDate = VietnamTime.Today;
            await _context.SaveChangesAsync();

            return (true, null);
        }

        public async Task<List<SelectListItem>> GetEmployeeSelectListAsync(int? selectedId = null)
        {
            var empsQuery = _context.Employees.AsNoTracking().Where(e => e.Status == "Active" || e.Status == "Probation");
            if (_dataScope != null)
            {
                empsQuery = _dataScope.ApplyEmployeeScope(empsQuery);
            }

            return await empsQuery
                .OrderBy(e => e.FullName)
                .Select(e => new SelectListItem
                {
                    Value = e.Id.ToString(),
                    Text = $"{e.EmployeeCode} - {e.FullName}",
                    Selected = selectedId.HasValue && e.Id == selectedId.Value
                })
                .ToListAsync();
        }
    }
}
