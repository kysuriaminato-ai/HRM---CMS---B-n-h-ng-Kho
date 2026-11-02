using System.Globalization;
using System.Text;
using ERP_HRM_CMS.Models.Entities;

namespace ERP_HRM_CMS.Services.Printing
{
    /// <summary>
    /// Phiếu lương một nhân viên.
    /// [OOP - KẾ THỪA] Kế thừa PrintableDocument, không viết lại đầu trang và cách mã hóa.
    /// [OOP - ĐA HÌNH] Ghi đè NoiDungChinh() (bắt buộc) và RenderFooter() (thêm chữ ký, vẫn gọi base).
    /// </summary>
    public sealed class PhieuLuongPrintable : PrintableDocument
    {
        private readonly PayrollPeriod _ky;
        private readonly SalarySlip _phieu;
        private readonly Employee _nhanVien;

        public PhieuLuongPrintable(PayrollPeriod ky, SalarySlip phieu, Employee nhanVien)
            : base(TaoTieuDe(ky, nhanVien))
        {
            _ky = ky;
            _phieu = phieu ?? throw new ArgumentNullException(nameof(phieu));
            _nhanVien = nhanVien;
        }

        private static string TaoTieuDe(PayrollPeriod ky, Employee nhanVien)
        {
            ArgumentNullException.ThrowIfNull(ky);
            ArgumentNullException.ThrowIfNull(nhanVien);
            return "Phiếu lương " + nhanVien.EmployeeCode + " - Tháng " + ky.Month + "/" + ky.Year;
        }

        protected override string NoiDungChinh()
        {
            var sb = new StringBuilder();
            sb.Append("<table>");
            sb.Append(Dong("Họ và tên", Enc(_nhanVien.FullName)));
            sb.Append(Dong("Mã nhân viên", Enc(_nhanVien.EmployeeCode)));
            sb.Append(Dong("Phòng ban", Enc(_nhanVien.Department?.Name)));
            sb.Append(Dong("Chức vụ", Enc(_nhanVien.Position?.Name)));
            sb.Append(Dong("Kỳ lương", Enc(_ky.Name)));
            sb.Append(Dong("Ngày công chuẩn / thực tế", Enc(_ky.StandardWorkDays + " / " + _phieu.ActualWorkDays.ToString("0.##", CultureInfo.InvariantCulture))));
            sb.Append(Dong("Lương cơ bản", Tien(_phieu.BasicSalary)));
            sb.Append(Dong("Lương theo ngày công", Tien(_phieu.StandardWorkSalary)));
            sb.Append(Dong("Tiền làm thêm (OT)", Tien(_phieu.OvertimeSalary)));
            sb.Append(Dong("Phụ cấp chức vụ", Tien(_phieu.PositionAllowance)));
            sb.Append(Dong("Phụ cấp ăn trưa", Tien(_phieu.LunchAllowance)));
            sb.Append(Dong("Phụ cấp khác", Tien(_phieu.OtherAllowance)));
            sb.Append(Dong("Thưởng", Tien(_phieu.Bonus)));
            sb.Append(Dong("Tổng thu nhập (Gross)", Tien(_phieu.GrossSalary), dam: true));
            sb.Append(Dong("Phạt đi muộn", Tien(_phieu.LatePenalty)));
            sb.Append(Dong("BHXH (8%)", Tien(_phieu.SocialInsurance)));
            sb.Append(Dong("BHYT (1.5%)", Tien(_phieu.HealthInsurance)));
            sb.Append(Dong("BHTN (1%)", Tien(_phieu.UnemploymentInsurance)));
            sb.Append(Dong("Thu nhập tính thuế TNCN", Tien(_phieu.TaxableIncome)));
            sb.Append(Dong("Thực lĩnh", Tien(_phieu.NetSalary), dam: true));
            sb.Append("</table>");
            return sb.ToString();
        }

        protected override string RenderFooter()
        {
            // [OOP - OVERRIDE] Thêm khu vực chữ ký, rồi gọi base để giữ dòng ghi chú chung
            return "<div class=\"ky\"><div><b>Người lập phiếu</b></div><div><b>Nhân viên ký nhận</b></div></div>"
                   + base.RenderFooter();
        }
    }
}
