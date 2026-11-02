using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Filters;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Models.ViewModels;

namespace ERP_HRM_CMS.Controllers
{
    [Authorize]
    public class HopDongController : Controller
    {
        private readonly AppDbContext _context;

        public HopDongController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [KiemTraQuyen("hrm.employee.view")]
        public async Task<IActionResult> Index(string? keyword, string? status)
        {
            var query = _context.Contracts
                .Include(c => c.Employee)
                    .ThenInclude(e => e.Department)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim().ToLower();
                query = query.Where(c => c.ContractNumber.ToLower().Contains(keyword) ||
                                         c.Employee.FullName.ToLower().Contains(keyword) ||
                                         c.Employee.EmployeeCode.ToLower().Contains(keyword));
            }

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(c => c.Status == status);

            var contracts = await query.OrderByDescending(c => c.StartDate).ToListAsync();

            // Lọc danh sách cảnh báo sắp hết hạn trong vòng 30 ngày
            var warningDate = DateTime.Today.AddDays(30);
            var expiring = await _context.Contracts
                .Include(c => c.Employee)
                    .ThenInclude(e => e.Department)
                .Where(c => c.Status == "Active" && c.EndDate != null && c.EndDate <= warningDate && c.EndDate >= DateTime.Today)
                .OrderBy(c => c.EndDate)
                .ToListAsync();

            var vm = new ContractListViewModel
            {
                Contracts = contracts,
                ExpiringContracts = expiring,
                Keyword = keyword,
                Status = status
            };

            return View(vm);
        }

        [HttpGet]
        [KiemTraQuyen("hrm.employee.manage")]
        public async Task<IActionResult> Create(int? employeeId)
        {
            await LoadEmployees(employeeId);
            return View(new Contract
            {
                EmployeeId = employeeId ?? 0,
                ContractNumber = $"HDLD-{DateTime.Now:yyyyMMdd}-{new Random().Next(100, 999)}",
                StartDate = DateTime.Today,
                SignDate = DateTime.Today,
                ContractType = "Xác định thời hạn 1 năm",
                BasicSalary = 10000000,
                InsuranceSalary = 5000000,
                Status = "Active"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.employee.manage")]
        public async Task<IActionResult> Create(Contract contract)
        {
            if (await _context.Contracts.AnyAsync(c => c.ContractNumber == contract.ContractNumber))
                ModelState.AddModelError("ContractNumber", "Số hợp đồng này đã tồn tại trên hệ thống.");

            if (contract.EndDate.HasValue && contract.EndDate <= contract.StartDate)
                ModelState.AddModelError("EndDate", "Ngày kết thúc phải lớn hơn ngày bắt đầu hợp đồng.");

            if (ModelState.IsValid)
            {
                contract.CreatedAt = DateTime.Now;
                _context.Contracts.Add(contract);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Lập hợp đồng số {contract.ContractNumber} thành công!";
                return RedirectToAction(nameof(Index));
            }

            await LoadEmployees(contract.EmployeeId);
            return View(contract);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.employee.manage")]
        public async Task<IActionResult> ChamDutHopDong(int id)
        {
            var contract = await _context.Contracts.FindAsync(id);
            if (contract == null) return NotFound();

            contract.Status = "Terminated";
            contract.EndDate = DateTime.Today;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã chấm dứt hiệu lực hợp đồng {contract.ContractNumber}!";
            return RedirectToAction(nameof(Index));
        }

        private async Task LoadEmployees(int? selectedId = null)
        {
            var emps = await _context.Employees
                .Where(e => e.Status == "Active")
                .OrderBy(e => e.FullName)
                .Select(e => new { e.Id, Display = $"{e.FullName} ({e.EmployeeCode})" })
                .ToListAsync();

            ViewBag.Employees = new SelectList(emps, "Id", "Display", selectedId);
        }
    }
}
