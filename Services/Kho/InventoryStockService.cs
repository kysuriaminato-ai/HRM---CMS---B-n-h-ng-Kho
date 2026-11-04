using ERP_HRM_CMS.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP_HRM_CMS.Services.Kho
{
    /// <summary>
    /// Thay đổi số lượng tồn kho an toàn khi nhiều giao dịch chạy đồng thời.
    /// Mỗi thay đổi là MỘT câu UPDATE có điều kiện trên database (không đọc rồi ghi trong bộ nhớ),
    /// nên hai giao dịch cùng lúc không thể cùng trừ trên một số tồn cũ.
    /// Gọi trong một DbTransaction; số dòng bị ảnh hưởng quyết định thành công hay thất bại.
    /// </summary>
    public static class InventoryStockService
    {
        /// <summary>Trừ tồn nếu đủ hàng. Trả về false khi không đủ (không thay đổi gì).</summary>
        public static async Task<bool> TruTonAsync(AppDbContext context, int branchId, int productId, int soLuong, DateTime thoiDiem)
        {
            if (soLuong <= 0) throw new ArgumentOutOfRangeException(nameof(soLuong), "Số lượng phải lớn hơn 0.");

            int rows = await context.Inventories
                .Where(i => i.BranchId == branchId && i.ProductId == productId && i.StockQuantity >= soLuong)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(i => i.StockQuantity, i => i.StockQuantity - soLuong)
                    .SetProperty(i => i.UpdatedAt, thoiDiem));

            return rows == 1;
        }

        /// <summary>Cộng tồn cho dòng tồn đã có. Trả về false nếu chưa có dòng tồn (người gọi tự thêm mới).</summary>
        public static async Task<bool> CongTonAsync(AppDbContext context, int branchId, int productId, int soLuong, DateTime thoiDiem)
        {
            if (soLuong <= 0) throw new ArgumentOutOfRangeException(nameof(soLuong), "Số lượng phải lớn hơn 0.");

            int rows = await context.Inventories
                .Where(i => i.BranchId == branchId && i.ProductId == productId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(i => i.StockQuantity, i => i.StockQuantity + soLuong)
                    .SetProperty(i => i.UpdatedAt, thoiDiem));

            return rows == 1;
        }

        /// <summary>
        /// Đặt tồn về số thực tế (kiểm kê) chỉ khi tồn hiện tại vẫn bằng số đã đọc (optimistic).
        /// Trả về false nếu tồn đã thay đổi trong lúc kiểm kê.
        /// </summary>
        public static async Task<bool> DatTonTuyetDoiAsync(AppDbContext context, int branchId, int productId, int tonDaDoc, int tonThucTe, DateTime thoiDiem)
        {
            int rows = await context.Inventories
                .Where(i => i.BranchId == branchId && i.ProductId == productId && i.StockQuantity == tonDaDoc)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(i => i.StockQuantity, tonThucTe)
                    .SetProperty(i => i.UpdatedAt, thoiDiem));

            return rows == 1;
        }

        /// <summary>Số liệu tổng hợp tồn kho của một chi nhánh, tính ngay trên database.</summary>
        public sealed record TongHopTonKho(int SoMatHang, int SoMatHangCanhBao, decimal GiaTriTonKho);

        public static async Task<TongHopTonKho> TongHopTonKhoAsync(AppDbContext context, int branchId)
        {
            var query = context.Inventories.AsNoTracking().Where(i => i.BranchId == branchId);

            int soMatHang = await query.CountAsync();
            int canhBao = await query.CountAsync(i => i.StockQuantity <= i.MinStockAlert);
            decimal giaTri = await query.SumAsync(i => (decimal)i.StockQuantity * i.Product.CostPrice);

            return new TongHopTonKho(soMatHang, canhBao, giaTri);
        }
    }
}
