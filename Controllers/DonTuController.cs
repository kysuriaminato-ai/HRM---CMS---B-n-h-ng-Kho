using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Filters;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Services;

namespace ERP_HRM_CMS.Controllers
{
    [Authorize]
    public class DonTuController : Controller
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;

        public DonTuController(AppDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        // 1. DANH SÁCH ĐƠN TỪ & BỘ LỌC ĐA CHIỀU
        [HttpGet]
        [KiemTraQuyen("hrm.leave.view")]
        public async Task<IActionResult> Index(string? status, string? type, int? departmentId, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.LeaveRequests
                .Include(l => l.Employee)
                    .ThenInclude(e => e.Department)
                .Include(l => l.DepartmentHead)
                .Include(l => l.Director)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(l => l.Status == status);

            if (!string.IsNullOrWhiteSpace(type))
                query = query.Where(l => l.RequestType == type);

            if (departmentId.HasValue && departmentId.Value > 0)
                query = query.Where(l => l.Employee.DepartmentId == departmentId.Value);

            if (fromDate.HasValue)
                query = query.Where(l => l.FromDate >= fromDate.Value.Date);

            if (toDate.HasValue)
                query = query.Where(l => l.ToDate <= toDate.Value.Date.AddDays(1).AddTicks(-1));

            var requests = await query.OrderByDescending(l => l.CreatedAt).ToListAsync();

            // Thống kê nhanh KPI
            ViewBag.TotalCount = await _context.LeaveRequests.CountAsync();
            ViewBag.PendingCount = await _context.LeaveRequests.CountAsync(l => l.Status == "CHO_DUYET");
            ViewBag.DeptApprovedCount = await _context.LeaveRequests.CountAsync(l => l.Status == "TRUONG_PHONG_DUYET");
            ViewBag.ApprovedCount = await _context.LeaveRequests.CountAsync(l => l.Status == "GIAM_DOC_DUYET");
            ViewBag.RejectedCount = await _context.LeaveRequests.CountAsync(l => l.Status == "TU_CHOI");

            ViewBag.Departments = new SelectList(await _context.Departments.OrderBy(d => d.Name).ToListAsync(), "Id", "Name", departmentId);
            ViewBag.Status = status;
            ViewBag.Type = type;
            ViewBag.DepartmentId = departmentId;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            return View(requests);
        }

        // 2. TẠO ĐƠN TỪ MỚI
        [HttpGet]
        [KiemTraQuyen("hrm.leave.manage")]
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();
            var model = new LeaveRequest
            {
                FromDate = DateTime.Today,
                ToDate = DateTime.Today,
                TotalDays = 1.0m,
                RequestType = "ANNUAL_LEAVE"
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.leave.manage")]
        public async Task<IActionResult> Create(LeaveRequest model)
        {
            if (model.ToDate < model.FromDate)
            {
                ModelState.AddModelError("ToDate", "Ngày kết thúc không được nhỏ hơn ngày bắt đầu.");
            }

            if (ModelState.IsValid)
            {
                // Tự sinh mã đơn
                string prefix = $"LR-{DateTime.Now:yyMM}";
                int countThisMonth = await _context.LeaveRequests.CountAsync(l => l.RequestCode.StartsWith(prefix));
                model.RequestCode = $"{prefix}-{(countThisMonth + 1):D3}";

                if (model.TotalDays <= 0)
                {
                    var span = model.ToDate.Date - model.FromDate.Date;
                    model.TotalDays = Math.Max(1, (decimal)span.TotalDays + 1);
                }

                model.Status = "CHO_DUYET";
                model.CreatedAt = DateTime.Now;

                _context.LeaveRequests.Add(model);
                await _context.SaveChangesAsync();

                var emp = await _context.Employees.Include(e => e.Department).FirstOrDefaultAsync(e => e.Id == model.EmployeeId);
                string empName = emp?.FullName ?? "Nhân viên";

                // Gửi thông báo Real-time cho Trưởng phòng / HR Manager
                await _notificationService.SendToRoleAsync("HR_MANAGER",
                    $"Đơn từ mới cần duyệt: {model.RequestCode}",
                    $"{empName} ({emp?.Department?.Name}) đã nộp đơn {GetRequestTypeName(model.RequestType)}. Thời gian: {model.FromDate:dd/MM/yyyy} - {model.ToDate:dd/MM/yyyy}.",
                    $"/DonTu/Index?status=CHO_DUYET",
                    "LEAVE_REQUEST");

                TempData["SuccessMessage"] = $"Đã nộp đơn {model.RequestCode} thành công và gửi thông báo đến người phê duyệt!";
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdowns(model.EmployeeId);
            return View(model);
        }

        // 3. DUYỆT ĐƠN (CẤP 1: TRƯỞNG PHÒNG / CẤP 2: GIÁM ĐỐC)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.leave.approve")]
        public async Task<IActionResult> Duyet(int id, string level, string? note)
        {
            var req = await _context.LeaveRequests
                .Include(l => l.Employee)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (req == null) return NotFound();

            int currentUserId = GetCurrentUserId();

            if (level == "TP") // Trưởng phòng duyệt
            {
                req.Status = "TRUONG_PHONG_DUYET";
                req.DepartmentHeadId = currentUserId;
                req.DepartmentHeadApprovedAt = DateTime.Now;
                req.DepartmentHeadNote = note;

                // Gửi thông báo lên Giám Đốc / SuperAdmin
                await _notificationService.SendToRoleAsync("SUPERADMIN",
                    $"Đơn từ {req.RequestCode} đã qua duyệt Trưởng bộ phận",
                    $"Trưởng phòng đã xác nhận đơn của {req.Employee.FullName}. Chờ Ban Giám Đốc phê duyệt cuối cùng.",
                    $"/DonTu/Index?status=TRUONG_PHONG_DUYET",
                    "LEAVE_REQUEST");

                TempData["SuccessMessage"] = $"Đã duyệt đơn {req.RequestCode} ở cấp Trưởng bộ phận!";
            }
            else // Giám đốc duyệt (Hoàn tất)
            {
                req.Status = "GIAM_DOC_DUYET";
                req.DirectorId = currentUserId;
                req.DirectorApprovedAt = DateTime.Now;
                req.DirectorNote = note;

                // TỰ ĐỘNG ĐỒNG BỘ VÀO BẢNG CHẤM CÔNG HÀNG NGÀY (AttendanceDaily)
                await SyncAttendanceOnApprovalAsync(req);

                // Gửi thông báo cho Nhân viên
                var linkedUser = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeId == req.EmployeeId);
                if (linkedUser != null)
                {
                    await _notificationService.SendToUserAsync(linkedUser.Id,
                        $"Đơn {req.RequestCode} đã được phê duyệt chính thức!",
                        $"Ban Giám Đốc đã duyệt đơn của bạn. Hệ thống đã tự động ghi nhận vào bảng công.",
                        $"/DonTu/Index",
                        "LEAVE_REQUEST");
                }

                TempData["SuccessMessage"] = $"Đã phê duyệt chính thức đơn {req.RequestCode} và tự động cập nhật bảng chấm công!";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // 4. TỪ CHỐI ĐƠN TỪ
        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("hrm.leave.approve")]
        public async Task<IActionResult> TuChoi(int id, string reason)
        {
            var req = await _context.LeaveRequests
                .Include(l => l.Employee)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (req == null) return NotFound();

            int currentUserId = GetCurrentUserId();
            req.Status = "TU_CHOI";
            req.DirectorId = currentUserId;
            req.DirectorApprovedAt = DateTime.Now;
            req.DirectorNote = string.IsNullOrWhiteSpace(reason) ? "Từ chối theo quy định nội bộ" : reason;

            // Thông báo cho Nhân viên
            var linkedUser = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeId == req.EmployeeId);
            if (linkedUser != null)
            {
                await _notificationService.SendToUserAsync(linkedUser.Id,
                    $"Đơn {req.RequestCode} đã bị từ chối",
                    $"Lý do: {req.DirectorNote}. Vui lòng liên hệ Trưởng bộ phận để biết thêm chi tiết.",
                    $"/DonTu/Index",
                    "LEAVE_REQUEST");
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã từ chối đơn {req.RequestCode} và phản hồi đến nhân sự!";
            return RedirectToAction(nameof(Index));
        }

        // Tự động đồng bộ vào Bảng chấm công
        private async Task SyncAttendanceOnApprovalAsync(LeaveRequest req)
        {
            var defaultShift = await _context.Shifts.FirstOrDefaultAsync(s => s.IsActive) ?? 
                               new Shift { Id = 1, StartTime = new TimeSpan(8,0,0), EndTime = new TimeSpan(17,30,0), WorkHours = 8.0m };

            for (var d = req.FromDate.Date; d <= req.ToDate.Date; d = d.AddDays(1))
            {
                if (d.DayOfWeek == DayOfWeek.Sunday) continue; // Bỏ qua chủ nhật

                var att = await _context.AttendanceDailies
                    .FirstOrDefaultAsync(a => a.EmployeeId == req.EmployeeId && a.WorkDate == d);

                if (att == null)
                {
                    att = new AttendanceDaily
                    {
                        EmployeeId = req.EmployeeId,
                        ShiftId = defaultShift.Id,
                        WorkDate = d,
                        CreatedAt = DateTime.Now
                    };
                    _context.AttendanceDailies.Add(att);
                }

                if (req.RequestType == "ANNUAL_LEAVE" || req.RequestType == "SICK_LEAVE" || req.RequestType == "UNPAID_LEAVE")
                {
                    att.AttendanceStatus = "Leave";
                    att.LateMinutes = 0;
                    att.EarlyMinutes = 0;
                    att.Note = $"[Phép] {GetRequestTypeName(req.RequestType)} ({req.RequestCode}): {req.Reason}";
                }
                else if (req.RequestType == "BUSINESS_TRIP")
                {
                    att.AttendanceStatus = "BusinessTrip";
                    att.WorkHourCount = 8.0m;
                    att.LateMinutes = 0;
                    att.Note = $"[Công tác] ({req.RequestCode}): {req.Reason}";
                }
                else if (req.RequestType == "LATE_EARLY")
                {
                    att.LateMinutes = 0;
                    att.EarlyMinutes = 0;
                    att.AttendanceStatus = "Present";
                    att.Note = $"[Đi muộn/về sớm có phép] ({req.RequestCode})";
                }
                else if (req.RequestType == "OVERTIME")
                {
                    att.OvertimeHours += Math.Max(2.0m, req.TotalDays * 4.0m);
                    att.Note = $"[Đăng ký OT đã duyệt] ({req.RequestCode})";
                }
            }
        }

        private async Task LoadDropdowns(int? selectedEmpId = null)
        {
            var employees = await _context.Employees
                .Where(e => e.Status == "Active")
                .Include(e => e.Department)
                .OrderBy(e => e.FullName)
                .Select(e => new { e.Id, DisplayName = $"{e.EmployeeCode} - {e.FullName} ({e.Department.Name})" })
                .ToListAsync();

            ViewBag.Employees = new SelectList(employees, "Id", "DisplayName", selectedEmpId);
        }

        private int GetCurrentUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(idClaim, out int uid)) return uid;
            return HttpContext.Session.GetInt32("UserId") ?? 1;
        }

        private static string GetRequestTypeName(string type) => type switch
        {
            "ANNUAL_LEAVE" => "Nghỉ phép năm",
            "SICK_LEAVE" => "Nghỉ ốm / Khám bệnh",
            "LATE_EARLY" => "Đi muộn / Về sớm",
            "BUSINESS_TRIP" => "Đi công tác ngoại tỉnh",
            "UNPAID_LEAVE" => "Nghỉ không hưởng lương",
            "OVERTIME" => "Đăng ký làm thêm giờ (OT)",
            _ => type
        };
    }
}
