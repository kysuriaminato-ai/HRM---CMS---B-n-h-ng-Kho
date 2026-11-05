using System;
using System.IO;
using System.Threading.Tasks;
using ERP.Application.Common.Interfaces;
using ERP_HRM_CMS.Filters;
using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Models.ViewModels;
using ERP_HRM_CMS.Services.HRM;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ERP_HRM_CMS.Controllers
{
    /// <summary>
    /// Cổng Tự Phục Vụ Nhân Viên (Employee Self-Service — ESS Portal)
    /// Đảm bảo nguyên tắc Zero IDOR: toàn bộ thao tác chỉ đọc và ghi dữ liệu của chính nhân viên đăng nhập.
    /// </summary>
    [Authorize]
    [KiemTraQuyen("hrm.self.service")]
    [Route("CongNhanVien")]
    [Route("SelfService")]
    public class SelfServiceController : Controller
    {
        public const string PayslipUnlockKey = "ESS_PAYSLIP_UNLOCK_EXPIRY";

        private readonly ISelfServicePortalService _portalService;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<SelfServiceController> _logger;

        public SelfServiceController(
            ISelfServicePortalService portalService,
            ICurrentUserService currentUser,
            ILogger<SelfServiceController> logger)
        {
            _portalService = portalService;
            _currentUser = currentUser;
            _logger = logger;
        }

        private int CurrentUserId => _currentUser.UserId ?? 0;
        private int? CurrentEmployeeId => _currentUser.EmployeeId;

        private bool IsPayslipUnlocked()
        {
            var expiryStr = HttpContext.Session.GetString(PayslipUnlockKey);
            if (string.IsNullOrEmpty(expiryStr)) return false;

            if (DateTime.TryParse(expiryStr, out var expiry) && expiry > VietnamTime.Now)
            {
                return true;
            }

            HttpContext.Session.Remove(PayslipUnlockKey);
            return false;
        }

        // ═══════════════════════════════════════════════════════════
        // 1. TRANG CHỦ BENTO DASHBOARD
        // ═══════════════════════════════════════════════════════════

        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index(int? year, int? month, string? tab)
        {
            if (!CurrentEmployeeId.HasValue || CurrentEmployeeId.Value <= 0)
            {
                ViewBag.ErrorMessage = "Tài khoản của bạn chưa được liên kết với hồ sơ nhân viên trong hệ thống. Vui lòng liên hệ Phòng Nhân sự (HR) để được hỗ trợ.";
                return View("~/Views/CongNhanVien/NoEmployeeProfile.cshtml");
            }

            int targetYear = year ?? VietnamTime.Today.Year;
            int targetMonth = month ?? VietnamTime.Today.Month;

            var vm = await _portalService.GetDashboardAsync(
                CurrentEmployeeId.Value, targetYear, targetMonth, IsPayslipUnlocked());

            if (!string.IsNullOrWhiteSpace(tab))
            {
                vm.ActiveTab = tab.ToLowerInvariant();
            }

            return View("~/Views/CongNhanVien/Index.cshtml", vm);
        }

        // ═══════════════════════════════════════════════════════════
        // 2. CHẤM CÔNG TRỰC TUYẾN (WEB CHECK-IN / CHECK-OUT)
        // ═══════════════════════════════════════════════════════════

        [HttpPost("CheckIn")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckIn()
        {
            if (!CurrentEmployeeId.HasValue)
            {
                return Json(new { success = false, message = "Không xác định được danh tính nhân viên của bạn." });
            }

            string clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            string userAgent = Request.Headers["User-Agent"].ToString();

            var (success, error, status) = await _portalService.WebCheckInAsync(
                CurrentEmployeeId.Value, clientIp, userAgent, CurrentUserId);

            if (!success)
            {
                return Json(new { success = false, message = error, status });
            }

            return Json(new
            {
                success = true,
                message = $"Check-in thành công lúc {status?.CheckInTime:hh\\:mm} ({status?.StatusText})!",
                status
            });
        }

        [HttpPost("CheckOut")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckOut()
        {
            if (!CurrentEmployeeId.HasValue)
            {
                return Json(new { success = false, message = "Không xác định được danh tính nhân viên của bạn." });
            }

            string clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            string userAgent = Request.Headers["User-Agent"].ToString();

            var (success, error, status) = await _portalService.WebCheckOutAsync(
                CurrentEmployeeId.Value, clientIp, userAgent, CurrentUserId);

            if (!success)
            {
                return Json(new { success = false, message = error, status });
            }

            return Json(new
            {
                success = true,
                message = $"Check-out thành công lúc {status?.CheckOutTime:hh\\:mm} (Tổng {status?.WorkHours:N1} giờ công)!",
                status
            });
        }

        // ═══════════════════════════════════════════════════════════
        // 3. TẠO & HỦY ĐƠN TỪ CÁ NHÂN
        // ═══════════════════════════════════════════════════════════

        [HttpPost("TaoDon")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TaoDon(PersonalLeaveRequestCreateDto model)
        {
            if (!CurrentEmployeeId.HasValue)
            {
                return Json(new { success = false, message = "Không xác định được danh tính nhân viên." });
            }

            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Dữ liệu nhập không hợp lệ. Vui lòng kiểm tra lại." });
            }

            var (success, error, requestId) = await _portalService.CreateLeaveRequestAsync(
                model, CurrentEmployeeId.Value, CurrentUserId);

            if (!success)
            {
                return Json(new { success = false, message = error });
            }

            return Json(new
            {
                success = true,
                message = "Đã gửi đơn từ thành công! Đơn đang chờ cấp quản lý phê duyệt.",
                requestId
            });
        }

        [HttpPost("HuyDon")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HuyDon(int id)
        {
            if (!CurrentEmployeeId.HasValue)
            {
                return Json(new { success = false, message = "Không xác định được danh tính nhân viên." });
            }

            var (success, error) = await _portalService.CancelLeaveRequestAsync(
                id, CurrentEmployeeId.Value, CurrentUserId);

            if (!success)
            {
                return Json(new { success = false, message = error });
            }

            return Json(new { success = true, message = "Đã rút lại (hủy) đơn từ thành công!" });
        }

        [HttpPost("GuiGiaiTrinh")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuiGiaiTrinh(PersonalAdjustmentCreateDto model)
        {
            if (!CurrentEmployeeId.HasValue)
            {
                return Json(new { success = false, message = "Không xác định được danh tính nhân viên." });
            }

            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Dữ liệu giải trình không hợp lệ." });
            }

            var (success, error, requestId) = await _portalService.CreateAdjustmentRequestAsync(
                model, CurrentEmployeeId.Value, CurrentUserId);

            if (!success)
            {
                return Json(new { success = false, message = error });
            }

            return Json(new
            {
                success = true,
                message = "Đã gửi giải trình chấm công thành công! Quản lý sẽ sớm xem xét điều chỉnh.",
                requestId
            });
        }

        // ═══════════════════════════════════════════════════════════
        // 4. TRA CỨU PHIẾU LƯƠNG BẢO MẬT (XÁC THỰC MẬT KHẨU 5 PHÚT)
        // ═══════════════════════════════════════════════════════════

        [HttpPost("XacThucMatKhauPhieuLuong")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XacThucMatKhauPhieuLuong([FromBody] VerifyPasswordDto model)
        {
            if (string.IsNullOrWhiteSpace(model?.Password))
            {
                return Json(new { success = false, message = "Vui lòng nhập mật khẩu tài khoản của bạn." });
            }

            bool isValid = await _portalService.VerifyPasswordAsync(CurrentUserId, model.Password);
            if (!isValid)
            {
                return Json(new { success = false, message = "Mật khẩu xác thực không chính xác. Vui lòng thử lại." });
            }

            // Mở khóa phiên 5 phút trong Session
            var expiry = VietnamTime.Now.AddMinutes(5);
            HttpContext.Session.SetString(PayslipUnlockKey, expiry.ToString("o"));

            return Json(new
            {
                success = true,
                expiryMinutes = 5,
                message = "Xác thực thành công. Phiên tra cứu phiếu lương có hiệu lực trong 5 phút."
            });
        }

        [HttpGet("ChiTietPhieuLuong/{id}")]
        public async Task<IActionResult> ChiTietPhieuLuong(int id)
        {
            if (!CurrentEmployeeId.HasValue)
            {
                return Forbid();
            }

            if (!IsPayslipUnlocked())
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    requiresUnlock = true,
                    message = "Yêu cầu xác thực lại mật khẩu đăng nhập để mở khóa xem thông tin thu nhập nhạy cảm."
                });
            }

            var detail = await _portalService.GetSalarySlipDetailAsync(id, CurrentEmployeeId.Value);
            if (detail == null)
            {
                return NotFound(new { message = "Không tìm thấy phiếu lương này hoặc bạn không có quyền truy cập." });
            }

            return Json(new { success = true, data = detail });
        }

        [HttpGet("InPhieuLuong/{id}")]
        public async Task<IActionResult> InPhieuLuong(int id)
        {
            if (!CurrentEmployeeId.HasValue)
            {
                return Forbid();
            }

            if (!IsPayslipUnlocked())
            {
                TempData["ErrorMessage"] = "Vui lòng xác thực lại mật khẩu đăng nhập để mở bản in phiếu lương.";
                return RedirectToAction(nameof(Index), new { tab = "payslips" });
            }

            var detail = await _portalService.GetSalarySlipDetailAsync(id, CurrentEmployeeId.Value);
            if (detail == null)
            {
                return NotFound("Không tìm thấy phiếu lương của bạn.");
            }

            return View("~/Views/CongNhanVien/InPhieuLuong.cshtml", detail);
        }

        // ═══════════════════════════════════════════════════════════
        // 5. CẬP NHẬT THÔNG TIN LIÊN HỆ & ĐĂNG KÝ NGƯỜI PHỤ THUỘC
        // ═══════════════════════════════════════════════════════════

        [HttpPost("CapNhatLienHe")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CapNhatLienHe(PersonalContactUpdateDto model)
        {
            if (!CurrentEmployeeId.HasValue)
            {
                return Json(new { success = false, message = "Không xác định được danh tính nhân viên." });
            }

            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Thông tin liên hệ không hợp lệ." });
            }

            var (success, error) = await _portalService.UpdateContactInfoAsync(
                model, CurrentEmployeeId.Value, CurrentUserId);

            if (!success)
            {
                return Json(new { success = false, message = error });
            }

            return Json(new { success = true, message = "Đã cập nhật thông tin liên hệ thành công!" });
        }

        [HttpPost("DangKyNguoiPhuThuoc")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DangKyNguoiPhuThuoc(PersonalDependentRegisterDto model)
        {
            if (!CurrentEmployeeId.HasValue)
            {
                return Json(new { success = false, message = "Không xác định được danh tính nhân viên." });
            }

            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Thông tin người phụ thuộc không hợp lệ." });
            }

            var (success, error) = await _portalService.RegisterDependentAsync(
                model, CurrentEmployeeId.Value, CurrentUserId);

            if (!success)
            {
                return Json(new { success = false, message = error });
            }

            return Json(new { success = true, message = "Đã gửi thông tin đăng ký người phụ thuộc thành công!" });
        }
    }
}

