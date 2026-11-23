using ERP.Application.Common.Interfaces;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Models.ViewModels;
using ERP_HRM_CMS.Services;
using ERP_HRM_CMS.Services.Kho;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP_HRM_CMS.Controllers
{
    /// <summary>
    /// Trang thanh toán dành cho KHÁCH HÀNG, nằm trong khu vực Shop nên dùng giao diện
    /// bán hàng, không có thanh menu quản trị.
    ///
    /// Khác biệt so với màn hình POS của nhân viên (Areas/Staff/BanHangController):
    /// - Không yêu cầu quyền nội bộ, khách chưa đăng nhập vẫn đặt được hàng.
    /// - Không cho nhập giảm giá thủ công. Chiết khấu chỉ đến từ hạng thẻ thành viên.
    /// - Đơn tạo ra ở trạng thái chờ xử lý để nhân viên duyệt sau.
    /// </summary>
    [AllowAnonymous]
    [Area("Shop")]
    public class ThanhToanController : Controller
    {
        private const int DefaultBranchId = 1;

        private readonly ICartService _cartService;
        private readonly ICurrentUserService _currentUser;
        private readonly AppDbContext _context;

        public ThanhToanController(ICartService cartService, ICurrentUserService currentUser, AppDbContext context)
        {
            _cartService = cartService;
            _currentUser = currentUser;
            _context = context;
        }

        private int? CurrentUserId => _currentUser.UserId;
        private string CurrentSessionId => HttpContext.Session.Id;

        /// <summary>Chiết khấu theo hạng thẻ, trùng với quyền lợi công bố trên trang chủ.</summary>
        private static decimal DiscountPercentOf(string tierCode) => tierCode switch
        {
            "DIAMOND" => 12m,
            "PLATINUM" => 8m,
            "GOLD" => 5m,
            _ => 0m
        };

        /// <summary>
        /// Tìm hồ sơ khách hàng của người đang đăng nhập, dựa trên email tài khoản.
        /// Khách chưa đăng nhập thì chưa có hồ sơ, trả về null.
        /// </summary>
        private async Task<Customer?> TimHoSoKhachHangAsync()
        {
            if (CurrentUserId is not int userId || userId <= 0)
            {
                return null;
            }

            var email = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => u.Email)
                .FirstOrDefaultAsync();

            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            return await _context.Customers
                .FirstOrDefaultAsync(c => c.Email == email && !c.IsDeleted);
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var cart = await _cartService.GetCartViewModelAsync(DefaultBranchId, CurrentUserId, CurrentSessionId);
            if (!cart.Items.Any())
            {
                TempData["ErrorMessage"] = "Giỏ hàng đang trống. Mời quý khách chọn sản phẩm trước khi thanh toán.";
                return Redirect("/GioHang");
            }

            var vm = await DungViewModelAsync(cart, new CustomerCheckoutDto());
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DatHang(CustomerCheckoutDto form)
        {
            var cart = await _cartService.GetCartViewModelAsync(DefaultBranchId, CurrentUserId, CurrentSessionId);
            if (!cart.Items.Any())
            {
                TempData["ErrorMessage"] = "Giỏ hàng đang trống nên chưa thể đặt hàng.";
                return Redirect("/GioHang");
            }

            if (!ModelState.IsValid)
            {
                return View(nameof(Index), await DungViewModelAsync(cart, form));
            }

            // Kiểm tra tồn kho trước khi ghi đơn, tránh nhận đơn rồi mới báo hết hàng
            var productIds = cart.Items.Select(x => x.ProductId).Distinct().ToList();
            var variants = await _context.ProductVariants
                .Where(v => productIds.Contains(v.ProductId) && v.IsDefault)
                .ToDictionaryAsync(v => v.ProductId, v => v);

            var variantIds = variants.Values.Select(v => v.Id).ToList();
            var tonKho = await _context.Inventories
                .AsNoTracking()
                .Where(i => i.BranchId == DefaultBranchId && variantIds.Contains(i.VariantId))
                .ToDictionaryAsync(i => i.VariantId, i => i.StockQuantity);

            foreach (var item in cart.Items)
            {
                variants.TryGetValue(item.ProductId, out var variant);
                int conLai = variant != null && tonKho.TryGetValue(variant.Id, out var sl) ? sl : 0;

                if (conLai < item.Quantity)
                {
                    ModelState.AddModelError(string.Empty,
                        $"Sản phẩm \"{item.ProductName}\" chỉ còn {conLai} {item.Unit}. Mời quý khách giảm số lượng.");
                    return View(nameof(Index), await DungViewModelAsync(cart, form));
                }
            }

            // Hồ sơ khách hàng: ưu tiên hồ sơ gắn với tài khoản, sau đó tìm theo số điện thoại
            var khach = await TimHoSoKhachHangAsync()
                     ?? await _context.Customers.FirstOrDefaultAsync(c => c.Phone == form.CustomerPhone && !c.IsDeleted);

            decimal doanhSoTruoc = khach?.TotalRevenue ?? 0m;
            var hangTruoc = LoyaltyHelper.GetTier(doanhSoTruoc);

            decimal phanTramChietKhau = khach == null ? 0m : DiscountPercentOf(hangTruoc.Code);
            decimal tienChietKhau = Math.Round(cart.SubTotal * phanTramChietKhau / 100m, 0, MidpointRounding.AwayFromZero);
            decimal thanhTien = cart.SubTotal - tienChietKhau;

            bool nhanTaiShowroom = string.Equals(form.PaymentMethod, "SHOWROOM", StringComparison.OrdinalIgnoreCase);

            var order = new Order
            {
                OrderCode = MaChungTu.Sinh("WEB"),
                BranchId = DefaultBranchId,
                CustomerId = khach?.Id,
                CustomerName = form.CustomerName.Trim(),
                CustomerPhone = form.CustomerPhone.Trim(),
                CustomerAddress = form.CustomerAddress.Trim(),
                TotalAmount = cart.SubTotal,
                DiscountAmount = tienChietKhau,
                FinalAmount = thanhTien,
                OrderStatus = "Pending",
                PaymentStatus = "Unpaid",
                PaymentMethod = form.PaymentMethod,
                CreatedBy = CurrentUserId,
                CreatedAt = VietnamTime.Now,
                Note = GhiChuDon(form, phanTramChietKhau, hangTruoc.Name, nhanTaiShowroom)
            };

            // InventoryTransaction.CreatedBy là int bắt buộc và có khoá ngoại tới Users.
            // Khách vãng lai không có tài khoản nên phiếu xuất ghi tên tài khoản hệ thống.
            int nguoiGhiNhan = CurrentUserId ?? await _context.Users
                .Where(u => u.IsSuperAdmin && u.IsActive)
                .OrderBy(u => u.Id)
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            if (nguoiGhiNhan <= 0)
            {
                ModelState.AddModelError(string.Empty,
                    "Hệ thống chưa sẵn sàng nhận đơn trực tuyến. Mời quý khách gọi hotline 1900 6868.");
                return View(nameof(Index), await DungViewModelAsync(cart, form));
            }

            var dongHang = new List<CheckoutCompletedLine>();

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                foreach (var item in cart.Items)
                {
                    variants.TryGetValue(item.ProductId, out var variant);
                    int variantId = variant?.Id
                        ?? await InventoryStockService.ResolveVariantIdAsync(_context, item.ProductId, null);

                    _context.OrderDetails.Add(new OrderDetail
                    {
                        OrderId = order.Id,
                        ProductId = item.ProductId,
                        VariantId = variantId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice
                    });

                    if (!await InventoryStockService.TruTonAsync(_context, DefaultBranchId, variantId, item.Quantity, VietnamTime.Now))
                    {
                        throw new TonKhoKhongDuException(
                            $"Sản phẩm \"{item.ProductName}\" vừa hết hàng. Đơn hàng chưa được ghi nhận.");
                    }

                    _context.InventoryTransactions.Add(new InventoryTransaction
                    {
                        TransactionCode = $"PXK-WEB-{order.Id}-{item.ProductId}",
                        TransactionType = "OUT",
                        BranchId = DefaultBranchId,
                        ProductId = item.ProductId,
                        VariantId = variantId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        Note = $"Giữ hàng cho đơn đặt trực tuyến {order.OrderCode}",
                        CreatedBy = nguoiGhiNhan,
                        CreatedAt = VietnamTime.Now
                    });

                    dongHang.Add(new CheckoutCompletedLine
                    {
                        ProductName = item.ProductName,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice
                    });
                }

                // Tạo hồ sơ khách hàng nếu chưa có, để lần sau khách được tính hạng thẻ
                if (khach == null)
                {
                    khach = new Customer
                    {
                        Code = MaChungTu.Sinh("KH"),
                        FullName = form.CustomerName.Trim(),
                        Phone = form.CustomerPhone.Trim(),
                        Email = form.CustomerEmail?.Trim(),
                        Address = form.CustomerAddress.Trim(),
                        CustomerType = "Individual",
                        IsActive = true,
                        CreatedAt = VietnamTime.Now
                    };
                    _context.Customers.Add(khach);
                    await _context.SaveChangesAsync();

                    order.CustomerId = khach.Id;
                }

                // Cộng doanh số tích lũy để hạng thẻ phản ánh đúng chi tiêu trọn đời
                khach.TotalRevenue += thanhTien;
                khach.UpdatedAt = VietnamTime.Now;

                await _context.SaveChangesAsync();
                await _cartService.ClearCartAsync(CurrentUserId, CurrentSessionId);
                await transaction.CommitAsync();
            }
            catch (TonKhoKhongDuException ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(nameof(Index), await DungViewModelAsync(cart, form));
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError(string.Empty,
                    "Hệ thống chưa ghi nhận được đơn hàng. Mời quý khách thử lại hoặc gọi hotline 1900 6868.");
                return View(nameof(Index), await DungViewModelAsync(cart, form));
            }

            var hangSau = LoyaltyHelper.GetTier(khach.TotalRevenue);

            var ketQua = new CheckoutCompletedViewModel
            {
                OrderCode = order.OrderCode,
                CustomerName = order.CustomerName,
                CustomerPhone = order.CustomerPhone,
                CustomerAddress = order.CustomerAddress ?? string.Empty,
                PaymentMethodLabel = NhanPhuongThuc(order.PaymentMethod),
                TotalAmount = order.TotalAmount,
                DiscountAmount = order.DiscountAmount,
                FinalAmount = order.FinalAmount,
                CreatedAt = order.CreatedAt,
                Lines = dongHang,
                LoyaltyTierName = hangSau.Name,
                LoyaltyTierUpgraded = !string.Equals(hangSau.Code, hangTruoc.Code, StringComparison.Ordinal)
            };

            return View(nameof(HoanTat), ketQua);
        }

        [HttpGet]
        public IActionResult HoanTat()
        {
            // Vào thẳng địa chỉ này mà không qua đặt hàng thì không có gì để hiển thị
            return Redirect("/");
        }

        private async Task<CustomerCheckoutViewModel> DungViewModelAsync(CartViewModel cart, CustomerCheckoutDto form)
        {
            var khach = await TimHoSoKhachHangAsync();
            var hang = LoyaltyHelper.GetTier(khach?.TotalRevenue ?? 0m);
            decimal phanTram = khach == null ? 0m : DiscountPercentOf(hang.Code);
            decimal tienGiam = Math.Round(cart.SubTotal * phanTram / 100m, 0, MidpointRounding.AwayFromZero);

            // Điền sẵn thông tin đã biết để khách đỡ phải gõ lại
            if (khach != null)
            {
                if (string.IsNullOrWhiteSpace(form.CustomerName)) form.CustomerName = khach.FullName;
                if (string.IsNullOrWhiteSpace(form.CustomerPhone)) form.CustomerPhone = khach.Phone ?? string.Empty;
                if (string.IsNullOrWhiteSpace(form.CustomerEmail)) form.CustomerEmail = khach.Email;
                if (string.IsNullOrWhiteSpace(form.CustomerAddress)) form.CustomerAddress = khach.Address ?? string.Empty;
            }

            return new CustomerCheckoutViewModel
            {
                Cart = cart,
                Form = form,
                LoyaltyTierCode = khach == null ? null : hang.Code,
                LoyaltyTierName = khach == null ? null : hang.Name,
                LoyaltyDiscountPercent = phanTram,
                DiscountAmount = tienGiam,
                FinalAmount = cart.SubTotal - tienGiam
            };
        }

        private static string GhiChuDon(CustomerCheckoutDto form, decimal phanTram, string tenHang, bool nhanTaiShowroom)
        {
            var phan = new List<string>();

            if (phanTram > 0)
            {
                phan.Add($"Chiết khấu hạng {tenHang}: {phanTram:0.#}%");
            }

            if (nhanTaiShowroom)
            {
                phan.Add("Khách nhận tại showroom");
            }

            if (!string.IsNullOrWhiteSpace(form.Note))
            {
                phan.Add(form.Note.Trim());
            }

            return phan.Count == 0 ? "Đơn đặt trực tuyến" : string.Join(" | ", phan);
        }

        private static string NhanPhuongThuc(string method) => method switch
        {
            "BANK" => "Chuyển khoản ngân hàng",
            "SHOWROOM" => "Thanh toán khi nhận tại showroom",
            _ => "Thanh toán khi nhận hàng (COD)"
        };
    }
}
