using ERP_HRM_CMS.Models.Enums;
using System.Globalization;
using System.Text;
using ERP_HRM_CMS.Models.Entities;

namespace ERP_HRM_CMS.Services.Printing
{
    /// <summary>
    /// Bảng lương toàn bộ nhân viên trong một kỳ.
    /// [OOP - KẾ THỪA] Kế thừa PrintableDocument.
    /// [OOP - ĐA HÌNH / OVERRIDE] Ghi đè RenderHeader() để thêm thông tin kỳ, và NoiDungChinh() để vẽ bảng.
    /// [OOP - ĐÓNG GÓI] Danh sách phiếu được sao chép vào field private; tổng tiền chỉ tính, không gán từ ngoài.
    /// </summary>
    public sealed class BangLuongPrintable : PrintableDocument
    {
        private readonly PayrollPeriod _ky;
        private readonly List<SalarySlip> _cacPhieu;

        public BangLuongPrintable(PayrollPeriod ky, IEnumerable<SalarySlip> cacPhieu)
            : base(TaoTieuDe(ky))
        {
            _ky = ky;
            ArgumentNullException.ThrowIfNull(cacPhieu);
            _cacPhieu = cacPhieu.ToList();
        }

        private static string TaoTieuDe(PayrollPeriod ky)
        {
            ArgumentNullException.ThrowIfNull(ky);
            return ky.Name;
        }

        private decimal TongGross => _cacPhieu.Sum(p => p.GrossSalary);

        private decimal TongThucLinh => _cacPhieu.Sum(p => p.NetSalary);

        protected override string RenderHeader()
        {
            return base.RenderHeader() +
                   "<p class=\"ngay\">Kỳ: tháng " + _ky.Month + "/" + _ky.Year +
                   " - Ngày công chuẩn: " + _ky.StandardWorkDays +
                   " - Trạng thái: " + Enc(WorkflowStatusCodes.ToCode(WorkflowStatusCodes.PayrollPeriod, _ky.Status)) + "</p>";
        }

        protected override string NoiDungChinh()
        {
            var sb = new StringBuilder();
            sb.Append("<table><tr><th>STT</th><th>Mã NV</th><th>Họ tên</th><th>Phòng ban</th>")
              .Append("<th class=\"so\">Gross</th><th class=\"so\">Bảo hiểm</th><th class=\"so\">Thực lĩnh</th></tr>");

            int stt = 1;
            foreach (var phieu in _cacPhieu)
            {
                sb.Append("<tr><td>").Append(stt.ToString(CultureInfo.InvariantCulture))
                  .Append("</td><td>").Append(Enc(phieu.Employee?.EmployeeCode))
                  .Append("</td><td>").Append(Enc(phieu.Employee?.FullName))
                  .Append("</td><td>").Append(Enc(phieu.Employee?.Department?.Name))
                  .Append("</td><td class=\"so\">").Append(Tien(phieu.GrossSalary))
                  .Append("</td><td class=\"so\">").Append(Tien(phieu.TotalInsurance))
                  .Append("</td><td class=\"so\">").Append(Tien(phieu.NetSalary))
                  .Append("</td></tr>");
                stt++;
            }

            sb.Append("<tr class=\"dam\"><td colspan=\"4\">Tổng cộng (")
              .Append(_cacPhieu.Count.ToString(CultureInfo.InvariantCulture))
              .Append(" nhân viên)</td><td class=\"so\">").Append(Tien(TongGross))
              .Append("</td><td></td><td class=\"so\">").Append(Tien(TongThucLinh))
              .Append("</td></tr></table>");
            return sb.ToString();
        }
    }
}
