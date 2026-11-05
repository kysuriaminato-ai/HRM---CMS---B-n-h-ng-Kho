using ERP.Application.Common.Interfaces;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP_HRM_CMS.Services.HRM
{
    public class DepartmentService : IDepartmentService
    {
        private readonly AppDbContext _context;
        private readonly IDataScopeService? _dataScope;

        public DepartmentService(AppDbContext context, IDataScopeService? dataScope = null)
        {
            _context = context;
            _dataScope = dataScope;
        }

        public async Task<List<DepartmentTreeNodeDto>> GetTreeAsync()
        {
            var allDepts = await _context.Departments
                .Include(d => d.Branch)
                .Include(d => d.Employees.Where(e => e.Status == "Active" || e.Status == "Probation"))
                    .ThenInclude(e => e.Position)
                .AsNoTracking()
                .OrderBy(d => d.BranchId).ThenBy(d => d.Code)
                .ToListAsync();

            var roots = new List<DepartmentTreeNodeDto>();

            foreach (var dept in allDepts)
            {
                // Tìm trưởng phòng hoặc nhân sự cấp quản lý cao nhất
                var manager = dept.Employees
                    .OrderBy(e => e.Position.Name.Contains("Trưởng") ? 0 :
                                  e.Position.Name.Contains("Giám đốc") ? 1 :
                                  e.Position.Name.Contains("Phó") ? 2 :
                                  e.Position.Name.Contains("Lead") ? 3 : 4)
                    .ThenBy(e => e.HireDate)
                    .FirstOrDefault();

                var node = new DepartmentTreeNodeDto
                {
                    Id = dept.Id,
                    Code = dept.Code,
                    Name = dept.Name,
                    ParentId = dept.ParentId,
                    BranchId = dept.BranchId,
                    BranchName = dept.Branch?.Name ?? "",
                    IsActive = dept.IsActive,
                    EmployeeCount = dept.Employees.Count,
                    ManagerName = manager?.FullName,
                    ManagerTitle = manager?.Position?.Name,
                    ManagerAvatar = manager?.AvatarUrl
                };

                if (dept.ParentId == null)
                {
                    roots.Add(node);
                }
                else
                {
                    var parentNode = FindNode(roots, dept.ParentId.Value);
                    if (parentNode != null)
                        parentNode.Children.Add(node);
                    else
                        roots.Add(node);
                }
            }

            return roots;
        }

        public async Task<List<DepartmentFlatDto>> GetFlatListAsync()
        {
            var tree = await GetTreeAsync();
            var flat = new List<DepartmentFlatDto>();
            FlattenTree(tree, flat, 0);
            return flat;
        }

        public async Task<Department?> GetByIdAsync(int id)
        {
            return await _context.Departments
                .Include(d => d.Branch)
                .Include(d => d.Employees)
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<DepartmentDrawerDetailDto?> GetDrawerDetailAsync(int departmentId)
        {
            var dept = await _context.Departments
                .Include(d => d.Branch)
                .Include(d => d.Parent)
                .Include(d => d.Employees)
                    .ThenInclude(e => e.Position)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == departmentId);

            if (dept == null) return null;

            var activeEmployees = dept.Employees
                .Where(e => e.Status == "Active" || e.Status == "Probation")
                .ToList();

            var manager = activeEmployees
                .OrderBy(e => e.Position.Name.Contains("Trưởng") ? 0 :
                              e.Position.Name.Contains("Giám đốc") ? 1 :
                              e.Position.Name.Contains("Phó") ? 2 :
                              e.Position.Name.Contains("Lead") ? 3 : 4)
                .ThenBy(e => e.HireDate)
                .FirstOrDefault();

            return new DepartmentDrawerDetailDto
            {
                Id = dept.Id,
                Code = dept.Code,
                Name = dept.Name,
                BranchName = dept.Branch?.Name ?? "",
                ParentDepartmentName = dept.Parent?.Name,
                IsActive = dept.IsActive,
                StaffingQuota = Math.Max(20, activeEmployees.Count + 5),
                CurrentHeadcount = activeEmployees.Count,
                ManagerName = manager?.FullName ?? "Chưa bổ nhiệm",
                ManagerTitle = manager?.Position?.Name ?? "Trưởng đơn vị",
                ManagerAvatar = manager?.AvatarUrl,
                ManagerEmail = manager?.Email,
                ManagerPhone = manager?.Phone,
                Members = dept.Employees
                    .OrderBy(e => e.Status == "Active" ? 0 : e.Status == "Probation" ? 1 : 2)
                    .ThenBy(e => e.FullName)
                    .Select(e => new DepartmentMemberDto
                    {
                        Id = e.Id,
                        EmployeeCode = e.EmployeeCode,
                        FullName = e.FullName,
                        PositionName = e.Position?.Name ?? "Chưa phân",
                        Email = e.Email,
                        Phone = e.Phone,
                        AvatarUrl = e.AvatarUrl,
                        Status = e.Status,
                        HireDate = e.HireDate
                    })
                    .ToList()
            };
        }

        public async Task<(bool Success, string? Error)> CreateAsync(Department dept)
        {
            if (await _context.Departments.AnyAsync(d => d.Code == dept.Code))
                return (false, "Mã phòng ban đã tồn tại.");

            dept.CreatedAt = VietnamTime.Now;
            _context.Departments.Add(dept);
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> UpdateAsync(int id, Department model)
        {
            var dept = await _context.Departments.FindAsync(id);
            if (dept == null) return (false, "Không tìm thấy phòng ban.");

            if (await _context.Departments.AnyAsync(d => d.Code == model.Code && d.Id != id))
                return (false, "Mã phòng ban đã được dùng bởi phòng ban khác.");

            if (model.ParentId == id)
                return (false, "Phòng ban không thể là cấp trên của chính nó.");

            dept.Code = model.Code.Trim();
            dept.Name = model.Name.Trim();
            dept.ParentId = model.ParentId;
            dept.BranchId = model.BranchId;
            dept.IsActive = model.IsActive;

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> ToggleActiveAsync(int id)
        {
            var dept = await _context.Departments.FindAsync(id);
            if (dept == null) return (false, "Không tìm thấy phòng ban.");

            dept.IsActive = !dept.IsActive;
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> MoveAsync(int id, int newParentId, int newBranchId)
        {
            var dept = await _context.Departments.FindAsync(id);
            if (dept == null) return (false, "Không tìm thấy phòng ban.");

            if (newParentId == id)
                return (false, "Không thể chuyển phòng ban vào chính nó.");

            dept.ParentId = newParentId > 0 ? newParentId : null;
            if (newBranchId > 0)
                dept.BranchId = newBranchId;

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> DeleteAsync(int id)
        {
            var dept = await _context.Departments.Include(d => d.Employees).FirstOrDefaultAsync(d => d.Id == id);
            if (dept == null) return (false, "Không tìm thấy phòng ban.");

            if (dept.Employees.Any())
                return (false, "Không thể xóa phòng ban đang có nhân viên trực thuộc!");

            _context.Departments.Remove(dept);
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<List<Branch>> GetBranchesAsync()
        {
            return await _context.Branches.Include(b => b.Company).AsNoTracking().ToListAsync();
        }

        public async Task<List<Position>> GetPositionsAsync()
        {
            return await _context.Positions.AsNoTracking().OrderBy(p => p.Name).ToListAsync();
        }

        public async Task<(bool Success, string? Error)> CreatePositionAsync(Position pos)
        {
            if (await _context.Positions.AnyAsync(p => p.Code == pos.Code))
                return (false, "Mã chức vụ đã tồn tại.");

            pos.CreatedAt = VietnamTime.Now;
            _context.Positions.Add(pos);
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>> GetDepartmentSelectListAsync(int? selectedId = null)
        {
            var flats = await GetFlatListAsync();
            return flats.Select(d => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = d.Id.ToString(),
                Text = d.DisplayName,
                Selected = selectedId.HasValue && d.Id == selectedId.Value
            }).ToList();
        }

        private static DepartmentTreeNodeDto? FindNode(List<DepartmentTreeNodeDto> nodes, int id)
        {
            foreach (var node in nodes)
            {
                if (node.Id == id) return node;
                var found = FindNode(node.Children, id);
                if (found != null) return found;
            }
            return null;
        }

        private static void FlattenTree(List<DepartmentTreeNodeDto> nodes, List<DepartmentFlatDto> result, int level)
        {
            foreach (var node in nodes)
            {
                var indent = new string('—', level * 2);
                result.Add(new DepartmentFlatDto
                {
                    Id = node.Id,
                    Code = node.Code,
                    Name = node.Name,
                    DisplayName = level > 0 ? $"{indent} {node.Name}" : node.Name,
                    Level = level,
                    IsActive = node.IsActive,
                    BranchId = node.BranchId,
                    BranchName = node.BranchName
                });
                if (node.Children.Count > 0)
                    FlattenTree(node.Children, result, level + 1);
            }
        }
    }
}
