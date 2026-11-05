using ERP_HRM_CMS.Filters;
using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Models.ViewModels;
using ERP_HRM_CMS.Services.HRM;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP_HRM_CMS.Controllers
{
    [Authorize]
    public class HopDongController : Controller
    {
        private readonly IContractService _contractService;

        public HopDongController(IContractService contractService)
        {
            _contractService = contractService;
        }

        [HttpGet]
        [KiemTraQuyen("hrm.employee.view")]
        public async Task<IActionResult> Index(
            string? keyword, string? status, int? departmentId, int? expiringDays, int page = 1, int pageSize = 20)
        {
            var filter = new ContractFilterDto
            {
                Keyword = keyword,
                Status = status,
                DepartmentId = departmentId,
                ExpiringWithinDays = expiringDays,
                Page = page,
                PageSize = pageSize
            };

            var paged = await _contractService.GetPagedAsync(filter);
            var expiring = await _contractService.GetExpiringContractsAsync(30);

            var vm = new ContractListViewModel
            {
                Contracts = paged.Select(item => new Contract
                {
                    Id = item.Id,
                    EmployeeId = item.EmployeeId,
                    ContractNumber = item.ContractNumber,
                    ContractType = item.ContractType,
                    StartDate = item.StartDate,
                    EndDate = item.EndDate,
                    BasicSalary = item.BasicSalary,
                    InsuranceSalary = item.InsuranceSalary,
                    Status = item.Status,
                    SignDate = item.SignDate,
                    Employee = new Employee
                    {
                        Id = item.EmployeeId,
                        EmployeeCode = item.EmployeeCode,
                        FullName = item.FullName,
                        Department = new Department { Name = item.DepartmentName },
                        Position = new Position { Name = item.PositionName }
                    }
                }).ToList(),
                ExpiringContracts = expiring.Select(item => new Contract
                {
                    Id = item.Id,
                    EmployeeId = item.EmployeeId,
                    ContractNumber = item.ContractNumber,
                    ContractType = item.ContractType,
                    StartDate = item.StartDate,
                    EndDate = item.EndDate,
                    BasicSalary = item.BasicSalary,
                    InsuranceSalary = item.InsuranceSalary,
                    Status = item.Status,
                    SignDate = item.SignDate,
                    Employee = new Employee
                    {
                        Id = item.EmployeeId,
                        EmployeeCode = item.EmployeeCode,
                        FullName = item.FullName,
                        Department = new Department { Name = item.DepartmentName },
                        Position = new Position { Name = item.PositionName }
                    }
                }).ToList(),
                Keyword = keyword,
                Status = status
            };

            ViewBag.PagedList = paged;
            ViewBag.Filter = filter;
            return View(vm);
        }

        [HttpGet]
        [KiemTraQuyen("hrm.employee.manage")]
        public async Task<IActionResult> Create(int? employeeId)
        {
            ViewBag.Employees = await _contractService.GetEmployeeSelectListAsync(employeeId);
            return View(new Contract
            {
                EmployeeId = employeeId ?? 0,
                ContractNumber = MaChungTu.Sinh("HDLD"),
                StartDate = VietnamTime.Today,
                SignDate = VietnamTime.Today,
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
            if (ModelState.IsValid)
            {
                var (success, error, id) = await _contractService.CreateAsync(contract, null);
                if (success)
                {
                    TempData["SuccessMessage"] = $"Lập hợp đồng số {contract.ContractNumber} thành công!";
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError("", error ?? "Không thể lập hợp đồng.");
            }

            ViewBag.Employees = await _contractService.GetEmployeeSelectListAsync(contract.EmployeeId);
            return View(contract);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.employee.manage")]
        public async Task<IActionResult> ChamDutHopDong(int id)
        {
            var (success, error) = await _contractService.TerminateAsync(id, null);
            if (success)
            {
                TempData["SuccessMessage"] = "Đã chấm dứt hiệu lực hợp đồng thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = error;
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [KiemTraQuyen("hrm.employee.view")]
        public async Task<IActionResult> InHopDong(int id)
        {
            var printModel = await _contractService.GetPrintModelAsync(id);
            if (printModel == null) return NotFound();
            return View(printModel);
        }
    }
}
