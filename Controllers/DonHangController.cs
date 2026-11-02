using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Filters;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Models.ViewModels;

namespace ERP_HRM_CMS.Controllers
{
    [Authorize]
    public class DonHangController : Controller
    {
        private readonly AppDbContext _context;

        public DonHangController(AppDbContext context)
        {
            _context = context;
        }

        // 1. DANH SÁCH ĐƠN HÀNG
        [HttpGet]
        [KiemTraQuyen("order.view")]
        public async Task<IActionResult> Index(string? keyword, string? orderStatus, string? paymentStatus, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.Orders
                .Include(o => o.Branch)
                .Include(o => o.Creator)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim().ToLower();
                query = query.Where(o => o.OrderCode.ToLower().Contains(keyword) ||
                                         o.CustomerName.ToLower().Contains(keyword) ||
                                         o.CustomerPhone.Contains(keyword));
            }

            if (!string.IsNullOrWhiteSpace(orderStatus))
                query = query.Where(o => o.OrderStatus == orderStatus);

            if (!string.IsNullOrWhiteSpace(paymentStatus))
                query = query.Where(o => o.PaymentStatus == paymentStatus);

            if (fromDate.HasValue)
                query = query.Where(o => o.CreatedAt.Date >= fromDate.Value.Date);

            if (toDate.HasValue)
                query = query.Where(o => o.CreatedAt.Date <= toDate.Value.Date);

            var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();

            var vm = new OrderListViewModel
            {
                Orders = orders,
                Keyword = keyword,
                OrderStatus = orderStatus,
                PaymentStatus = paymentStatus,
                FromDate = fromDate,
                ToDate = toDate,
                TotalRevenue = orders.Where(o => o.OrderStatus != "Cancelled").Sum(o => o.FinalAmount),
                TotalOrdersCount = orders.Count
            };

            return View(vm);
        }

        // 2. CHI TIẾT ĐƠN HÀNG & IN HÓA ĐƠN
        [HttpGet]
        [KiemTraQuyen("order.view")]
        public async Task<IActionResult> ChiTiet(long id)
        {
            var order = await _context.Orders
                .Include(o => o.Branch)
                .Include(o => o.Creator)
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();
            return View(order);
        }

        // 3. DUYỆT ĐƠN / CẬP NHẬT TRẠNG THÁI (CHO GIÁM ĐỐC SALE)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [KiemTraQuyen("order.manage")]
        public async Task<IActionResult> CapNhatTrangThai(long id, string status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            order.OrderStatus = status;
            if (status == "Completed")
            {
                order.PaymentStatus = "Paid";
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã cập nhật trạng thái đơn {order.OrderCode} thành '{status}'!";
            return RedirectToAction(nameof(ChiTiet), new { id });
        }

        // 4. DASHBOARD DOANH SỐ CHO SALE DIRECTOR
        [HttpGet]
        [KiemTraQuyen("report.sales")]
        public async Task<IActionResult> Dashboard()
        {
            var today = DateTime.Today;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);

            // Doanh số hôm nay
            var todayOrders = await _context.Orders
                .Where(o => o.CreatedAt.Date == today && o.OrderStatus != "Cancelled")
                .ToListAsync();

            // Doanh số tháng này
            var monthOrders = await _context.Orders
                .Where(o => o.CreatedAt >= startOfMonth && o.OrderStatus != "Cancelled")
                .ToListAsync();

            // Top sản phẩm bán chạy nhất trong tháng
            var topProducts = await _context.OrderDetails
                .Include(d => d.Order)
                .Include(d => d.Product)
                .Where(d => d.Order.CreatedAt >= startOfMonth && d.Order.OrderStatus != "Cancelled")
                .GroupBy(d => new { d.ProductId, d.Product.SKU, d.Product.Name })
                .Select(g => new TopProductDto
                {
                    ProductId = g.Key.ProductId,
                    SKU = g.Key.SKU,
                    ProductName = g.Key.Name,
                    TotalQuantitySold = g.Sum(x => x.Quantity),
                    TotalSalesValue = g.Sum(x => x.TotalPrice ?? (x.Quantity * x.UnitPrice))
                })
                .OrderByDescending(x => x.TotalQuantitySold)
                .Take(5)
                .ToListAsync();

            // Đơn hàng gần nhất
            var recent = await _context.Orders
                .OrderByDescending(o => o.CreatedAt)
                .Take(8)
                .Select(o => new RecentOrderDto
                {
                    OrderId = o.Id,
                    OrderCode = o.OrderCode,
                    CustomerName = o.CustomerName,
                    FinalAmount = o.FinalAmount,
                    OrderStatus = o.OrderStatus,
                    PaymentStatus = o.PaymentStatus,
                    CreatedAt = o.CreatedAt
                })
                .ToListAsync();

            var vm = new SalesDashboardViewModel
            {
                TodayRevenue = todayOrders.Sum(o => o.FinalAmount),
                TodayOrderCount = todayOrders.Count,
                MonthRevenue = monthOrders.Sum(o => o.FinalAmount),
                MonthOrderCount = monthOrders.Count,
                TopSellingProducts = topProducts,
                RecentOrders = recent
            };

            return View(vm);
        }

        // =========================================================================
        // 5. IN HÓA ĐƠN BÁN HÀNG POS (KHỔ IN NHIỆT 80MM HOẶC KHỔ A5 VỚI VIETQR ĐỘNG)
        // =========================================================================
        [HttpGet]
        [KiemTraQuyen("order.view")]
        public async Task<IActionResult> InHoaDon(long id)
        {
            var order = await _context.Orders
                .Include(o => o.Branch)
                .Include(o => o.Creator)
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();
            return View(order);
        }

        // =========================================================================
        // 6. XUẤT DANH SÁCH ĐƠN HÀNG & DOANH THU THÁNG RA EXCEL (CLOSEDXML)
        // =========================================================================
        [HttpGet]
        [KiemTraQuyen("report.sales")]
        public async Task<IActionResult> ExportDoanhThuExcel(int? month, int? year)
        {
            int selectedMonth = month ?? DateTime.Now.Month;
            int selectedYear = year ?? DateTime.Now.Year;

            var orders = await _context.Orders
                .Include(o => o.Branch)
                .Include(o => o.Creator)
                .Where(o => o.CreatedAt.Month == selectedMonth && o.CreatedAt.Year == selectedYear)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var worksheet = workbook.Worksheets.Add($"DoanhThu_{selectedMonth}_{selectedYear}");

            // 1. Tiêu đề
            worksheet.Cell("A1").Value = "TIÊN PHONG ENTERPRISE - BÁO CÁO DOANH THU & ĐƠN HÀNG BÁN LẺ";
            worksheet.Cell("A1").Style.Font.Bold = true;
            worksheet.Cell("A1").Style.Font.FontSize = 14;

            worksheet.Cell("A2").Value = $"KỲ BÁO CÁO: THÁNG {selectedMonth:00}/{selectedYear} (Xuất lúc: {DateTime.Now:dd/MM/yyyy HH:mm})";
            worksheet.Cell("A2").Style.Font.Bold = true;
            worksheet.Cell("A2").Style.Font.FontColor = ClosedXML.Excel.XLColor.FromHtml("#2563EB");

            // 2. Headers
            string[] headers = { "STT", "Mã Hóa Đơn", "Ngày Bán", "Khách Hàng", "Số Điện Thoại", "Chi Nhánh", "Người Lập", "Phương Thức", "Trạng Thái Đơn", "Trạng Thái TT", "Tổng Tiền (đ)", "Giảm Giá (đ)", "Doanh Thu Thực (đ)" };
            for (int col = 1; col <= headers.Length; col++)
            {
                var cell = worksheet.Cell(4, col);
                cell.Value = headers[col - 1];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
                cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#0F172A");
                cell.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = ClosedXML.Excel.XLAlignmentVerticalValues.Center;
            }
            worksheet.Row(4).Height = 25;

            // 3. Data rows
            int row = 5;
            int stt = 1;
            foreach (var o in orders)
            {
                worksheet.Cell(row, 1).Value = stt++;
                worksheet.Cell(row, 1).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

                worksheet.Cell(row, 2).Value = o.OrderCode;
                worksheet.Cell(row, 3).Value = o.CreatedAt.ToString("dd/MM/yyyy HH:mm");
                worksheet.Cell(row, 4).Value = o.CustomerName;
                worksheet.Cell(row, 5).Value = o.CustomerPhone;
                worksheet.Cell(row, 6).Value = o.Branch?.Name ?? "Chính";
                worksheet.Cell(row, 7).Value = o.Creator?.Username ?? "System";
                worksheet.Cell(row, 8).Value = o.PaymentMethod;
                worksheet.Cell(row, 9).Value = o.OrderStatus;
                worksheet.Cell(row, 10).Value = o.PaymentStatus;

                worksheet.Cell(row, 11).Value = o.TotalAmount;
                worksheet.Cell(row, 11).Style.NumberFormat.Format = "#,##0";

                worksheet.Cell(row, 12).Value = o.DiscountAmount;
                worksheet.Cell(row, 12).Style.NumberFormat.Format = "#,##0";

                worksheet.Cell(row, 13).Value = o.FinalAmount;
                worksheet.Cell(row, 13).Style.NumberFormat.Format = "#,##0";
                worksheet.Cell(row, 13).Style.Font.Bold = true;

                if (row % 2 == 1)
                {
                    worksheet.Row(row).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#F8FAFC");
                }
                row++;
            }

            // Summary row
            worksheet.Cell(row, 1).Value = "TỔNG CỘNG DOANH THU";
            worksheet.Range(row, 1, row, 10).Merge();
            worksheet.Cell(row, 1).Style.Font.Bold = true;
            worksheet.Cell(row, 1).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

            worksheet.Cell(row, 11).FormulaA1 = $"SUM(K5:K{row - 1})";
            worksheet.Cell(row, 11).Style.Font.Bold = true;
            worksheet.Cell(row, 11).Style.NumberFormat.Format = "#,##0";

            worksheet.Cell(row, 12).FormulaA1 = $"SUM(L5:L{row - 1})";
            worksheet.Cell(row, 12).Style.Font.Bold = true;
            worksheet.Cell(row, 12).Style.NumberFormat.Format = "#,##0";

            worksheet.Cell(row, 13).FormulaA1 = $"SUM(M5:M{row - 1})";
            worksheet.Cell(row, 13).Style.Font.Bold = true;
            worksheet.Cell(row, 13).Style.Font.FontColor = ClosedXML.Excel.XLColor.FromHtml("#10B981");
            worksheet.Cell(row, 13).Style.NumberFormat.Format = "#,##0";

            worksheet.Row(row).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#E2E8F0");
            worksheet.Row(row).Height = 22;

            worksheet.Range(4, 1, row, 13).Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
            worksheet.Range(4, 1, row, 13).Style.Border.InsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            return File(
                content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"BaoCao_DoanhThu_{selectedMonth:00}_{selectedYear}.xlsx"
            );
        }
    }
}
