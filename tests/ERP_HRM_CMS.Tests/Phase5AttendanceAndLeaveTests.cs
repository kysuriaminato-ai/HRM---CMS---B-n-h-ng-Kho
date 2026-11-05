using ClosedXML.Excel;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Models.Enums;
using ERP_HRM_CMS.Services.HRM;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ERP_HRM_CMS.Tests
{
    /// <summary>
    /// Bộ kiểm thử PHIÊN 5: Chấm Công Thông Minh, Phân Ca (Shift Roster), Quỹ Phép Năm & Duyệt Đơn Từ Đa Cấp
    /// </summary>
    public class Phase5AttendanceAndLeaveTests : IClassFixture<TestAppFactory>
    {
        private readonly TestAppFactory _factory;

        public Phase5AttendanceAndLeaveTests(TestAppFactory factory)
        {
            _factory = factory;
        }

        // ═══════════════════════════════════════════════════════════
        // 1. TÍNH PHÉP THÂM NIÊN (ĐIỀU 114 BỘ LUẬT LAO ĐỘNG)
        // ═══════════════════════════════════════════════════════════
        [Theory]
        [InlineData(1, 0)]   // 1 năm: 0 ngày bonus
        [InlineData(4, 0)]   // 4 năm: 0 ngày bonus
        [InlineData(5, 1)]   // 5 năm: +1 ngày bonus
        [InlineData(9, 1)]   // 9 năm: +1 ngày bonus
        [InlineData(10, 2)]  // 10 năm: +2 ngày bonus
        [InlineData(14, 2)]  // 14 năm: +2 ngày bonus
        [InlineData(15, 3)]  // 15 năm: +3 ngày bonus
        [InlineData(20, 4)]  // 20 năm: +4 ngày bonus
        public void CalculateSeniorityBonus_Follows_LaborCode_Article114(int completedYears, decimal expectedBonus)
        {
            using var scope = _factory.Services.CreateScope();
            var leaveService = scope.ServiceProvider.GetRequiredService<ILeaveManagementService>();

            int targetYear = 2026;
            var hireDate = new DateTime(targetYear - completedYears, 6, 1);

            decimal bonus = leaveService.CalculateSeniorityBonus(hireDate, targetYear);
            Assert.Equal(expectedBonus, bonus);
        }

        // ═══════════════════════════════════════════════════════════
        // 2. KHỞI TẠO & TÍNH TOÁN QUỸ PHÉP NĂM (LEAVE BALANCE)
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task InitializeOrRecalculateBalance_Calculates_Standard_Seniority_And_Remaining()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var leaveService = scope.ServiceProvider.GetRequiredService<ILeaveManagementService>();

            // Tạo nhân viên làm việc 11 năm (hưởng +2 ngày phép thâm niên)
            var dept = await db.Departments.FirstAsync();
            var pos = await db.Positions.FirstAsync();
            var emp = new Employee
            {
                EmployeeCode = $"NV-SEN-{Guid.NewGuid():N}".Substring(0, 10),
                Email = $"emp_{Guid.NewGuid():N}@test.local",
                FullName = "Nhân viên thâm niên 11 năm",
                DepartmentId = dept.Id,
                PositionId = pos.Id,
                Status = "Active",
                HireDate = DateTime.Today.AddYears(-11)
            };
            db.Employees.Add(emp);
            await db.SaveChangesAsync();

            int year = DateTime.Today.Year;
            var (success, error, balanceId) = await leaveService.InitializeOrRecalculateBalanceAsync(emp.Id, year, 1);
            Assert.True(success, error);
            Assert.True(balanceId > 0);

            var balance = await leaveService.GetEmployeeLeaveBalanceAsync(emp.Id, year);
            Assert.NotNull(balance);
            Assert.Equal(12.0m, balance.StandardDays);
            Assert.Equal(2.0m, balance.SeniorityBonusDays); // 11 năm -> +2 ngày
            Assert.Equal(14.0m, balance.RemainingDays);    // 12 + 2 = 14 ngày
        }

        // ═══════════════════════════════════════════════════════════
        // 3. TẠO ĐƠN & RÀNG BUỘC SỐ DƯ PHÉP NĂM, TRÙNG LỊCH NGHỈ
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task CreateLeaveRequest_Fails_When_Days_Exceed_Remaining_Balance()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var leaveService = scope.ServiceProvider.GetRequiredService<ILeaveManagementService>();

            // Nhân viên mới vào làm (quỹ phép 12 ngày)
            var dept = await db.Departments.FirstAsync();
            var pos = await db.Positions.FirstAsync();
            var emp = new Employee
            {
                EmployeeCode = $"NV-LEAVE-{Guid.NewGuid():N}".Substring(0, 10),
                Email = $"emp_{Guid.NewGuid():N}@test.local",
                FullName = "Nhân viên xin phép vượt hạn",
                DepartmentId = dept.Id,
                PositionId = pos.Id,
                Status = "Active",
                HireDate = DateTime.Today
            };
            db.Employees.Add(emp);
            await db.SaveChangesAsync();

            int year = DateTime.Today.Year;
            await leaveService.InitializeOrRecalculateBalanceAsync(emp.Id, year, 1);

            // Xin nghỉ 20 ngày phép năm (vượt quá 12 ngày)
            var req = new LeaveRequest
            {
                EmployeeId = emp.Id,
                RequestType = "ANNUAL_LEAVE",
                FromDate = new DateTime(year, 7, 1),
                ToDate = new DateTime(year, 7, 20),
                TotalDays = 20.0m,
                Reason = "Nghỉ hè dài ngày"
            };

            var (success, error, _) = await leaveService.CreateLeaveRequestAsync(req, 1);
            Assert.False(success);
            Assert.Contains("không đủ để đăng ký", error ?? "");
        }

        [Fact]
        public async Task CreateLeaveRequest_Fails_When_Overlapping_With_Existing_Request()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var leaveService = scope.ServiceProvider.GetRequiredService<ILeaveManagementService>();

            var dept = await db.Departments.FirstAsync();
            var pos = await db.Positions.FirstAsync();
            var emp = new Employee
            {
                EmployeeCode = $"NV-OVL-{Guid.NewGuid():N}".Substring(0, 10),
                Email = $"emp_{Guid.NewGuid():N}@test.local",
                FullName = "Nhân viên trùng lịch",
                DepartmentId = dept.Id,
                PositionId = pos.Id,
                Status = "Active",
                HireDate = DateTime.Today
            };
            db.Employees.Add(emp);
            await db.SaveChangesAsync();

            int year = DateTime.Today.Year;
            await leaveService.InitializeOrRecalculateBalanceAsync(emp.Id, year, 1);

            // Đơn 1: Từ 10/08 đến 12/08
            var req1 = new LeaveRequest
            {
                EmployeeId = emp.Id,
                RequestType = "ANNUAL_LEAVE",
                FromDate = new DateTime(year, 8, 10),
                ToDate = new DateTime(year, 8, 12),
                TotalDays = 3.0m,
                Reason = "Đi du lịch đợt 1"
            };
            var (ok1, _, _) = await leaveService.CreateLeaveRequestAsync(req1, 1);
            Assert.True(ok1);

            // Đơn 2: Từ 11/08 đến 15/08 (trùng ngày 11, 12)
            var req2 = new LeaveRequest
            {
                EmployeeId = emp.Id,
                RequestType = "ANNUAL_LEAVE",
                FromDate = new DateTime(year, 8, 11),
                ToDate = new DateTime(year, 8, 15),
                TotalDays = 4.0m,
                Reason = "Đi du lịch đợt 2"
            };
            var (ok2, err2, _) = await leaveService.CreateLeaveRequestAsync(req2, 1);
            Assert.False(ok2);
            Assert.Contains("trùng lặp", err2 ?? "");
        }

        // ═══════════════════════════════════════════════════════════
        // 4. DUYỆT ĐƠN ĐA CẤP, TỰ ĐỘNG TRỪ QUỸ PHÉP & ĐỒNG BỘ BẢNG CÔNG
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task FinalApproval_Deducts_Leave_Balance_And_Updates_AttendanceDailies()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var leaveService = scope.ServiceProvider.GetRequiredService<ILeaveManagementService>();

            var dept = await db.Departments.FirstAsync();
            var pos = await db.Positions.FirstAsync();
            var emp = new Employee
            {
                EmployeeCode = $"NV-APPR-{Guid.NewGuid():N}".Substring(0, 10),
                Email = $"emp_{Guid.NewGuid():N}@test.local",
                FullName = "Nhân viên duyệt đơn",
                DepartmentId = dept.Id,
                PositionId = pos.Id,
                Status = "Active",
                HireDate = DateTime.Today
            };
            db.Employees.Add(emp);
            await db.SaveChangesAsync();

            int year = DateTime.Today.Year;
            int testMonth = 3;

            // Xóa khóa kỳ công tháng 3 nếu có từ test trước để đảm bảo môi trường sạch
            var existingLocks = await db.AttendanceLocks.Where(l => l.Year == year && l.Month == testMonth).ToListAsync();
            if (existingLocks.Any())
            {
                db.AttendanceLocks.RemoveRange(existingLocks);
                await db.SaveChangesAsync();
            }

            await leaveService.InitializeOrRecalculateBalanceAsync(emp.Id, year, 1);

            // Lấy 2 user hợp lệ trong database để người tạo và người duyệt là 2 tài khoản khác nhau (chống vi phạm kiểm soát nội bộ)
            var users = await db.Users.OrderBy(u => u.Id).Take(2).ToListAsync();
            int creatorUserId = users[0].Id;
            int approverUserId = users.Count > 1 ? users[1].Id : creatorUserId;
            if (creatorUserId == approverUserId)
            {
                var secondUser = new User
                {
                    Username = $"approver_{Guid.NewGuid():N}".Substring(0, 15),
                    PasswordSalt = "salt",
                    PasswordHash = "hash",
                    IsActive = true,
                    IsSuperAdmin = true,
                    CreatedAt = DateTime.UtcNow
                };
                db.Users.Add(secondUser);
                await db.SaveChangesAsync();
                approverUserId = secondUser.Id;
            }

            // Tạo đơn xin nghỉ phép 2 ngày trong tháng 3
            var req = new LeaveRequest
            {
                EmployeeId = emp.Id,
                RequestType = "ANNUAL_LEAVE",
                FromDate = new DateTime(year, testMonth, 10),
                ToDate = new DateTime(year, testMonth, 11),
                TotalDays = 2.0m,
                Reason = "Nghỉ giải quyết việc gia đình"
            };
            var (createOk, createErr, reqId) = await leaveService.CreateLeaveRequestAsync(req, creatorUserId);
            Assert.True(createOk, createErr);

            // 1. Duyệt cấp 1: Trưởng phòng
            var (tpOk, tpErr) = await leaveService.ApproveLeaveRequestAsync(reqId, "TP", "Đồng ý đề xuất", approverUserId);
            Assert.True(tpOk, tpErr);

            var reloadedReq = await leaveService.GetLeaveRequestByIdAsync(reqId);
            Assert.NotNull(reloadedReq);
            Assert.Equal(LeaveRequestStatus.DepartmentApproved, reloadedReq.Status);

            // 2. Duyệt cấp 2: Giám đốc / HR
            var (gdOk, gdErr) = await leaveService.ApproveLeaveRequestAsync(reqId, "GD", "Phê duyệt chính thức", approverUserId);
            Assert.True(gdOk, gdErr);

            // Kiểm tra quỹ phép năm đã bị trừ 2 ngày
            var balance = await leaveService.GetEmployeeLeaveBalanceAsync(emp.Id, year);
            Assert.NotNull(balance);
            Assert.Equal(2.0m, balance.UsedDays);
            Assert.Equal(10.0m, balance.RemainingDays); // 12 - 2 = 10 ngày

            // Kiểm tra bảng công AttendanceDailies tự động cập nhật
            var att1 = await db.AttendanceDailies
                .FirstOrDefaultAsync(a => a.EmployeeId == emp.Id && a.WorkDate.Date == new DateTime(year, testMonth, 10));
            var att2 = await db.AttendanceDailies
                .FirstOrDefaultAsync(a => a.EmployeeId == emp.Id && a.WorkDate.Date == new DateTime(year, testMonth, 11));

            Assert.NotNull(att1);
            Assert.Equal("Leave", att1.AttendanceStatus);
            Assert.NotNull(att2);
            Assert.Equal("Leave", att2.AttendanceStatus);
        }

        // ═══════════════════════════════════════════════════════════
        // 5. KHÓA KỲ CHẤM CÔNG (ATTENDANCE LOCK)
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task AttendanceLock_Blocks_Approval_And_Unlocks_With_Audit()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var attService = scope.ServiceProvider.GetRequiredService<IAttendanceService>();
            var leaveService = scope.ServiceProvider.GetRequiredService<ILeaveManagementService>();

            int lockYear = 2025;
            int lockMonth = 12;

            // 1. Khóa bảng công tháng 12/2025
            var (lockOk, lockErr, lockId) = await attService.LockPeriodAsync(lockYear, lockMonth, null, null, 1, "Chốt lương năm");
            Assert.True(lockOk, lockErr);

            bool isLocked = await attService.IsPeriodLockedAsync(lockYear, lockMonth);
            Assert.True(isLocked);

            // 2. Tạo đơn xin nghỉ lùi vào tháng 12/2025 -> Phải bị chặn
            var emp = await db.Employees.FirstAsync();
            var req = new LeaveRequest
            {
                EmployeeId = emp.Id,
                RequestType = "UNPAID_LEAVE",
                FromDate = new DateTime(lockYear, lockMonth, 15),
                ToDate = new DateTime(lockYear, lockMonth, 16),
                TotalDays = 2.0m,
                Reason = "Nghỉ việc riêng"
            };
            var (createOk, createErr, _) = await leaveService.CreateLeaveRequestAsync(req, 1);
            Assert.False(createOk);
            Assert.Contains("đã bị khóa", createErr ?? "");

            // 3. Mở khóa bảng công và kiểm tra AuditLog
            var (unlockOk, unlockErr) = await attService.UnlockPeriodAsync(lockId, 1, "Mở khóa duyệt bổ sung đơn công tác");
            Assert.True(unlockOk, unlockErr);

            bool isStillLocked = await attService.IsPeriodLockedAsync(lockYear, lockMonth);
            Assert.False(isStillLocked);

            // Kiểm tra vết kiểm toán AuditLog
            var audit = await db.AuditLogs
                .Where(a => a.Action == "UNLOCK_ATTENDANCE_PERIOD" && a.RecordId == lockId.ToString())
                .FirstOrDefaultAsync();
            Assert.NotNull(audit);
            Assert.Contains("Mở khóa duyệt bổ sung", audit.NewValues ?? "");
        }

        // ═══════════════════════════════════════════════════════════
        // 6. IMPORT CHẤM CÔNG MÁY QUẸT VÀ ĐỐI CHIẾU CA LÀM VIỆC
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task ImportAttendanceLog_Calculates_Late_Early_And_Overtime()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var attService = scope.ServiceProvider.GetRequiredService<IAttendanceService>();

            var emp = await db.Employees.FirstAsync(e => e.Status == "Active");
            var shift = await db.Shifts.FirstAsync(s => s.IsActive);

            // Tạo file Excel giả lập máy chấm công (4 cột: Mã NV, Ngày, Giờ vào, Giờ ra)
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("LogChamCong");
            ws.Cell(1, 1).Value = "MaNV";
            ws.Cell(1, 2).Value = "Ngay";
            ws.Cell(1, 3).Value = "GioVao";
            ws.Cell(1, 4).Value = "GioRa";

            // Dòng 1: Vào muộn 30 phút so với giờ bắt đầu ca
            var testDate = new DateTime(2026, 11, 10);
            var checkInTime = shift.StartTime.Add(TimeSpan.FromMinutes(30));
            var checkOutTime = shift.EndTime;

            ws.Cell(2, 1).Value = emp.EmployeeCode;
            ws.Cell(2, 2).Value = testDate.ToString("dd/MM/yyyy");
            ws.Cell(2, 3).Value = checkInTime.ToString(@"hh\:mm");
            ws.Cell(2, 4).Value = checkOutTime.ToString(@"hh\:mm");

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var result = await attService.ImportAttendanceLogAsync(stream, "test_attendance.xlsx", shift.Id, 1);
            Assert.True(result.Success);
            Assert.Equal(1, result.SuccessCount);

            // Kiểm tra bản ghi chấm công được tạo và tính toán đúng
            var att = await db.AttendanceDailies
                .FirstOrDefaultAsync(a => a.EmployeeId == emp.Id && a.WorkDate.Date == testDate.Date);
            Assert.NotNull(att);
            Assert.Equal(30, att.LateMinutes);
            Assert.Equal("Late", att.AttendanceStatus);
        }

        // ═══════════════════════════════════════════════════════════
        // 7. ĐƠN GIẢI TRÌNH CHẤM CÔNG (QUÊN QUẸT THẺ)
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task AttendanceAdjustment_Approval_Normalizes_Daily_Attendance()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var leaveService = scope.ServiceProvider.GetRequiredService<ILeaveManagementService>();

            var emp = await db.Employees.FirstAsync(e => e.Status == "Active");
            var shift = await db.Shifts.FirstAsync(s => s.IsActive);
            var workDate = new DateTime(2026, 11, 15);

            // Tạo đơn giải trình quên quẹt thẻ lúc ra
            var adj = new AttendanceAdjustmentRequest
            {
                EmployeeId = emp.Id,
                ShiftId = shift.Id,
                WorkDate = workDate,
                AdjustmentType = "MissedCheckOut",
                ActualInTime = new TimeSpan(8, 0, 0),
                ActualOutTime = new TimeSpan(17, 30, 0),
                Reason = "Quên quẹt thẻ ra do vội về đón con"
            };

            var (createOk, createErr, adjId) = await leaveService.CreateAdjustmentRequestAsync(adj, 1);
            Assert.True(createOk, createErr);

            // Phê duyệt giải trình
            var (approveOk, approveErr) = await leaveService.ApproveAdjustmentRequestAsync(adjId, 1, "Xác nhận đúng");
            Assert.True(approveOk, approveErr);

            // Kiểm tra bản ghi công đã được chuẩn hóa lại
            var att = await db.AttendanceDailies
                .FirstOrDefaultAsync(a => a.EmployeeId == emp.Id && a.WorkDate.Date == workDate.Date);
            Assert.NotNull(att);
            Assert.Equal("Present", att.AttendanceStatus);
            Assert.Equal(0, att.LateMinutes);
            Assert.Equal(0, att.EarlyMinutes);
            Assert.Equal(new TimeSpan(17, 30, 0), att.LastOutTime);
        }

        // ═══════════════════════════════════════════════════════════
        // 8. PHÂN CA & SAO CHÉP LỊCH PHÂN CA TUẦN
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task ShiftSchedule_CopyWeekSchedule_Replicates_Shifts_Accurately()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var shiftService = scope.ServiceProvider.GetRequiredService<IShiftScheduleService>();

            var emp = await db.Employees.FirstAsync(e => e.Status == "Active");
            var shift = await db.Shifts.FirstAsync(s => s.IsActive);

            var sourceMonday = new DateTime(2026, 11, 2); // Thứ 2 tuần 1
            var targetMonday = new DateTime(2026, 11, 9); // Thứ 2 tuần 2

            // Gán ca làm việc cho nhân viên vào Thứ 2 và Thứ 3 tuần 1
            await shiftService.AssignShiftAsync(emp.Id, sourceMonday, shift.Id, 1);
            await shiftService.AssignShiftAsync(emp.Id, sourceMonday.AddDays(1), shift.Id, 1);

            // Sao chép lịch sang tuần 2
            var (success, error, count) = await shiftService.CopyWeekScheduleAsync(
                sourceMonday, targetMonday, emp.DepartmentId, null, 1);
            Assert.True(success, error);
            Assert.True(count >= 2);

            // Kiểm tra tuần 2 đã có lịch tương ứng
            var targetAtt1 = await db.AttendanceDailies
                .FirstOrDefaultAsync(a => a.EmployeeId == emp.Id && a.WorkDate.Date == targetMonday.Date);
            var targetAtt2 = await db.AttendanceDailies
                .FirstOrDefaultAsync(a => a.EmployeeId == emp.Id && a.WorkDate.Date == targetMonday.AddDays(1).Date);

            Assert.NotNull(targetAtt1);
            Assert.Equal(shift.Id, targetAtt1.ShiftId);
            Assert.NotNull(targetAtt2);
            Assert.Equal(shift.Id, targetAtt2.ShiftId);
        }
    }
}

