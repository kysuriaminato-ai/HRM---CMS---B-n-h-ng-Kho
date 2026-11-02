using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Models.ViewModels;

namespace ERP_HRM_CMS.Services
{
    public class CartService : ICartService
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const string SessionCartKey = "ERP_SALES_SESSION_CART";

        public CartService(AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        private ISession Session => _httpContextAccessor.HttpContext!.Session;

        // 1. DATA PRESENTATION: ĐỌC DỮ LIỆU GIỎ HÀNG KẾT HỢP KIỂM TRA TỒN KHO THỰC TẾ
        public async Task<CartViewModel> GetCartViewModelAsync(int branchId, int? userId, string sessionId)
        {
            var rawItems = await GetRawCartItemsAsync(userId, sessionId);
            var productIds = rawItems.Select(i => i.ProductId).Distinct().ToList();

            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p);

            // Kiểm tra trực tiếp bảng Inventories theo BranchId
            var inventories = await _context.Inventories
                .Where(i => i.BranchId == branchId && productIds.Contains(i.ProductId))
                .ToDictionaryAsync(i => i.ProductId, i => i.StockQuantity);

            var result = new CartViewModel();

            foreach (var item in rawItems)
            {
                if (!products.TryGetValue(item.ProductId, out var prod)) continue;

                int availableStock = inventories.GetValueOrDefault(item.ProductId, 0);
                string? stockWarning = null;
                int currentQty = item.Quantity;

                // Real-time Stock Validation: Nếu giỏ hàng vượt quá tồn kho khả dụng
                if (currentQty > availableStock)
                {
                    stockWarning = availableStock > 0
                        ? $"Số lượng đã được tự động điều chỉnh về {availableStock} (tối đa trong kho)."
                        : "Sản phẩm này hiện đã hết hàng trong kho!";

                    // Giới hạn về mức khả dụng
                    currentQty = Math.Max(0, availableStock);
                    result.Warnings.Add($"Sản phẩm '{prod.Name}' chỉ còn {availableStock} {prod.Unit} trong kho.");
                }

                result.Items.Add(new CartItemDto
                {
                    ProductId = prod.Id,
                    SKU = prod.SKU,
                    ProductName = prod.Name,
                    ImageUrl = prod.ImageUrl,
                    Unit = prod.Unit,
                    UnitPrice = prod.SalePrice,
                    Quantity = currentQty,
                    AvailableStock = availableStock,
                    StockWarning = stockWarning
                });
            }

            result.SubTotal = result.Items.Sum(x => x.TotalPrice);
            result.TaxAmount = 0; // Cấu hình bổ sung VAT nếu cần
            result.DiscountAmount = 0;
            result.FinalTotal = result.SubTotal - result.DiscountAmount + result.TaxAmount;
            result.TotalItemsCount = result.Items.Sum(x => x.Quantity);

            return result;
        }

        // 2. THÊM SẢN PHẨM VÀO GIỎ HÀNG
        public async Task<CartOperationResultDto> AddToCartAsync(int productId, int quantity, int branchId, int? userId, string sessionId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null || !product.IsActive)
            {
                return new CartOperationResultDto { Success = false, Message = "Sản phẩm không tồn tại hoặc đã ngừng kinh doanh." };
            }

            // Kiểm tra tồn kho trước khi nhận thêm
            var inventory = await _context.Inventories
                .FirstOrDefaultAsync(i => i.ProductId == productId && i.BranchId == branchId);
            int availableStock = inventory?.StockQuantity ?? 0;

            var items = await GetRawCartItemsAsync(userId, sessionId);
            var existing = items.FirstOrDefault(i => i.ProductId == productId);
            int targetQty = (existing?.Quantity ?? 0) + quantity;

            bool hasStockAlert = false;
            string message = "Đã thêm sản phẩm vào giỏ hàng!";

            if (targetQty > availableStock)
            {
                targetQty = availableStock;
                hasStockAlert = true;
                message = availableStock > 0
                    ? $"Số lượng trong kho chỉ còn {availableStock}. Đã điều chỉnh giỏ hàng về mức tối đa!"
                    : "Sản phẩm đã hết hàng trong kho!";
            }

            if (targetQty <= 0)
            {
                return new CartOperationResultDto { Success = false, Message = "Không thể thêm sản phẩm do kho đã hết hàng." };
            }

            if (existing != null)
            {
                existing.Quantity = targetQty;
            }
            else
            {
                items.Add(new SimpleCartItem { ProductId = productId, Quantity = targetQty });
            }

            await SaveRawCartItemsAsync(items, userId, sessionId);
            var cartModel = await GetCartViewModelAsync(branchId, userId, sessionId);

            return new CartOperationResultDto
            {
                Success = true,
                Message = message,
                TotalQuantity = cartModel.TotalItemsCount,
                SubTotal = cartModel.SubTotal,
                FinalTotal = cartModel.FinalTotal,
                AdjustedQuantity = targetQty,
                HasStockAlert = hasStockAlert
            };
        }

        // 3. CẬP NHẬT TỨC THỜI SỐ LƯỢNG (DÙNG CHO FETCH API / AJAX TẠI POS & CART)
        public async Task<CartOperationResultDto> UpdateQuantityInstantAsync(int productId, int newQuantity, int branchId, int? userId, string sessionId)
        {
            var inventory = await _context.Inventories
                .FirstOrDefaultAsync(i => i.ProductId == productId && i.BranchId == branchId);
            int availableStock = inventory?.StockQuantity ?? 0;

            var items = await GetRawCartItemsAsync(userId, sessionId);
            var existing = items.FirstOrDefault(i => i.ProductId == productId);
            if (existing == null)
            {
                return new CartOperationResultDto { Success = false, Message = "Không tìm thấy sản phẩm trong giỏ hàng." };
            }

            bool hasStockAlert = false;
            string message = "Đã cập nhật số lượng thành công.";

            if (newQuantity > availableStock)
            {
                newQuantity = availableStock;
                hasStockAlert = true;
                message = $"Tồn kho khả dụng chỉ còn {availableStock} {inventory?.Product?.Unit ?? "sản phẩm"}!";
            }

            if (newQuantity <= 0)
            {
                items.Remove(existing);
            }
            else
            {
                existing.Quantity = newQuantity;
            }

            await SaveRawCartItemsAsync(items, userId, sessionId);
            var cartModel = await GetCartViewModelAsync(branchId, userId, sessionId);
            var updatedItem = cartModel.Items.FirstOrDefault(i => i.ProductId == productId);

            return new CartOperationResultDto
            {
                Success = true,
                Message = message,
                TotalQuantity = cartModel.TotalItemsCount,
                SubTotal = cartModel.SubTotal,
                FinalTotal = cartModel.FinalTotal,
                ItemTotal = updatedItem?.TotalPrice ?? 0,
                AdjustedQuantity = newQuantity,
                HasStockAlert = hasStockAlert
            };
        }

        // 4. XÓA SẢN PHẨM KHỎI GIỎ
        public async Task<CartOperationResultDto> RemoveItemAsync(int productId, int? userId, string sessionId)
        {
            var items = await GetRawCartItemsAsync(userId, sessionId);
            items.RemoveAll(i => i.ProductId == productId);
            await SaveRawCartItemsAsync(items, userId, sessionId);

            return new CartOperationResultDto { Success = true, Message = "Đã xóa sản phẩm khỏi giỏ hàng." };
        }

        // 5. XÓA SẠCH GIỎ HÀNG (KHI THANH TOÁN XONG HOẶC HỦY ĐƠN)
        public async Task ClearCartAsync(int? userId, string sessionId)
        {
            Session.Remove(SessionCartKey);
            if (userId.HasValue && userId.Value > 0)
            {
                var dbItems = _context.CartItems.Where(c => c.UserId == userId.Value);
                _context.CartItems.RemoveRange(dbItems);
                await _context.SaveChangesAsync();
            }
        }

        // 6. ĐỒNG BỘ SESSION CART XUỐNG DATABASE KHI USER ĐĂNG NHẬP
        public async Task SyncSessionCartToDatabaseAsync(int userId, string sessionId)
        {
            var sessionJson = Session.GetString(SessionCartKey);
            if (string.IsNullOrEmpty(sessionJson)) return;

            var sessionItems = JsonSerializer.Deserialize<List<SimpleCartItem>>(sessionJson) ?? new();
            var dbItems = await _context.CartItems.Where(c => c.UserId == userId).ToListAsync();

            foreach (var sItem in sessionItems)
            {
                var match = dbItems.FirstOrDefault(d => d.ProductId == sItem.ProductId);
                if (match != null)
                {
                    match.Quantity += sItem.Quantity;
                }
                else
                {
                    _context.CartItems.Add(new CartItem
                    {
                        UserId = userId,
                        ProductId = sItem.ProductId,
                        Quantity = sItem.Quantity,
                        AddedAt = DateTime.Now
                    });
                }
            }

            await _context.SaveChangesAsync();
            Session.Remove(SessionCartKey);
        }

        // --- CÁC HÀM TIỆN ÍCH LƯU TRỮ RAW CART ---
        private async Task<List<SimpleCartItem>> GetRawCartItemsAsync(int? userId, string sessionId)
        {
            if (userId.HasValue && userId.Value > 0)
            {
                return await _context.CartItems
                    .Where(c => c.UserId == userId.Value)
                    .Select(c => new SimpleCartItem { ProductId = c.ProductId, Quantity = c.Quantity })
                    .ToListAsync();
            }

            var json = Session.GetString(SessionCartKey);
            if (string.IsNullOrEmpty(json)) return new List<SimpleCartItem>();
            return JsonSerializer.Deserialize<List<SimpleCartItem>>(json) ?? new();
        }

        private async Task SaveRawCartItemsAsync(List<SimpleCartItem> items, int? userId, string sessionId)
        {
            if (userId.HasValue && userId.Value > 0)
            {
                var currentDb = await _context.CartItems.Where(c => c.UserId == userId.Value).ToListAsync();
                _context.CartItems.RemoveRange(currentDb);

                foreach (var i in items)
                {
                    _context.CartItems.Add(new CartItem
                    {
                        UserId = userId.Value,
                        ProductId = i.ProductId,
                        Quantity = i.Quantity,
                        AddedAt = DateTime.Now
                    });
                }
                await _context.SaveChangesAsync();
            }
            else
            {
                Session.SetString(SessionCartKey, JsonSerializer.Serialize(items));
            }
        }

        private class SimpleCartItem
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; }
        }
    }
}
