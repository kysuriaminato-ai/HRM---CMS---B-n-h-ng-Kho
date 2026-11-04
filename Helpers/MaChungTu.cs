using System.Security.Cryptography;

namespace ERP_HRM_CMS.Helpers
{
    /// <summary>
    /// Sinh mã chứng từ (đơn hàng, phiếu kho, hợp đồng, SKU...).
    /// Mã gồm tiền tố, thời điểm theo giờ Việt Nam và 8 ký tự hex từ bộ sinh số ngẫu nhiên
    /// an toàn mật mã (thay cho System.Random, vốn đoán được và dễ trùng).
    /// Khi có kiểm tra trùng, dùng SinhDuyNhatAsync để thử lại tối đa SoLanThu lần.
    /// </summary>
    public static class MaChungTu
    {
        public const int SoLanThu = 5;
        private const int DoDaiNgauNhien = 8;

        public static string Sinh(string tienTo, DateTime? thoiDiem = null)
        {
            var moc = thoiDiem ?? VietnamTime.Now;
            string ngauNhien = RandomNumberGenerator.GetHexString(DoDaiNgauNhien);
            return $"{tienTo}-{moc:yyyyMMddHHmmss}-{ngauNhien}";
        }

        public static async Task<string> SinhDuyNhatAsync(string tienTo, Func<string, Task<bool>> daTonTai)
        {
            for (int lan = 0; lan < SoLanThu; lan++)
            {
                string ma = Sinh(tienTo);
                if (!await daTonTai(ma))
                {
                    return ma;
                }
            }

            throw new InvalidOperationException($"Không thể sinh mã chứng từ duy nhất với tiền tố '{tienTo}'.");
        }
    }
}
