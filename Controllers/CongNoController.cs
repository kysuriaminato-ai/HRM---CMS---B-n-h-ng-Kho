using ERP.Application.Common.Interfaces;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Filters;
using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP_HRM_CMS.Controllers
{
    [Authorize]
    public class CongNoController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public CongNoController(AppDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        private int CurrentUserId => _currentUser.GetRequiredUserId();

        // 1. DASHBOARD CÔNG NỢ & BÁO CÁO TUỔI NỢ (AGING REPORT)
        [HttpGet]
        [KiemTraQuyen("debt.view")]
        public async Task<IActionResult> Index(string? type, string? status, string? aging, string? keyword)
        {
            var query = _context.DebtRecords
                .Include(d => d.Supplier)
                .Include(d => d.DebtPayments)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(type))
            {
                query = query.Where(d => d.DebtType == type);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (WorkflowStatusCodes.TryParse(WorkflowStatusCodes.Debt, status, out var debtStatus))
                    query = query.Where(d => d.Status == debtStatus);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim().ToLower();
                query = query.Where(d => d.DocumentCode.ToLower().Contains(keyword) ||
                                         d.PartnerName.ToLower().Contains(keyword) ||
                                         (d.PartnerPhone != null && d.PartnerPhone.Contains(keyword)));
            }

            var now = VietnamTime.Today;
            var date30DaysAgo = now.AddDays(-30);
            var date60DaysAgo = now.AddDays(-60);

            // Lọc theo tuổi nợ
            if (!string.IsNullOrWhiteSpace(aging))
            {
                switch (aging)
                {
                    case "CURRENT": // Chưa đến hạn
                        query = query.Where(d => d.DueDate >= now && d.RemainingAmount > 0);
                        break;
                    case "1_30": // Quá hạn 1 - 30 ngày
                        query = query.Where(d => d.DueDate < now && d.DueDate >= date30DaysAgo && d.RemainingAmount > 0);
                        break;
                    case "31_60": // Quá hạn 31 - 60 ngày
                        query = query.Where(d => d.DueDate < date30DaysAgo && d.DueDate >= date60DaysAgo && d.RemainingAmount > 0);
                        break;
                    case "OVER_60": // Quá hạn trên 60 ngày (Nợ xấu / rủi ro cao)
                        query = query.Where(d => d.DueDate < date60DaysAgo && d.RemainingAmount > 0);
                        break;
                }
            }

            var allRecords = await _context.DebtRecords.AsNoTracking().ToListAsync();
            var filteredRecords = await query.OrderByDescending(d => d.CreatedAt).ToListAsync();

            // Tính toán số liệu Dashboard
            ViewBag.TotalReceivable = allRecords.Where(d => d.DebtType == "RECEIVABLE").Sum(d => d.RemainingAmount);
            ViewBag.TotalPayable = allRecords.Where(d => d.DebtType == "PAYABLE").Sum(d => d.RemainingAmount);

            // Báo cáo Tuổi nợ (Aging)
            var unpaidRecords = allRecords.Where(d => d.RemainingAmount > 0).ToList();
            ViewBag.AgingCurrent = unpaidRecords.Where(d => d.DueDate >= now).Sum(d => d.RemainingAmount);
            ViewBag.Aging1_30 = unpaidRecords.Where(d => d.DueDate < now && (now - d.DueDate.Date).TotalDays <= 30).Sum(d => d.RemainingAmount);
            ViewBag.Aging31_60 = unpaidRecords.Where(d => (now - d.DueDate.Date).TotalDays > 30 && (now - d.DueDate.Date).TotalDays <= 60).Sum(d => d.RemainingAmount);
            ViewBag.AgingOver60 = unpaidRecords.Where(d => (now - d.DueDate.Date).TotalDays > 60).Sum(d => d.RemainingAmount);

            ViewBag.SelectedType = type ?? "ALL";
            ViewBag.SelectedStatus = status;
            ViewBag.SelectedAging = aging;
            ViewBag.Keyword = keyword;

            return View(filteredRecords);
        }

        // 2. GHI NHẬN THANH TOÁN / THU CÔNG NỢ
        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("debt.manage")]
        public async Task<IActionResult> ThanhToan(long debtRecordId, decimal amount, string paymentMethod, string? note)
        {
            if (amount <= 0)
            {
                TempData["ErrorMessage"] = "Số tiền thanh toán phải lớn hơn 0.";
                return RedirectToAction(nameof(Index));
            }

            var debt = await _context.DebtRecords
                .Include(d => d.Supplier)
                .FirstOrDefaultAsync(d => d.Id == debtRecordId);

            if (debt == null) return NotFound();

            if (amount > debt.RemainingAmount)
            {
                TempData["ErrorMessage"] = $"Số tiền thanh toán ({amount:N0} đ) vượt quá số dư còn nợ ({debt.RemainingAmount:N0} đ).";
                return RedirectToAction(nameof(Index));
            }

            string codePrefix = debt.DebtType == "RECEIVABLE" ? "PT" : "PC";
            string paymentCode = $"{codePrefix}{VietnamTime.Now:yyMMddHHmmss}";

            var payment = new DebtPayment
            {
                DebtRecordId = debt.Id,
                PaymentCode = paymentCode,
                Amount = amount,
                PaymentMethod = paymentMethod ?? "BANK_TRANSFER",
                PaymentDate = VietnamTime.Now,
                Note = note,
                CreatedBy = CurrentUserId
            };
            _context.DebtPayments.Add(payment);

            // Cập nhật số tiền
            debt.PaidAmount += amount;
            debt.RemainingAmount = Math.Max(0, debt.TotalAmount - debt.PaidAmount);

            if (debt.RemainingAmount <= 0)
            {
                debt.Status = DebtStatus.Paid;
            }
            else
            {
                debt.Status = DebtStatus.PartiallyPaid;
            }

            // Nếu là nợ phải trả NCC, trừ tổng nợ NCC
            if (debt.DebtType == "PAYABLE" && debt.Supplier != null)
            {
                debt.Supplier.TotalDebt = Math.Max(0, debt.Supplier.TotalDebt - amount);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Ghi nhận phiếu thanh toán {paymentCode} số tiền {amount:N0} đ thành công!";
            return RedirectToAction(nameof(Index), new { type = debt.DebtType });
        }
    }
}
