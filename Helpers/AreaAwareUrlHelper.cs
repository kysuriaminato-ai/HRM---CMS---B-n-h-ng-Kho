using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;

namespace ERP_HRM_CMS.Helpers
{
    /// <summary>
    /// Bọc <see cref="UrlHelperFactory"/> để tự động điền giá trị <c>area</c> tương ứng với từng Controller
    /// khi gọi <c>Url.Action</c>, <c>RedirectToAction</c>, hoặc TagHelper (<c>asp-controller</c> / <c>asp-action</c>)
    /// mà không cần sửa từng liên kết trong View khi tách Controller sang các Area <c>Shop</c>, <c>Admin</c>, <c>Staff</c>.
    /// </summary>
    public sealed class AreaAwareUrlHelperFactory : IUrlHelperFactory
    {
        private readonly UrlHelperFactory _inner = new();

        public IUrlHelper GetUrlHelper(ActionContext context)
        {
            return new AreaAwareUrlHelper(_inner.GetUrlHelper(context));
        }
    }

    public sealed class AreaAwareUrlHelper : IUrlHelper
    {
        private static readonly Dictionary<string, string> ControllerAreaMap = new(StringComparer.OrdinalIgnoreCase)
        {
            // Khu vực Shop
            ["Public"] = "Shop",
            ["GioHang"] = "Shop",
            ["Auth"] = "Shop",

            // Khu vực Admin (quản trị hệ thống)
            ["QuanTriTaiKhoan"] = "Admin",
            ["PhanQuyen"] = "Admin",
            ["Content"] = "Admin",
            ["SanPham"] = "Admin",
            ["DanhMuc"] = "Admin",
            ["UI"] = "Admin",
            ["VaiTro"] = "Admin",
            ["Menu"] = "Admin",
            ["LayoutConfig"] = "Admin",
            ["AuditLog"] = "Admin",

            // Khu vực Staff (nghiệp vụ nội bộ)
            ["BanHang"] = "Staff",
            ["Kho"] = "Staff",
            ["CongNo"] = "Staff",
            ["DonHang"] = "Staff",
            ["PurchaseOrder"] = "Staff",
            ["NhanSu"] = "Staff",
            ["ChamCong"] = "Staff",
            ["DonTu"] = "Staff",
            ["BangLuong"] = "Staff",
            ["DaoTao"] = "Staff",
            ["TuyenDung"] = "Staff",
            ["DanhGiaKpi"] = "Staff",
            ["QuyetDinhNhanSu"] = "Staff",
            ["SelfService"] = "Staff",
            ["PhieuIn"] = "Staff",
            ["PhongBan"] = "Staff",
            ["HopDong"] = "Staff",
            ["TaiSanNhanSu"] = "Staff",
            ["Supplier"] = "Staff"
        };

        private readonly IUrlHelper _inner;

        public AreaAwareUrlHelper(IUrlHelper inner)
        {
            _inner = inner;
        }

        public ActionContext ActionContext => _inner.ActionContext;

        public string? Action(UrlActionContext actionContext)
        {
            var controller = actionContext.Controller;
            if (string.IsNullOrEmpty(controller))
            {
                controller = ActionContext.RouteData.Values["controller"]?.ToString();
            }

            if (!string.IsNullOrEmpty(controller))
            {
                var routeValues = new RouteValueDictionary(actionContext.Values);
                if (!routeValues.ContainsKey("area"))
                {
                    routeValues["area"] = ResolveArea(controller);
                    actionContext.Values = routeValues;
                }
            }

            return _inner.Action(actionContext);
        }

        public static string ResolveArea(string controllerName)
        {
            return ControllerAreaMap.TryGetValue(controllerName, out var area) ? area : string.Empty;
        }

        public string? Content(string? contentPath) => _inner.Content(contentPath);

        public bool IsLocalUrl(string? url) => _inner.IsLocalUrl(url);

        public string? Link(string? routeName, object? values) => _inner.Link(routeName, values);

        public string? RouteUrl(UrlRouteContext routeContext) => _inner.RouteUrl(routeContext);
    }
}

