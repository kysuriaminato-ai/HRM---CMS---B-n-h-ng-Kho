using ERP.Application.Common.Interfaces;
using ERP_HRM_CMS.Models.ViewModels;
using ERP_HRM_CMS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP_HRM_CMS.Controllers
{
    // Giỏ hàng dùng được cả khi khách chưa đăng nhập (lưu theo phiên).
    [AllowAnonymous]
    public class GioHangController : Controller
    {
        private readonly ICartService _cartService;
        private readonly ICurrentUserService _currentUser;

        public GioHangController(ICartService cartService, ICurrentUserService currentUser)
        {
            _cartService = cartService;
            _currentUser = currentUser;
        }

        private int? CurrentUserId => _currentUser.UserId;
        private string CurrentSessionId => HttpContext.Session.Id;

        // Trang giỏ hàng chuẩn
        [HttpGet]
        public async Task<IActionResult> Index(int? branchId)
        {
            int currentBranchId = branchId ?? 1; // Chi nhánh mặc định
            var vm = await _cartService.GetCartViewModelAsync(currentBranchId, CurrentUserId, CurrentSessionId);
            ViewBag.CurrentBranchId = currentBranchId;
            return View(vm);
        }

        // AJAX API: Thêm vào giỏ hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCartApi([FromBody] AddToCartRequestDto dto)
        {
            int branchId = dto.BranchId ?? 1;
            var result = await _cartService.AddToCartAsync(dto.ProductId, dto.Quantity, branchId, CurrentUserId, CurrentSessionId);
            return Json(result);
        }

        // AJAX API: Cập nhật số lượng tức thời (Kiểm tra kho trực tiếp)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantityApi([FromBody] UpdateCartQuantityDto dto)
        {
            int branchId = dto.BranchId ?? 1;
            var result = await _cartService.UpdateQuantityInstantAsync(dto.ProductId, dto.NewQuantity, branchId, CurrentUserId, CurrentSessionId);
            return Json(result);
        }

        // AJAX API: Xóa sản phẩm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveItemApi(int productId)
        {
            var result = await _cartService.RemoveItemAsync(productId, CurrentUserId, CurrentSessionId);
            return Json(result);
        }

        // Xóa toàn bộ giỏ hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearCart(int? branchId)
        {
            await _cartService.ClearCartAsync(CurrentUserId, CurrentSessionId);
            TempData["SuccessMessage"] = "Đã dọn sạch giỏ hàng!";
            return RedirectToAction(nameof(Index), new { branchId });
        }
    }
}
