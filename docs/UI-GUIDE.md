# UI Guide — ERP HRM · CMS · Bán hàng & Kho (Giai đoạn A)

> Tài liệu chuẩn hóa Design Tokens, Layout hợp nhất, 12 Shared Components, Micro-interactions và quy ước giao diện quản trị.
> Cập nhật: Giai đoạn A (2026-11-04) — Xem trực tiếp tại `/UI/Styleguide` (Super Admin).

---

## 1. Hệ thống Design Tokens (`wwwroot/css/enterprise-theme.css`)

Tất cả giá trị màu sắc, bo góc, khoảng cách, đổ bóng và tốc độ chuyển động được khai báo tập trung qua CSS Custom Properties (`--erp-*`).
**Tuyệt đối không hard-code mã màu `#hex`, `rgb(...)`, `rgba(...)` trực tiếp trong các View `.cshtml`.**

### 1.1 Chế độ Sáng / Tối (Light / Dark Mode)
- Mặc định `:root` là chế độ Sáng (`Light`).
- Chế độ Tối kích hoạt khi `html[data-theme="dark"]`, `html[data-bs-theme="dark"]` hoặc `.dark-mode`.
- Anti-FOUC script trong `<head>` của `_Layout.cshtml` và `_ErrorDocument.cshtml` đọc `localStorage.getItem('erp-theme')` (fallback `prefers-color-scheme: dark`) trước khi render.

### 1.2 Màu nhấn theo phân hệ (`[data-module]`)
Mỗi phân hệ tự động ghi đè `--erp-primary`, `--erp-primary-rgb`, `--erp-primary-hover`, `--erp-primary-subtle` cho cả Light & Dark mode:

| `ViewData["Module"]` | Thuộc tính HTML                 | Ý nghĩa phân hệ        | Tông màu chủ đạo  |
|----------------------|---------------------------------|------------------------|-------------------|
| `"system"`           | `data-module="system"`          | Hệ thống & Tổng quan   | Royal Blue        |
| `"hrm"`              | `data-module="hrm"`             | Nhân sự & Chấm công    | Indigo            |
| `"payroll"`          | `data-module="payroll"`         | Lương & Phúc lợi       | Emerald           |
| `"sales"`            | `data-module="sales"`           | Bán hàng, POS & Công nợ| Teal / Cyan       |
| `"warehouse"`        | `data-module="warehouse"`       | Kho vận & Mua hàng PO  | Amber / Orange    |
| `"cms"`              | `data-module="cms"`             | Nội dung & Giao diện   | Violet            |

### 1.3 Nhóm Token nền tảng
- **Surface & Text**: `--erp-bg-body`, `--erp-bg-card`, `--erp-bg-surface`, `--erp-bg-subtle`, `--erp-bg-hover`, `--erp-bg-sidebar`, `--erp-text-main`, `--erp-text-secondary`, `--erp-text-muted`, `--erp-border-color`, `--erp-border-strong`.
- **Semantic**: `--erp-success`, `--erp-warning`, `--erp-danger`, `--erp-info`, `--erp-purple` (mỗi màu đều có biến thể `-rgb`, `-hover`, `-subtle`).
- **Spacing**: `--erp-space-1` (`4px`) → `--erp-space-12` (`48px`).
- **Border Radius**: `--erp-radius-xs` (`4px`), `sm` (`6px`), `md` (`8px`), `lg` (`12px`), `xl` (`16px`), `2xl` (`20px`), `full` (`9999px`).
- **Shadow**: `--erp-shadow-xs`, `sm`, `md`, `lg`, `xl`, `--erp-card-shadow`, `--erp-elevated-shadow`.
- **Motion**: `--erp-duration-instant` (`80ms`), `fast` (`150ms`), `normal` (`240ms`), `slow` (`380ms`), `slower` (`550ms`); `--erp-ease-default`, `out`, `in`, `spring`.

---

## 2. Layout Hợp Nhất (`Views/Shared/_Layout.cshtml`)

- **Tất cả View quản trị** dùng chung `Views/Shared/_Layout.cshtml` (mặc định qua `Views/_ViewStart.cshtml`) và chỉ cần khai báo `ViewData["Module"] = "hrm" | "payroll" | "sales" | "warehouse" | "cms" | "system"`.
- **Trang công khai** (`Views/Public/*`) tiếp tục dùng riêng `Views/Shared/_PublicLayout.cshtml`.
- **Accordion Sidebar**: các nhóm chức năng đóng/mở mượt bằng CSS Grid (`0fr → 1fr`), tự động mở nhóm chứa trang hiện tại và lưu trạng thái vào `localStorage('erp_sidebar_groups')`.
- **Topbar**: hiển thị huy hiệu phân hệ hiện tại (`.erp-module-pill`), tìm kiếm nhanh `Ctrl+K`, thông báo thời gian thực SignalR, nút chuyển Sáng/Tối Circular Reveal và Avatar người dùng.

---

## 3. Bộ 12 Shared UI Components (Tag Helpers & Partial Views)

Đã đăng ký toàn cục qua `@addTagHelper *, ERP_HRM_CMS` trong `Views/_ViewImports.cshtml`:

1. **PageHeader** (`<erp-page-header>` / `Views/Shared/UI/_PageHeader.cshtml`):
   ```html
   <erp-page-header title="Danh sách nhân sự"
                    subtitle="Quản lý hồ sơ, phòng ban và hợp đồng"
                    icon="bi-people"
                    breadcrumbs="Tổng quan:/Home/Index|Nhân sự">
       <a href="/NhanSu/Create" class="btn btn-primary btn-sm"><i class="bi bi-plus-lg me-1"></i>Thêm mới</a>
   </erp-page-header>
   ```
2. **StatCard** (`<erp-stat-card>` / `Views/Shared/UI/_StatCard.cshtml`):
   ```html
   <erp-stat-card label="Doanh thu tháng" count-up-target="125000000" suffix=" đ"
                  trend="+12.5%" trend-dir="up" icon="bi-currency-dollar" variant="primary" />
   ```
3. **FilterBar** (`<erp-filter-bar>`):
   ```html
   <erp-filter-bar target-table="empTable" action-url="/NhanSu/TableData" search-placeholder="Tìm tên, mã NV...">
       <select name="deptId" class="form-select form-select-sm w-auto">...</select>
   </erp-filter-bar>
   ```
4. **DataTable AJAX** (`<erp-data-table>` + `Views/Shared/UI/_Pagination.cshtml`):
   ```html
   <erp-data-table id="empTable" title="Hồ sơ nhân viên" ajax-url="/NhanSu/TableData"
                   page-index="1" total-pages="5" total-count="48" page-size="10"
                   sort-by="code" sort-dir="asc">
       <table class="table table-hover align-middle mb-0">
           <thead><tr><th data-sort="code">Mã NV</th>...</tr></thead>
           <tbody>...</tbody>
       </table>
   </erp-data-table>
   ```
5. **EmptyState** (`<erp-empty-state>` / `Views/Shared/UI/_EmptyState.cshtml`)
6. **Skeleton** (`<erp-skeleton type="card|list|table|avatar" rows="3" />`)
7. **ConfirmModal** (`<erp-confirm-modal>` / `Views/Shared/UI/_ConfirmModal.cshtml` / thuộc tính `data-erp-confirm="..."`)
8. **Drawer** (`<erp-drawer id="..." title="..." position="end|start" size="sm|md|lg">` + `<erp-drawer-footer>`)
9. **Tabs trượt indicator** (`<erp-tabs>` + `<erp-tab id="..." title="..." icon="..." badge="..." active="true">`)
10. **Status Badge chuẩn `WorkflowStatuses`** (`<erp-status-badge status="@item.Status" workflow="PurchaseOrder" show-icon="true" />`) — ánh xạ qua `Helpers/WorkflowStatusUiHelper.cs` cho toàn bộ 5 enum trong `Models/Enums/WorkflowStatuses.cs` (`PurchaseOrderStatus`, `DebtStatus`, `LeaveRequestStatus`, `PayrollPeriodStatus`, `SalarySlipStatus`).
11. **Avatar** (`<erp-avatar name="Nguyễn Văn A" src="..." size="xs|sm|md|lg|xl" status="online|busy|away|offline" />`)
12. **Timeline** (`<erp-timeline>` + `<erp-timeline-item title="..." time="..." actor="..." variant="success" icon="bi-check-lg">`)

---

## 4. Hiệu ứng & JavaScript API (`admin-animations.css`, `admin-ui.js`, `enterprise-theme.js`)

Mọi hiệu ứng đều kiểm tra và tôn trọng `prefers-reduced-motion: reduce`:

| API / Class                                 | Chức năng                                                                 |
|---------------------------------------------|---------------------------------------------------------------------------|
| `window.toggleTheme(event)`                 | Chuyển Sáng/Tối kèm hiệu ứng lan tròn **Circular Reveal** từ vị trí bấm   |
| `window.setErpModule(module)`               | Đổi màu nhấn `data-module` động và phát sự kiện `erp:modulechange`        |
| `ERP.themeColor('--erp-primary')`           | Đọc giá trị token CSS hiện hành cho Chart.js / Canvas                     |
| `ERP.withViewTransition(fn)`                | Bọc cập nhật DOM bằng View Transitions API (có fallback an toàn)          |
| `ERP.toast(msg, variant, durationMs)`       | Toast có thanh đếm ngược (progress bar), tự tạm dừng khi rê chuột (hover) |
| `ERP.confirm(options)` / `[data-erp-confirm]`| Hộp thoại xác nhận dùng chung trả về `Promise<boolean>`                   |
| `ERP.drawer.open(id)` / `ERP.drawer.close(id)`| Đóng/mở ngăn kéo trượt (hỗ trợ phím `ESC` & backdrop)                   |
| `ERP.shake(el)`                             | Rung cảnh báo khi form/input không hợp lệ                                 |
| `ERP.successTick(container, label)`         | Vẽ dấu tích SVG hoàn tất thao tác                                         |
| `ERP.staggerRows(container)`                | Hiệu ứng xuất hiện tuần tự cho các dòng `tbody > tr`                      |

---

## 5. Trang Lỗi & Kiểm Định Giao Diện
- **Trang lỗi chuẩn hóa**: `Views/Shared/Error403.cshtml`, `Error404.cshtml`, `Error500.cshtml`, `Error.cshtml`, `Views/Auth/AccessDenied.cshtml`, `Views/Home/KhongCoQuyen.cshtml` (dùng chung `Views/Shared/UI/_ErrorDocument.cshtml`, hỗ trợ đầy đủ Light/Dark mode và giữ nguyên HTTP Status Code).
- **UI Styleguide**: `/UI/Styleguide` (`Controllers/UIController.cs`) — chỉ cho phép tài khoản **Super Admin** truy cập (xác thực trực tiếp qua `ISuperAdminService`).

---

## 6. Hệ Thống HRM Workspace Components (Phiên 3)

### 6.1 Theme "Nordic People & Indigo Slate" & Chuẩn WCAG AA
- **Độ tương phản chuẩn WCAG AA**:
  - Light mode: `--erp-text-main: #0F172A`, `--erp-text-secondary: #334155`, `--erp-text-muted: #475569` (tương phản > 4.5:1 trên nền trắng và `#F8FAFC`).
  - Dark mode: `--erp-text-main: #F8FAFC`, `--erp-text-secondary: #CBD5E1`, `--erp-text-muted: #94A3B8` (tương phản > 5.5:1 trên nền `#0B0F19` và `#111827`).
  - Màu nhấn trạng thái đạt chuẩn: `--erp-success-text`, `--erp-warning-text`, `--erp-danger-text`, `--erp-info-text` chống chói/mờ.
- **Tabular Nums cho số liệu (`tabular-nums`)**:
  - Mọi con số tiền tệ, giờ công, ngày tháng, mã NV và tỷ lệ phần trăm tự động căn hàng thẳng tắp nhờ `font-variant-numeric: tabular-nums; font-feature-settings: "tnum" 1;`.
  - Hỗ trợ qua các class `.tabular-nums`, `.col-number`, `.col-currency`, `.col-hours`, `.stat-value`.

### 6.2 Table & UI Density Switcher (Comfortable vs Compact)
- Nút chuyển đổi nhanh trực tiếp trên Topbar (`#densityToggleBtn`), lưu trạng thái vào `localStorage('erp_density')` và cookie `erp_density`:
  - `Comfortable`: Chiều cao dòng chuẩn 48px, khoảng đệm rộng rãi phục vụ thao tác chuột thông thường.
  - `Compact`: Chiều cao dòng 32px-34px, font-size `0.8125rem`, tối ưu cho chuyên viên nhân sự và kế toán theo dõi bảng công, bảng lương nhiều cột.
- Hỗ trợ cuộn ngang cố định:
  - `sticky-thead`: Cố định tiêu đề bảng khi cuộn dọc.
  - `sticky-col-1` / `sticky-col-2`: Cố định cột Mã NV và Họ tên khi cuộn ngang màn hình.
  - `sticky-col-actions`: Cố định cột Thao tác ở mép phải.

### 6.3 Bento Dashboard Grid (`wwwroot/css/hrm-workspace.css`)
- Bố cục lưới 12 cột đáp ứng (`.erp-bento-grid`, `.bento-col-12`, `.bento-col-8`, `.bento-col-6`, `.bento-col-4`, `.bento-col-3`).
- Hiệu ứng viền sáng tinh tế `.border-glow`, nâng thẻ khi hover `.hover-lift` và skeleton tải dữ liệu `.bento-shimmer`.

### 6.4 Interactive Org Chart (Sơ đồ cây tổ chức)
- Hiển thị cây phân cấp phòng ban với thẻ lãnh đạo `.org-node-card` (avatar, tên, chức danh, mã phòng, số lượng nhân sự biên chế).
- Hỗ trợ thu gọn/mở rộng nhánh con `.org-toggle-btn` có hiệu ứng mượt mà.
- Khung nhìn tương tác `.erp-org-viewport` hỗ trợ phóng to/thu nhỏ (`zoom-in`, `zoom-out`, `zoom-reset`) và kéo chuột để di chuyển (drag-to-pan).
- API khởi tạo: `ERP.orgChart.init()`.

### 6.5 ATS Kanban Board (Pipeline Tuyển Dụng)
- Bố cục cột ngang `.erp-kanban-board` cuộn độc lập `.kanban-column-body`.
- Thẻ ứng viên `.kanban-card` hiển thị điểm đánh giá (0-100), tag vị trí tuyển dụng, avatar và ngày nộp.
- Tương tác kéo-thả HTML5 Drag & Drop mượt mà kèm hiệu ứng nhấc thẻ nghiêng `.is-dragging` (`transform: rotate(2.5deg) scale(1.02);`), tự động cập nhật bộ đếm `.kanban-counter` trên đầu cột.
- API khởi tạo: `ERP.kanban.init()`.

### 6.6 Calendar & Shift Matrix Grid
- Ma trận theo dõi ngày công thực tế theo ngày trong tháng `.erp-shift-matrix-container`.
- Ô ca làm việc trực quan `.erp-shift-cell` với mã trạng thái phân màu:
  - `shift-ontime`: Đúng giờ (P - xanh lá).
  - `shift-late`: Đi muộn / về sớm (L - hổ phách).
  - `shift-leave`: Nghỉ phép (V - xanh dương).
  - `shift-ot`: Tăng ca (OT - chàm).
  - `shift-trip`: Công tác (CT - xanh ngọc).
  - `shift-absent`: Vắng mặt (X - đỏ).
  - `shift-weekend`: Cuối tuần (- xám).
- Thanh chú giải trạng thái trực quan `.shift-legend-bar`.

### 6.7 Print & PDF Stylesheet (`wwwroot/css/admin-print.css`)
- Tối ưu định dạng in chuẩn khổ A4 dọc và A4 ngang:
  - Ẩn 100% thanh bên (sidebar), thanh trên (topbar), nút bấm, phân trang, thông báo toast khi in.
  - Bố cục văn bản chuẩn: Quốc hiệu Tiêu ngữ, tiêu đề văn bản in `.print-doc-title`, bảng số liệu sắc nét `.print-table`, và khung chữ ký 3 bên `.print-signatures` (Người lập biểu, Kế toán trưởng, Giám đốc / Người lao động).
  - Kiểm soát ngắt trang hoàn hảo `break-inside: avoid`.
