using ERP.Application.Common.Interfaces;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ERP_HRM_CMS.Services.HRM
{
    public class AssetManagementService : IAssetManagementService
    {
        private readonly AppDbContext _context;
        private readonly IDataScopeService? _dataScope;

        public AssetManagementService(AppDbContext context, IDataScopeService? dataScope = null)
        {
            _context = context;
            _dataScope = dataScope;
        }

        public async Task<PaginatedList<AssetListItemDto>> GetAssetsPagedAsync(AssetFilterDto filter)
        {
            var query = _context.CompanyAssets
                .Include(a => a.Branch)
                .Include(a => a.Assignments.Where(asg => asg.Status == "Active"))
                    .ThenInclude(asg => asg.Employee)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var kw = filter.Keyword.Trim();
                query = query.Where(a => a.AssetCode.Contains(kw) ||
                                         a.AssetName.Contains(kw) ||
                                         (a.SerialNumber != null && a.SerialNumber.Contains(kw)));
            }

            if (!string.IsNullOrWhiteSpace(filter.AssetType))
                query = query.Where(a => a.AssetType == filter.AssetType);

            if (!string.IsNullOrWhiteSpace(filter.Status))
                query = query.Where(a => a.Status == filter.Status);

            if (filter.BranchId.HasValue && filter.BranchId > 0)
                query = query.Where(a => a.BranchId == filter.BranchId.Value);

            var projected = query
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new AssetListItemDto
                {
                    Id = a.Id,
                    AssetCode = a.AssetCode,
                    AssetName = a.AssetName,
                    AssetType = a.AssetType,
                    SerialNumber = a.SerialNumber,
                    Specification = a.Specification,
                    PurchaseDate = a.PurchaseDate,
                    PurchasePrice = a.PurchasePrice,
                    WarrantyExpiryDate = a.WarrantyExpiryDate,
                    Status = a.Status,
                    BranchName = a.Branch != null ? a.Branch.Name : null,
                    CurrentEmployeeId = a.Assignments.Where(asg => asg.Status == "Active").Select(asg => (int?)asg.EmployeeId).FirstOrDefault(),
                    CurrentEmployeeName = a.Assignments.Where(asg => asg.Status == "Active").Select(asg => asg.Employee.FullName).FirstOrDefault(),
                    CurrentEmployeeCode = a.Assignments.Where(asg => asg.Status == "Active").Select(asg => asg.Employee.EmployeeCode).FirstOrDefault(),
                    CurrentAssignedDate = a.Assignments.Where(asg => asg.Status == "Active").Select(asg => (DateTime?)asg.AssignedDate).FirstOrDefault()
                });

            return await PaginatedList<AssetListItemDto>.CreateAsync(projected, filter.Page, filter.PageSize);
        }

        public async Task<CompanyAsset?> GetAssetByIdAsync(int id)
        {
            return await _context.CompanyAssets
                .Include(a => a.Branch)
                .Include(a => a.Assignments)
                    .ThenInclude(asg => asg.Employee)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<List<AssetAssignmentDto>> GetAssetAssignmentsAsync(int assetId)
        {
            return await _context.EmployeeAssetAssignments
                .Include(asg => asg.Asset)
                .Include(asg => asg.Employee)
                .AsNoTracking()
                .Where(asg => asg.AssetId == assetId)
                .OrderByDescending(asg => asg.AssignedDate)
                .Select(asg => new AssetAssignmentDto
                {
                    Id = asg.Id,
                    AssetId = asg.AssetId,
                    AssetCode = asg.Asset.AssetCode,
                    AssetName = asg.Asset.AssetName,
                    EmployeeId = asg.EmployeeId,
                    EmployeeCode = asg.Employee.EmployeeCode,
                    EmployeeName = asg.Employee.FullName,
                    AssignedDate = asg.AssignedDate,
                    ConditionOnAssign = asg.ConditionOnAssign,
                    ReturnedDate = asg.ReturnedDate,
                    ConditionOnReturn = asg.ConditionOnReturn,
                    Status = asg.Status,
                    Note = asg.Note
                })
                .ToListAsync();
        }

        public async Task<List<AssetAssignmentDto>> GetEmployeeAssetsAsync(int employeeId)
        {
            return await _context.EmployeeAssetAssignments
                .Include(asg => asg.Asset)
                .Include(asg => asg.Employee)
                .AsNoTracking()
                .Where(asg => asg.EmployeeId == employeeId)
                .OrderByDescending(asg => asg.AssignedDate)
                .Select(asg => new AssetAssignmentDto
                {
                    Id = asg.Id,
                    AssetId = asg.AssetId,
                    AssetCode = asg.Asset.AssetCode,
                    AssetName = asg.Asset.AssetName,
                    EmployeeId = asg.EmployeeId,
                    EmployeeCode = asg.Employee.EmployeeCode,
                    EmployeeName = asg.Employee.FullName,
                    AssignedDate = asg.AssignedDate,
                    ConditionOnAssign = asg.ConditionOnAssign,
                    ReturnedDate = asg.ReturnedDate,
                    ConditionOnReturn = asg.ConditionOnReturn,
                    Status = asg.Status,
                    Note = asg.Note
                })
                .ToListAsync();
        }

        public async Task<(bool Success, string? Error, int Id)> CreateAssetAsync(CompanyAsset asset, int? currentUserId)
        {
            if (await _context.CompanyAssets.AnyAsync(a => a.AssetCode == asset.AssetCode))
                return (false, "Mã tài sản đã tồn tại trên hệ thống.", 0);

            asset.CreatedAt = VietnamTime.Now;
            asset.CreatedBy = currentUserId?.ToString();
            asset.Status = "Available";

            _context.CompanyAssets.Add(asset);
            await _context.SaveChangesAsync();

            return (true, null, asset.Id);
        }

        public async Task<(bool Success, string? Error)> UpdateAssetAsync(int id, CompanyAsset model, int? currentUserId)
        {
            var asset = await _context.CompanyAssets.FindAsync(id);
            if (asset == null) return (false, "Không tìm thấy tài sản.");

            if (await _context.CompanyAssets.AnyAsync(a => a.AssetCode == model.AssetCode && a.Id != id))
                return (false, "Mã tài sản đã tồn tại cho thiết bị khác.");

            asset.AssetName = model.AssetName;
            asset.AssetType = model.AssetType;
            asset.SerialNumber = model.SerialNumber;
            asset.Specification = model.Specification;
            asset.PurchaseDate = model.PurchaseDate;
            asset.PurchasePrice = model.PurchasePrice;
            asset.WarrantyExpiryDate = model.WarrantyExpiryDate;
            asset.BranchId = model.BranchId;
            asset.UpdatedAt = VietnamTime.Now;
            asset.UpdatedBy = currentUserId?.ToString();

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> AssignAssetAsync(
            int assetId, int employeeId, string? conditionOnAssign, int? assignedByUserId, string? note)
        {
            var asset = await _context.CompanyAssets.FindAsync(assetId);
            if (asset == null) return (false, "Không tìm thấy tài sản.");

            if (asset.Status != "Available")
                return (false, $"Tài sản đang ở trạng thái '{asset.Status}', không thể cấp phát.");

            var emp = await _context.Employees.FindAsync(employeeId);
            if (emp == null) return (false, "Không tìm thấy nhân viên được cấp phát.");

            if (emp.Status == "Resigned")
                return (false, "Không thể cấp phát tài sản cho nhân viên đã thôi việc.");

            // Cập nhật trạng thái tài sản
            asset.Status = "Assigned";
            asset.UpdatedAt = VietnamTime.Now;

            // Tạo lượt bàn giao
            var assignment = new EmployeeAssetAssignment
            {
                AssetId = assetId,
                EmployeeId = employeeId,
                AssignedDate = VietnamTime.Today,
                ConditionOnAssign = conditionOnAssign ?? "Tốt, hoạt động bình thường",
                Status = "Active",
                AssignedById = assignedByUserId,
                Note = note,
                CreatedAt = VietnamTime.Now
            };

            _context.EmployeeAssetAssignments.Add(assignment);
            await _context.SaveChangesAsync();

            return (true, null);
        }

        public async Task<(bool Success, string? Error)> ReturnAssetAsync(
            int assignmentId, string conditionOnReturn, string returnStatus, int? returnedByUserId, string? note)
        {
            var assignment = await _context.EmployeeAssetAssignments
                .Include(asg => asg.Asset)
                .FirstOrDefaultAsync(asg => asg.Id == assignmentId);

            if (assignment == null) return (false, "Không tìm thấy thông tin cấp phát.");

            if (assignment.Status != "Active")
                return (false, "Tài sản này đã được thu hồi trước đó.");

            assignment.Status = "Returned";
            assignment.ReturnedDate = VietnamTime.Today;
            assignment.ConditionOnReturn = conditionOnReturn;
            assignment.ReturnedById = returnedByUserId;
            if (!string.IsNullOrWhiteSpace(note))
                assignment.Note = string.IsNullOrWhiteSpace(assignment.Note) ? note : $"{assignment.Note} | Thu hồi: {note}";
            assignment.UpdatedAt = VietnamTime.Now;

            // Cập nhật trạng thái của tài sản (Available, UnderRepair, Liquidated, Lost)
            assignment.Asset.Status = string.IsNullOrWhiteSpace(returnStatus) ? "Available" : returnStatus;
            assignment.Asset.UpdatedAt = VietnamTime.Now;

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<bool> HasUnreturnedAssetsAsync(int employeeId)
        {
            return await _context.EmployeeAssetAssignments
                .AnyAsync(asg => asg.EmployeeId == employeeId && asg.Status == "Active");
        }

        public async Task<List<AssetAssignmentDto>> GetUnreturnedAssetsAsync(int employeeId)
        {
            return await _context.EmployeeAssetAssignments
                .Include(asg => asg.Asset)
                .Include(asg => asg.Employee)
                .AsNoTracking()
                .Where(asg => asg.EmployeeId == employeeId && asg.Status == "Active")
                .OrderBy(asg => asg.AssignedDate)
                .Select(asg => new AssetAssignmentDto
                {
                    Id = asg.Id,
                    AssetId = asg.AssetId,
                    AssetCode = asg.Asset.AssetCode,
                    AssetName = asg.Asset.AssetName,
                    EmployeeId = asg.EmployeeId,
                    EmployeeCode = asg.Employee.EmployeeCode,
                    EmployeeName = asg.Employee.FullName,
                    AssignedDate = asg.AssignedDate,
                    ConditionOnAssign = asg.ConditionOnAssign,
                    Status = asg.Status,
                    Note = asg.Note
                })
                .ToListAsync();
        }

        public async Task<AssetDropdownsDto> GetDropdownsAsync()
        {
            var branches = await _context.Branches.AsNoTracking()
                .Where(b => b.IsActive)
                .OrderBy(b => b.Name)
                .Select(b => new SelectListItem { Value = b.Id.ToString(), Text = b.Name })
                .ToListAsync();

            var assets = await _context.CompanyAssets.AsNoTracking()
                .Where(a => a.Status == "Available")
                .OrderBy(a => a.AssetName)
                .Select(a => new SelectListItem { Value = a.Id.ToString(), Text = $"{a.AssetCode} - {a.AssetName} ({a.AssetType})" })
                .ToListAsync();

            var emps = await _context.Employees.AsNoTracking()
                .Where(e => e.Status == "Active" || e.Status == "Probation")
                .OrderBy(e => e.FullName)
                .Select(e => new SelectListItem { Value = e.Id.ToString(), Text = $"{e.EmployeeCode} - {e.FullName} ({e.Department.Name})" })
                .ToListAsync();

            return new AssetDropdownsDto
            {
                Branches = branches,
                AvailableAssets = assets,
                Employees = emps
            };
        }
    }
}
