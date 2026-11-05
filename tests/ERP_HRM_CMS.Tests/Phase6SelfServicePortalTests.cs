using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Models.Enums;
using ERP_HRM_CMS.Models.ViewModels;
using ERP_HRM_CMS.Services.HRM;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ERP_HRM_CMS.Tests
{
    /// <summary>
    /// Bộ kiểm thử tích hợp PHIÊN 6: Cổng Tự Phục Vụ Nhân Viên (ESS — Employee Self-Service Portal)
    /// Kiểm tra toàn diện Zero IDOR, Phân quyền bảo mật 403, Xác thực mật khẩu mở khóa phiếu lương 5 phút,
    /// Web Check-in/out, Hủy đơn từ cá nhân và Điều hướng đăng nhập.
    /// </summary>
    public class Phase6SelfServicePortalTests : IClassFixture<TestAppFactory>
    {
        private readonly TestAppFactory _factory;

        public Phase6SelfServicePortalTests(TestAppFactory factory)
        {
            _factory = factory;
        }

        private async Task<(Employee emp, User user)> CreateEmployeeWithUserAsync(string prefix)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var dept = await db.Departments.FirstAsync();
            var pos = await db.Positions.FirstAsync();

            var emp = new Employee
            {
                EmployeeCode = $"NV-{Guid.NewGuid():N}"[..10],
                FullName = $"Nhân Viên ESS {prefix}",
                DepartmentId = dept.Id,
                PositionId = pos.Id,
                Email = $"ess_{prefix.ToLower()}_{Guid.NewGuid():N}@test.local",
                Phone = "0901234567",
                Status = "Active",
                HireDate = DateTime.Today.AddYears(-2)
            };
            db.Employees.Add(emp);
            await db.SaveChangesAsync();

            string salt = MatKhauHelper.GenerateSalt();
            var user = new User
            {
                Username = $"u_{prefix.ToLower()}_{Guid.NewGuid():N}"[..12],
                PasswordSalt = salt,
                PasswordHash = MatKhauHelper.HashPassword(TestHelpers.Password, salt),
                IsActive = true,
                IsSuperAdmin = false,
                EmployeeId = emp.Id,
                CreatedAt = VietnamTime.Now
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            // Gán vai trò EMPLOYEE (quyền hrm.self.service)
            var empRole = await db.Roles.FirstOrDefaultAsync(r => r.Code == "EMPLOYEE");
            if (empRole != null)
            {
                db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = empRole.Id });
                await db.SaveChangesAsync();
            }

            return (emp, user);
        }

        // ═══════════════════════════════════════════════════════════
        // 1. ĐIỀU HƯỚNG ĐĂNG NHẬP (EMPLOYEE ROLE REDIRECTS TO /CongNhanVien)
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Employee_Login_Redirects_Default_To_CongNhanVien()
        {
            var (_, user) = await CreateEmployeeWithUserAsync("Login");

            using var client = TestHelpers.NewClient(_factory);
            var res = await TestHelpers.LoginAsync(client, user.Username, TestHelpers.Password);

            Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
            Assert.NotNull(res.Headers.Location);
            Assert.Contains("/CongNhanVien", res.Headers.Location.OriginalString);
        }

        // ═══════════════════════════════════════════════════════════
        // 2. PHÂN QUYỀN BẢO MẬT: CHẶN TRUY CẬP TRANG QUẢN TRỊ (403 FORBIDDEN)
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Employee_Cannot_Access_Management_Pages_Returns_Forbidden()
        {
            var (_, user) = await CreateEmployeeWithUserAsync("Perm");

            using var client = TestHelpers.NewClient(_factory);
            await TestHelpers.LoginAsync(client, user.Username, TestHelpers.Password);

            // 1. Cố truy cập trang quản trị nhân sự tổng hợp
            var hrRes = await client.GetAsync("/NhanSu/Index");
            Assert.True(hrRes.StatusCode == HttpStatusCode.Forbidden || (hrRes.StatusCode == HttpStatusCode.Redirect && (hrRes.Headers.Location?.OriginalString.Contains("KhongCoQuyen") == true || hrRes.Headers.Location?.OriginalString.Contains("AccessDenied") == true)));

            // 2. Cố truy cập quản trị bảng lương
            var payrollRes = await client.GetAsync("/BangLuong/Index");
            Assert.True(payrollRes.StatusCode == HttpStatusCode.Forbidden || (payrollRes.StatusCode == HttpStatusCode.Redirect && (payrollRes.Headers.Location?.OriginalString.Contains("KhongCoQuyen") == true || payrollRes.Headers.Location?.OriginalString.Contains("AccessDenied") == true)));

            // 3. Cố gọi duyệt đơn từ quản trị
            string token = await TestHelpers.GetAntiForgeryTokenAsync(client, "/CongNhanVien");
            var approveReq = new HttpRequestMessage(HttpMethod.Post, "/DonTu/Duyet")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["id"] = "1",
                    ["level"] = "TP",
                    ["__RequestVerificationToken"] = token
                })
            };
            approveReq.Headers.Add("X-Requested-With", "XMLHttpRequest");
            var approveRes = await client.SendAsync(approveReq);
            Assert.Equal(HttpStatusCode.Forbidden, approveRes.StatusCode);
        }

        // ═══════════════════════════════════════════════════════════
        // 3. TRUY CẬP CỔNG ESS PORTAL THÀNH CÔNG
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Employee_Can_Access_CongNhanVien_Portal_Returns_200()
        {
            var (emp, user) = await CreateEmployeeWithUserAsync("Portal");

            using var client = TestHelpers.NewClient(_factory);
            await TestHelpers.LoginAsync(client, user.Username, TestHelpers.Password);

            var res = await client.GetAsync("/CongNhanVien");
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);

            string html = await res.Content.ReadAsStringAsync();
            string decodedHtml = WebUtility.HtmlDecode(html);
            Assert.Contains(emp.FullName, decodedHtml);
            Assert.Contains(emp.EmployeeCode, html);
            Assert.Contains("Chấm Công Trực Tuyến", decodedHtml);
            Assert.Contains("liveClockFace", html);
        }

        // ═══════════════════════════════════════════════════════════
        // 4. CHẤM CÔNG TRỰC TUYẾN: WEB CHECK-IN & CHECK-OUT WORKFLOW
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task WebCheckIn_And_CheckOut_Workflow_Succeeds_And_Prevents_Duplicate()
        {
            var (emp, user) = await CreateEmployeeWithUserAsync("CheckIn");

            using var scope = _factory.Services.CreateScope();
            var portalService = scope.ServiceProvider.GetRequiredService<ISelfServicePortalService>();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // 1. Check-in lần 1 -> Thành công
            var (inOk, inErr, inStatus) = await portalService.WebCheckInAsync(emp.Id, "192.168.1.100", "Chrome Desktop", user.Id);
            Assert.True(inOk, inErr);
            Assert.NotNull(inStatus);
            Assert.True(inStatus.HasCheckedIn);
            Assert.NotNull(inStatus.CheckInTime);

            // 2. Check-in lần 2 -> Bị chặn vì đã check-in hôm nay
            var (inOk2, inErr2, _) = await portalService.WebCheckInAsync(emp.Id, "192.168.1.100", "Chrome Desktop", user.Id);
            Assert.False(inOk2);
            Assert.Contains("đã thực hiện Check-in hôm nay", inErr2 ?? "");

            // 3. Check-out -> Thành công và ghi nhận số giờ công
            var (outOk, outErr, outStatus) = await portalService.WebCheckOutAsync(emp.Id, "192.168.1.100", "Chrome Desktop", user.Id);
            Assert.True(outOk, outErr);
            Assert.NotNull(outStatus);
            Assert.True(outStatus.HasCheckedOut);
            Assert.NotNull(outStatus.CheckOutTime);

            // Kiểm tra trong cơ sở dữ liệu
            var record = await db.AttendanceDailies.FirstOrDefaultAsync(a => a.EmployeeId == emp.Id && a.WorkDate.Date == VietnamTime.Today);
            Assert.NotNull(record);
            Assert.NotNull(record.FirstInTime);
            Assert.NotNull(record.LastOutTime);
            Assert.Contains("Web Check-in", record.Note ?? "");
        }

        // ═══════════════════════════════════════════════════════════
        // 5. BẢO MẬT PHIẾU LƯƠNG: XÁC THỰC MẬT KHẨU & CHỐNG IDOR TUYỆT ĐỐI
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task SecurePayslip_Requires_Password_Unlock_And_Blocks_IDOR()
        {
            var (empA, userA) = await CreateEmployeeWithUserAsync("PayA");
            var (empB, _) = await CreateEmployeeWithUserAsync("PayB");

            // Tạo kỳ lương và 2 phiếu lương
            int slipIdA, slipIdB;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var period = await db.PayrollPeriods.FirstOrDefaultAsync(p => p.Month == 11 && p.Year == 2026);
                if (period == null)
                {
                    period = new PayrollPeriod
                    {
                        Name = $"Kỳ lương ESS Test {Guid.NewGuid():N}"[..25],
                        Month = 11,
                        Year = 2026,
                        StandardWorkDays = 22,
                        Status = PayrollPeriodStatus.Draft,
                        CreatedAt = VietnamTime.Now
                    };
                    db.PayrollPeriods.Add(period);
                    await db.SaveChangesAsync();
                }

                var slipA = new SalarySlip
                {
                    PayrollPeriodId = period.Id,
                    EmployeeId = empA.Id,
                    BasicSalary = 15000000,
                    StandardDays = 22,
                    ActualWorkDays = 22,
                    GrossSalary = 15000000,
                    SocialInsurance = 1200000,
                    HealthInsurance = 225000,
                    UnemploymentInsurance = 150000,
                    TotalInsurance = 1575000,
                    NetSalary = 13425000,
                    Status = SalarySlipStatus.Sent,
                    CreatedAt = VietnamTime.Now
                };
                var slipB = new SalarySlip
                {
                    PayrollPeriodId = period.Id,
                    EmployeeId = empB.Id,
                    BasicSalary = 30000000,
                    StandardDays = 22,
                    ActualWorkDays = 22,
                    GrossSalary = 30000000,
                    TotalInsurance = 3150000,
                    NetSalary = 26850000,
                    Status = SalarySlipStatus.Sent,
                    CreatedAt = VietnamTime.Now
                };
                db.SalarySlips.AddRange(slipA, slipB);
                await db.SaveChangesAsync();

                slipIdA = slipA.Id;
                slipIdB = slipB.Id;
            }

            using var client = TestHelpers.NewClient(_factory);
            await TestHelpers.LoginAsync(client, userA.Username, TestHelpers.Password);
            string token = await TestHelpers.GetAntiForgeryTokenAsync(client, "/CongNhanVien");

            // 1. Khi chưa mở khóa -> Gọi API xem chi tiết phiếu lương của chính mình (slipA) -> 403 Forbidden
            var lockedReq = new HttpRequestMessage(HttpMethod.Get, $"/CongNhanVien/ChiTietPhieuLuong/{slipIdA}");
            lockedReq.Headers.Add("X-Requested-With", "XMLHttpRequest");
            var lockedRes = await client.SendAsync(lockedReq);
            Assert.Equal(HttpStatusCode.Forbidden, lockedRes.StatusCode);

            // 2. Gửi mật khẩu SAI để mở khóa -> Bị từ chối
            var wrongPwdReq = new HttpRequestMessage(HttpMethod.Post, "/CongNhanVien/XacThucMatKhauPhieuLuong")
            {
                Content = JsonContent.Create(new { password = "WrongPassword#123" })
            };
            wrongPwdReq.Headers.Add("RequestVerificationToken", token);
            wrongPwdReq.Headers.Add("X-Requested-With", "XMLHttpRequest");
            var wrongPwdRes = await client.SendAsync(wrongPwdReq);
            var wrongPwdData = await wrongPwdRes.Content.ReadFromJsonAsync<JsonElement>();
            Assert.False(wrongPwdData.GetProperty("success").GetBoolean());

            // 3. Gửi mật khẩu ĐÚNG -> Thành công mở khóa 5 phút
            var rightPwdReq = new HttpRequestMessage(HttpMethod.Post, "/CongNhanVien/XacThucMatKhauPhieuLuong")
            {
                Content = JsonContent.Create(new { password = TestHelpers.Password })
            };
            rightPwdReq.Headers.Add("RequestVerificationToken", token);
            rightPwdReq.Headers.Add("X-Requested-With", "XMLHttpRequest");
            var rightPwdRes = await client.SendAsync(rightPwdReq);
            var rightPwdData = await rightPwdRes.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(rightPwdData.GetProperty("success").GetBoolean());
            Assert.Equal(5, rightPwdData.GetProperty("expiryMinutes").GetInt32());

            // 4. Sau khi mở khóa -> Xem phiếu lương của chính mình (slipA) -> Thành công 200 OK
            var unlockedReq = new HttpRequestMessage(HttpMethod.Get, $"/CongNhanVien/ChiTietPhieuLuong/{slipIdA}");
            unlockedReq.Headers.Add("X-Requested-With", "XMLHttpRequest");
            var unlockedRes = await client.SendAsync(unlockedReq);
            Assert.Equal(HttpStatusCode.OK, unlockedRes.StatusCode);
            var slipData = await unlockedRes.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(slipData.GetProperty("success").GetBoolean());
            var d = slipData.GetProperty("data");
            Assert.Equal(13425000, d.GetProperty("netSalary").GetDecimal());

            // 5. THỬ TẤN CÔNG ZERO IDOR:
            // Nhân viên A cố tình truyền ID phiếu lương của Nhân viên B (slipB) -> Trả về 404 NotFound
            var idorReq = new HttpRequestMessage(HttpMethod.Get, $"/CongNhanVien/ChiTietPhieuLuong/{slipIdB}");
            idorReq.Headers.Add("X-Requested-With", "XMLHttpRequest");
            var idorRes = await client.SendAsync(idorReq);
            Assert.Equal(HttpStatusCode.NotFound, idorRes.StatusCode);
        }

        // ═══════════════════════════════════════════════════════════
        // 6. TẠO & HỦY ĐƠN TỪ CÁ NHÂN (RÚT LẠI ĐƠN & ZERO IDOR)
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task LeaveRequest_Can_Be_Cancelled_By_Owner_And_Blocks_Other_Employee()
        {
            var (empA, userA) = await CreateEmployeeWithUserAsync("ReqA");
            var (empB, _) = await CreateEmployeeWithUserAsync("ReqB");

            using var scope = _factory.Services.CreateScope();
            var portalService = scope.ServiceProvider.GetRequiredService<ISelfServicePortalService>();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Nhân viên A tạo đơn nghỉ phép
            var dto = new PersonalLeaveRequestCreateDto
            {
                RequestType = "UNPAID_LEAVE",
                FromDate = DateTime.Today.AddDays(10),
                ToDate = DateTime.Today.AddDays(11),
                TotalDays = 2,
                Reason = "Nghỉ việc cá nhân đột xuất"
            };
            var (createOk, createErr, reqId) = await portalService.CreateLeaveRequestAsync(dto, empA.Id, userA.Id);
            Assert.True(createOk, createErr);
            Assert.True(reqId > 0);

            // Kiểm tra trạng thái đơn vừa tạo: PendingApproval
            var req = await db.LeaveRequests.FindAsync(reqId);
            Assert.NotNull(req);
            Assert.Equal(LeaveRequestStatus.PendingApproval, req.Status);

            // TẤN CÔNG ZERO IDOR: Nhân viên B cố gọi hủy đơn của Nhân viên A
            var (idorCancelOk, idorCancelErr) = await portalService.CancelLeaveRequestAsync(reqId, empB.Id, 2);
            Assert.False(idorCancelOk);
            Assert.Contains("không có quyền thao tác trên đơn từ của nhân viên khác", idorCancelErr ?? "");

            // Nhân viên A tự rút lại (hủy) đơn của mình -> Thành công
            var (cancelOk, cancelErr) = await portalService.CancelLeaveRequestAsync(reqId, empA.Id, userA.Id);
            Assert.True(cancelOk, cancelErr);

            // Kiểm tra trong DB: Đơn đã chuyển sang trạng thái Rejected với ghi chú tự rút lại
            var updatedReq = await db.LeaveRequests.FindAsync(reqId);
            Assert.NotNull(updatedReq);
            Assert.Equal(LeaveRequestStatus.Rejected, updatedReq.Status);
            Assert.Contains("tự rút lại", updatedReq.DirectorNote ?? "");
        }

        // ═══════════════════════════════════════════════════════════
        // 7. CẬP NHẬT THÔNG TIN LIÊN HỆ & ĐĂNG KÝ NGƯỜI PHỤ THUỘC
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task ContactUpdate_And_DependentRegistration_Succeeds()
        {
            var (emp, user) = await CreateEmployeeWithUserAsync("Profile");

            using var scope = _factory.Services.CreateScope();
            var portalService = scope.ServiceProvider.GetRequiredService<ISelfServicePortalService>();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // 1. Cập nhật thông tin liên hệ
            var contactDto = new PersonalContactUpdateDto
            {
                Phone = "0987654321",
                Email = "new_contact@company.com",
                CurrentAddress = "123 Đường Nguyễn Trãi, Quận 1, TP.HCM",
                PermanentAddress = "456 Đường Lê Lợi, TP. Đà Nẵng",
                EmergencyContactName = "Nguyễn Thị Vợ",
                EmergencyContactPhone = "0911223344"
            };

            var (upOk, upErr) = await portalService.UpdateContactInfoAsync(contactDto, emp.Id, user.Id);
            Assert.True(upOk, upErr);

            var reloadedEmp = await db.Employees.Include(e => e.EmployeeDetail).FirstOrDefaultAsync(e => e.Id == emp.Id);
            Assert.NotNull(reloadedEmp);
            Assert.Equal("0987654321", reloadedEmp.Phone);
            Assert.Equal("new_contact@company.com", reloadedEmp.Email);
            Assert.Equal("123 Đường Nguyễn Trãi, Quận 1, TP.HCM", reloadedEmp.EmployeeDetail?.CurrentAddress);
            Assert.Equal("Nguyễn Thị Vợ", reloadedEmp.EmployeeDetail?.EmergencyContactName);

            // 2. Đăng ký người phụ thuộc
            var depDto = new PersonalDependentRegisterDto
            {
                FullName = "Nguyễn Con A",
                Relationship = "CON",
                DateOfBirth = new DateTime(2020, 5, 1),
                FromDate = new DateTime(2026, 1, 1),
                IdCardNumber = "012345678910"
            };

            var (depOk, depErr) = await portalService.RegisterDependentAsync(depDto, emp.Id, user.Id);
            Assert.True(depOk, depErr);

            var dep = await db.EmployeeDependents.FirstOrDefaultAsync(d => d.EmployeeId == emp.Id && d.FullName == "Nguyễn Con A");
            Assert.NotNull(dep);
            Assert.Equal("CON", dep.Relationship);
        }
    }
}
