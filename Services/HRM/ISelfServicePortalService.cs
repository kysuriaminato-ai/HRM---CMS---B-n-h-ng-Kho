using System.Threading.Tasks;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Models.ViewModels;

namespace ERP_HRM_CMS.Services.HRM
{
    /// <summary>
    /// Giao diện dịch vụ Cổng Tự Phục Vụ Nhân Viên (ESS Portal)
    /// Đảm bảo tính bảo mật dữ liệu tuyệt đối (Zero IDOR): mọi truy vấn dữ liệu cá nhân
    /// bắt buộc ràng buộc theo EmployeeId của người dùng đang đăng nhập.
    /// </summary>
    public interface ISelfServicePortalService
    {
        /// <summary>
        /// Nạp toàn bộ dữ liệu tổng quan cho Dashboard Bento của nhân viên
        /// </summary>
        Task<SelfServiceDashboardViewModel> GetDashboardAsync(int employeeId, int year, int month, bool isPayslipUnlocked);

        /// <summary>
        /// Thực hiện Web Check-in trực tuyến kèm ghi nhận IP, thiết bị và tính toán ân hạn
        /// </summary>
        Task<(bool Success, string? Error, TodayCheckInStatusDto? Status)> WebCheckInAsync(
            int employeeId, string? clientIp, string? userAgent, int? currentUserId);

        /// <summary>
        /// Thực hiện Web Check-out trực tuyến kèm tính toán số giờ làm việc
        /// </summary>
        Task<(bool Success, string? Error, TodayCheckInStatusDto? Status)> WebCheckOutAsync(
            int employeeId, string? clientIp, string? userAgent, int? currentUserId);

        /// <summary>
        /// Tạo đơn xin nghỉ phép / OT / công tác cá nhân
        /// </summary>
        Task<(bool Success, string? Error, int RequestId)> CreateLeaveRequestAsync(
            PersonalLeaveRequestCreateDto dto, int employeeId, int currentUserId);

        /// <summary>
        /// Hủy đơn từ khi còn đang ở trạng thái Chờ duyệt (PendingApproval)
        /// </summary>
        Task<(bool Success, string? Error)> CancelLeaveRequestAsync(
            int requestId, int employeeId, int currentUserId);

        /// <summary>
        /// Tạo đơn giải trình chấm công (quên quẹt thẻ vào/ra)
        /// </summary>
        Task<(bool Success, string? Error, int RequestId)> CreateAdjustmentRequestAsync(
            PersonalAdjustmentCreateDto dto, int employeeId, int currentUserId);

        /// <summary>
        /// Xác thực lại mật khẩu đăng nhập để mở khóa xem phiếu lương (an toàn 5 phút)
        /// </summary>
        Task<bool> VerifyPasswordAsync(int userId, string password);

        /// <summary>
        /// Xem chi tiết phiếu lương (bảo vệ Zero IDOR - chỉ lấy phiếu của chính nhân viên)
        /// </summary>
        Task<PersonalSalarySlipDetailDto?> GetSalarySlipDetailAsync(int slipId, int employeeId);

        /// <summary>
        /// Cập nhật thông tin liên hệ cá nhân (SĐT, địa chỉ, người liên hệ khẩn cấp)
        /// </summary>
        Task<(bool Success, string? Error)> UpdateContactInfoAsync(
            PersonalContactUpdateDto dto, int employeeId, int currentUserId);

        /// <summary>
        /// Đăng ký người phụ thuộc mới để giảm trừ gia cảnh thuế TNCN
        /// </summary>
        Task<(bool Success, string? Error)> RegisterDependentAsync(
            PersonalDependentRegisterDto dto, int employeeId, int currentUserId);

        /// <summary>
        /// Lấy entity Employee của nhân viên hiện tại
        /// </summary>
        Task<Employee?> GetEmployeeProfileAsync(int employeeId);
    }
}

