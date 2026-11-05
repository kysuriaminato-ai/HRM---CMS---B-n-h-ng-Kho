using ERP_HRM_CMS.Models.ViewModels;

namespace ERP_HRM_CMS.Helpers
{
    public static class MenuFeatureDefinitions
    {
        public static List<MenuFeatureCheckItem> GetAllFeatures()
        {
            return new List<MenuFeatureCheckItem>
            {
                // 1. Cổng Nhân Viên
                new() {
                    Code = "ess",
                    Name = "Cổng Nhân Viên (ESS)",
                    Group = "Cổng Nhân Viên (Tự phục vụ)",
                    Icon = "bi-person-workspace",
                    Description = "Xem hồ sơ, lịch ca kíp, bảng lương cá nhân và nộp đơn từ trực tuyến",
                    PermissionCodes = new() { "hrm.self.service", "hrm.leave.manage" }
                },

                // 2. Nhân sự & Chấm công
                new() {
                    Code = "hrm_dashboard",
                    Name = "Dashboard Nhân sự",
                    Group = "Nhân sự & Chấm công",
                    Icon = "bi-speedometer2",
                    Description = "Biểu đồ tổng quan định biên, cơ cấu nhân sự",
                    PermissionCodes = new() { "report.hrm" }
                },
                new() {
                    Code = "hrm_employee",
                    Name = "Hồ sơ nhân sự",
                    Group = "Nhân sự & Chấm công",
                    Icon = "bi-person-badge",
                    Description = "Xem và quản lý toàn bộ danh sách hồ sơ nhân viên",
                    PermissionCodes = new() { "hrm.employee.view", "hrm.employee.manage" }
                },
                new() {
                    Code = "hrm_department",
                    Name = "Phòng ban & Org Chart",
                    Group = "Nhân sự & Chấm công",
                    Icon = "bi-diagram-3",
                    Description = "Cây cơ cấu phòng ban và sơ đồ tổ chức công ty",
                    PermissionCodes = new() { "hrm.employee.view" }
                },
                new() {
                    Code = "hrm_contract",
                    Name = "Hợp đồng & Quyết định nhân sự",
                    Group = "Nhân sự & Chấm công",
                    Icon = "bi-file-earmark-text",
                    Description = "Hợp đồng lao động, phụ lục, khen thưởng kỷ luật và tài sản",
                    PermissionCodes = new() { "hrm.employee.manage" }
                },
                new() {
                    Code = "hrm_recruitment",
                    Name = "Tuyển dụng ATS & Onboarding",
                    Group = "Nhân sự & Chấm công",
                    Icon = "bi-briefcase",
                    Description = "Tin tuyển dụng, Kanban ứng viên và luồng Onboarding/Offboarding",
                    PermissionCodes = new() { "hrm.employee.manage" }
                },
                new() {
                    Code = "hrm_attendance",
                    Name = "Chấm công & Phân ca",
                    Group = "Nhân sự & Chấm công",
                    Icon = "bi-calendar2-check",
                    Description = "Chấm công máy, bảng công tháng, phân ca tuần và heatmap",
                    PermissionCodes = new() { "hrm.attendance.view", "hrm.attendance.manage" }
                },
                new() {
                    Code = "hrm_leave",
                    Name = "Đơn từ & Phê duyệt",
                    Group = "Nhân sự & Chấm công",
                    Icon = "bi-file-earmark-check",
                    Description = "Quản lý và xét duyệt các loại đơn nghỉ phép, công tác, giải trình",
                    PermissionCodes = new() { "hrm.leave.view", "hrm.leave.approve" }
                },
                new() {
                    Code = "hrm_kpi_lms",
                    Name = "Đánh giá KPI & Đào tạo LMS",
                    Group = "Nhân sự & Chấm công",
                    Icon = "bi-trophy",
                    Description = "Kỳ đánh giá KPI, ma trận 9-Box và quản trị khóa học đào tạo nội bộ",
                    PermissionCodes = new() { "hrm.employee.manage" }
                },
                new() {
                    Code = "hrm_analytics",
                    Name = "Executive People Analytics",
                    Group = "Nhân sự & Chấm công",
                    Icon = "bi-pie-chart",
                    Description = "Trung tâm phân tích dữ liệu nhân sự cấp điều hành & Xuất Excel",
                    PermissionCodes = new() { "report.hrm" }
                },

                // 3. Tiền Lương & C&B
                new() {
                    Code = "payroll",
                    Name = "Bảng lương & Chi trả (C&B)",
                    Group = "Tiền lương & Phúc lợi",
                    Icon = "bi-cash-stack",
                    Description = "Tính toán bảng lương, bảo hiểm, thuế TNCN và phiếu lương nhân viên",
                    PermissionCodes = new() { "hrm.payroll.view", "hrm.payroll.manage" }
                },

                // 4. Bán Hàng & POS
                new() {
                    Code = "sales_pos",
                    Name = "POS bán hàng",
                    Group = "Bán hàng & POS",
                    Icon = "bi-cart3",
                    Description = "Màn hình quầy thu ngân bán lẻ và in hóa đơn POS",
                    PermissionCodes = new() { "order.pos" }
                },
                new() {
                    Code = "sales_order",
                    Name = "Quản lý Đơn hàng & Doanh số",
                    Group = "Bán hàng & POS",
                    Icon = "bi-receipt",
                    Description = "Theo dõi đơn hàng, xuất giao và dashboard doanh số",
                    PermissionCodes = new() { "order.view", "order.manage" }
                },
                new() {
                    Code = "sales_product",
                    Name = "Sản phẩm & Danh mục",
                    Group = "Bán hàng & POS",
                    Icon = "bi-tags",
                    Description = "Quản lý danh mục hàng hóa, thuộc tính và bảng giá sản phẩm",
                    PermissionCodes = new() { "product.view", "product.manage" }
                },
                new() {
                    Code = "sales_debt",
                    Name = "Quản trị Công nợ",
                    Group = "Bán hàng & POS",
                    Icon = "bi-cash-coin",
                    Description = "Sổ theo dõi công nợ bán buôn và thu nợ khách hàng",
                    PermissionCodes = new() { "debt.view", "debt.manage" }
                },

                // 5. Kho Vận & Mua Hàng
                new() {
                    Code = "warehouse_inventory",
                    Name = "Tồn kho thực tế",
                    Group = "Kho vận & Mua hàng",
                    Icon = "bi-boxes",
                    Description = "Theo dõi tồn kho theo chi nhánh/kho, mức tồn an toàn và thẻ kho",
                    PermissionCodes = new() { "inventory.view" }
                },
                new() {
                    Code = "warehouse_inout",
                    Name = "Nhập / Xuất & Kiểm kê",
                    Group = "Kho vận & Mua hàng",
                    Icon = "bi-box-arrow-in-down",
                    Description = "Phiếu nhập kho, phiếu xuất kho, điều chuyển kho và phiếu kiểm kê",
                    PermissionCodes = new() { "inventory.transaction", "inventory.stocktaking" }
                },
                new() {
                    Code = "warehouse_scm",
                    Name = "Đơn mua hàng (PO) & Nhà cung cấp",
                    Group = "Kho vận & Mua hàng",
                    Icon = "bi-truck",
                    Description = "Đơn đặt mua hàng NCC, theo dõi tiến độ giao nhận và hồ sơ NCC",
                    PermissionCodes = new() { "scm.po.view", "scm.po.manage", "scm.supplier.view", "scm.supplier.manage" }
                },

                // 6. Nội Dung CMS
                new() {
                    Code = "cms",
                    Name = "Quản trị Website CMS & Menu",
                    Group = "Nội dung CMS",
                    Icon = "bi-newspaper",
                    Description = "Bài viết tin tức, menu điều hướng đa cấp và bố cục trang web",
                    PermissionCodes = new() { "content.view", "content.create", "menu.view", "layout.manage" }
                },

                // 7. Hệ Thống
                new() {
                    Code = "system",
                    Name = "Quản trị Hệ thống & Phân quyền",
                    Group = "Hệ thống & Cấu hình",
                    Icon = "bi-gear",
                    Description = "Quản lý tài khoản người dùng, phân quyền RBAC/ACL và nhật ký kiểm toán",
                    PermissionCodes = new() { "auth.matrix.view", "auth.user.manage" }
                }
            };
        }
    }
}
