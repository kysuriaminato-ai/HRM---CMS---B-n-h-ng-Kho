using System.Threading.Tasks;
using ERP_HRM_CMS.Models.ViewModels;

namespace ERP_HRM_CMS.Services.HRM
{
    public interface IPeopleAnalyticsService
    {
        /// <summary>
        /// Lấy báo cáo phân tích nguồn nhân lực cấp điều hành (Executive People Analytics)
        /// Sử dụng truy vấn gộp tối ưu trực tiếp xuống CSDL với AsNoTracking()
        /// </summary>
        Task<ExecutivePeopleAnalyticsDto> GetExecutiveAnalyticsAsync(PeopleAnalyticsFilterDto filter);

        /// <summary>
        /// Xuất Báo cáo Quản trị Nhân sự Tổng hợp ra file Excel đa Sheet (ClosedXML)
        /// Bao gồm: Tổng quan, Biến động nhân sự, Quỹ lương & Chi phí, Tuyển dụng & Cảnh báo
        /// </summary>
        byte[] ExportExecutiveReportExcel(ExecutivePeopleAnalyticsDto data);
    }
}

