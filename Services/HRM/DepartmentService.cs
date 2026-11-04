using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP_HRM_CMS.Services.HRM
{
    /// <summary>Triển khai quản lý phòng ban — cây đệ quy</summary>
    public class DepartmentService : IDepartmentService
    {
        private readonly AppDbContext _context;

        public DepartmentService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<DepartmentTreeNodeDto>> GetTreeAsync()
        {
            var allDepts = await _context.Departments
                .Include(d => d.Branch)
                .Include(d => d.Employees)
                .AsNoTracking()
                .OrderBy(d => d.BranchId).ThenBy(d => d.Code)
                .ToListAsync();

            // Xây dựng cây từ danh sách phẳng
            var lookup = allDepts.ToDictionary(d => d.Id);
            var roots = new List<DepartmentTreeNodeDto>();

            foreach (var dept in allDepts)
            {
                var node = new DepartmentTreeNodeDto
                {
                    Id = dept.Id,
                    Code = dept.Code,
                    Name = dept.Name,
                    ParentId = dept.ParentId,
                    BranchId = dept.BranchId,
                    BranchName = dept.Branch?.Name ?? "",
                    IsActive = dept.IsActive,
                    EmployeeCount = dept.Employees.Count
                };

                if (dept.ParentId == null)
                {
                    roots.Add(node);
                }
                else
                {
                    // Tìm parent node trong danh sách đã xử lý
                    var parentNode = FindNode(roots, dept.ParentId.Value);
                    if (parentNode != null)
                        parentNode.Children.Add(node);
                    else
                        roots.Add(node); // Fallback: nếu parent chưa có thì thêm vào root
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

            // Không cho phòng ban làm parent của chính nó
            if (model.ParentId == id)
                return (false, "Phòng ban không thể là cấp trên của chính nó.");

            dept.Code = model.Code;
            dept.Name = model.Name;
            dept.BranchId = model.BranchId;
            dept.ParentId = model.ParentId;

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> ToggleActiveAsync(int id)
        {
            var dept = await _context.Departments
                .Include(d => d.Employees)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (dept == null) return (false, "Không tìm thấy phòng ban.");

            if (dept.IsActive && dept.Employees.Any(e => e.Status == "Active"))
                return (false, "Không thể ngừng hoạt động phòng ban đang có nhân viên.");

            dept.IsActive = !dept.IsActive;
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> MoveAsync(int id, int newParentId, int newBranchId)
        {
            var dept = await _context.Departments.FindAsync(id);
            if (dept == null) return (false, "Không tìm thấy phòng ban.");

            if (newParentId == id) return (false, "Phòng ban không thể là cấp trên của chính nó.");

            dept.ParentId = newParentId == 0 ? null : newParentId;
            dept.BranchId = newBranchId;
            await _context.SaveChangesAsync();
            return (true, null);
        }

        // === HELPER: Tìm node trong cây ===
        private DepartmentTreeNodeDto? FindNode(List<DepartmentTreeNodeDto> nodes, int id)
        {
            foreach (var node in nodes)
            {
                if (node.Id == id) return node;
                var found = FindNode(node.Children, id);
                if (found != null) return found;
            }
            return null;
        }

        // === HELPER: Làm phẳng cây (cho dropdown có thụt lề) ===
        private void FlattenTree(List<DepartmentTreeNodeDto> nodes, List<DepartmentFlatDto> flat, int level)
        {
            foreach (var node in nodes)
            {
                var prefix = new string('─', level); // ── thụt lề
                flat.Add(new DepartmentFlatDto
                {
                    Id = node.Id,
                    Code = node.Code,
                    Name = node.Name,
                    DisplayName = level > 0 ? $"{'│'} {prefix} {node.Name}" : node.Name,
                    Level = level,
                    IsActive = node.IsActive,
                    BranchId = node.BranchId,
                    BranchName = node.BranchName
                });
                FlattenTree(node.Children, flat, level + 1);
            }
        }
    }
}
