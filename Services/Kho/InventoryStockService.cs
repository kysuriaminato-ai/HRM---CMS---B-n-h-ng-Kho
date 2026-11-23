using ERP_HRM_CMS.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP_HRM_CMS.Services.Kho
{
    /// <summary>
    /// Thay đổi số lượng tồn kho an toàn khi nhiều giao dịch chạy đồng thời.
    /// Mỗi thay đổi là MỘT câu UPDATE có điều kiện trên database (không đọc rồi ghi trong bộ nhớ),
    /// nên hai giao dịch cùng lúc không thể cùng trừ trên một số tồn cũ.
    /// Gọi trong một DbTransaction; số dòng bị ảnh hưởng quyết định thành công hay thất bại.
    /// Giai đoạn D1: Mọi thao tác làm việc theo VariantId (có hỗ trợ ProductId fallback nếu variantId = null).
    /// </summary>
    public static class InventoryStockService
    {
        public static async Task<int> ResolveVariantIdAsync(AppDbContext context, int productId, int? variantId)
        {
            if (variantId.HasValue && variantId.Value > 0) return variantId.Value;

            var vId = await context.ProductVariants
                .Where(v => v.ProductId == productId && v.IsDefault)
                .Select(v => v.Id)
                .FirstOrDefaultAsync();

            if (vId == 0)
            {
                vId = await context.ProductVariants
                    .Where(v => v.ProductId == productId)
                    .Select(v => v.Id)
                    .FirstOrDefaultAsync();
            }

            return vId > 0 ? vId : throw new InvalidOperationException($"Không tìm thấy biến thể cho sản phẩm Id={productId}");
        }

        /// <summary>Trừ tồn theo VariantId nếu đủ hàng. Trả về false khi không đủ (không thay đổi gì, không cho tồn âm).</summary>
        public static async Task<bool> TruTonAsync(AppDbContext context, int branchId, int variantId, int soLuong, DateTime thoiDiem)
        {
            if (soLuong <= 0) throw new ArgumentOutOfRangeException(nameof(soLuong), "Số lượng phải lớn hơn 0.");

            int rows = await context.Inventories
                .Where(i => i.BranchId == branchId && i.VariantId == variantId && i.StockQuantity >= soLuong)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(i => i.StockQuantity, i => i.StockQuantity - soLuong)
                    .SetProperty(i => i.UpdatedAt, thoiDiem));

            return rows == 1;
        }

        /// <summary>Trừ tồn tương thích (nhận productId hoặc variantId). Nếu variantId trống sẽ tự phân giải sang variant mặc định.</summary>
        public static async Task<bool> TruTonProductAsync(AppDbContext context, int branchId, int productId, int soLuong, DateTime thoiDiem, int? variantId = null)
        {
            int targetVariantId = variantId.HasValue && variantId.Value > 0
                ? variantId.Value
                : await ResolveVariantIdAsync(context, productId, variantId);

            return await TruTonAsync(context, branchId, targetVariantId, soLuong, thoiDiem);
        }

        /// <summary>Cộng tồn cho dòng tồn theo VariantId. Trả về false nếu chưa có dòng tồn (người gọi tự thêm mới).</summary>
        public static async Task<bool> CongTonAsync(AppDbContext context, int branchId, int variantId, int soLuong, DateTime thoiDiem)
        {
            if (soLuong <= 0) throw new ArgumentOutOfRangeException(nameof(soLuong), "Số lượng phải lớn hơn 0.");

            int rows = await context.Inventories
                .Where(i => i.BranchId == branchId && i.VariantId == variantId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(i => i.StockQuantity, i => i.StockQuantity + soLuong)
                    .SetProperty(i => i.UpdatedAt, thoiDiem));

            return rows == 1;
        }

        /// <summary>Cộng tồn tương thích theo productId.</summary>
        public static async Task<bool> CongTonProductAsync(AppDbContext context, int branchId, int productId, int soLuong, DateTime thoiDiem, int? variantId = null)
        {
            int targetVariantId = variantId.HasValue && variantId.Value > 0
                ? variantId.Value
                : await ResolveVariantIdAsync(context, productId, variantId);

            return await CongTonAsync(context, branchId, targetVariantId, soLuong, thoiDiem);
        }

        /// <summary>
        /// Đặt tồn về số thực tế (kiểm kê) chỉ khi tồn hiện tại vẫn bằng số đã đọc (optimistic).
        /// Trả về false nếu tồn đã thay đổi trong lúc kiểm kê.
        /// </summary>
        public static async Task<bool> DatTonTuyetDoiAsync(AppDbContext context, int branchId, int variantId, int tonDaDoc, int tonThucTe, DateTime thoiDiem)
        {
            int rows = await context.Inventories
                .Where(i => i.BranchId == branchId && i.VariantId == variantId && i.StockQuantity == tonDaDoc)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(i => i.StockQuantity, tonThucTe)
                    .SetProperty(i => i.UpdatedAt, thoiDiem));

            return rows == 1;
        }

        public static async Task<bool> DatTonTuyetDoiProductAsync(AppDbContext context, int branchId, int productId, int tonDaDoc, int tonThucTe, DateTime thoiDiem, int? variantId = null)
        {
            int targetVariantId = variantId.HasValue && variantId.Value > 0
                ? variantId.Value
                : await ResolveVariantIdAsync(context, productId, variantId);

            return await DatTonTuyetDoiAsync(context, branchId, targetVariantId, tonDaDoc, tonThucTe, thoiDiem);
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
