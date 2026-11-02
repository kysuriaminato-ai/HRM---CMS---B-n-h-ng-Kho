using System.Globalization;
using System.Text;
using ERP_HRM_CMS.Models.Entities;

namespace ERP_HRM_CMS.Services.Printing
{
    /// <summary>
    /// Hóa đơn bán hàng POS.
    /// [OOP - KẾ THỪA] Kế thừa PrintableDocument.
    /// [OOP - ĐÓNG GÓI] Tổng tiền hàng được tính từ chi tiết (TongTienHang), không tin số liệu rời.
    /// </summary>
    public sealed class HoaDonPrintable : PrintableDocument
    {
        private readonly Order _don;

        public HoaDonPrintable(Order don)
            : base(TaoTieuDe(don))
        {
            _don = don;
        }

        private static string TaoTieuDe(Order don)
        {
            ArgumentNullException.ThrowIfNull(don);
            return "Hóa đơn bán hàng " + don.OrderCode;
        }

        private decimal TongTienHang => _don.OrderDetails.Sum(d => d.Quantity * d.UnitPrice);

        protected override string NoiDungChinh()
        {
            var sb = new StringBuilder();
            sb.Append("<table>");
            sb.Append(Dong("Chi nhánh", Enc(_don.Branch?.Name)));
            sb.Append(Dong("Khách hàng", Enc(_don.CustomerName)));
            sb.Append(Dong("Điện thoại", Enc(_don.CustomerPhone)));
            sb.Append(Dong("Địa chỉ", Enc(_don.CustomerAddress)));
            sb.Append(Dong("Thanh toán", Enc(_don.PaymentMethod + " / " + _don.PaymentStatus)));
            sb.Append("</table>");

            sb.Append("<table><tr><th>STT</th><th>Sản phẩm</th><th class=\"so\">SL</th>")
              .Append("<th class=\"so\">Đơn giá</th><th class=\"so\">Thành tiền</th></tr>");
            int stt = 1;
            foreach (var d in _don.OrderDetails)
            {
                decimal thanhTien = d.Quantity * d.UnitPrice;
                sb.Append("<tr><td>").Append(stt.ToString(CultureInfo.InvariantCulture))
                  .Append("</td><td>").Append(Enc(d.Product?.Name))
                  .Append("</td><td class=\"so\">").Append(d.Quantity.ToString(CultureInfo.InvariantCulture))
                  .Append("</td><td class=\"so\">").Append(Tien(d.UnitPrice))
                  .Append("</td><td class=\"so\">").Append(Tien(thanhTien))
                  .Append("</td></tr>");
                stt++;
            }
            sb.Append("</table>");

            sb.Append("<table>");
            sb.Append(Dong("Tổng tiền hàng", Tien(TongTienHang)));
            sb.Append(Dong("Giảm giá", Tien(_don.DiscountAmount)));
            sb.Append(Dong("Thanh toán", Tien(_don.FinalAmount), dam: true));
            sb.Append("</table>");
            return sb.ToString();
        }
    }
}
