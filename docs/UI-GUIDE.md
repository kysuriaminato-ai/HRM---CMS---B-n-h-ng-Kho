# UI Guide — ERP HRM · CMS · Bán hàng Kho

> Tài liệu quy ước giao diện, bảng màu, class CSS và component dùng chung.
> Cập nhật: Phiên 3 (2026-11-04)

---

## 1. Bảng màu (CSS Variables)

Tất cả màu sắc được khai báo trên `:root` trong `enterprise-theme.css`.
Dark mode tự động đảo qua `[data-bs-theme="dark"]`.

### 1.1 Màu chính (Brand)

| Token                  | Light           | Dark            | Dùng cho                     |
|------------------------|-----------------|-----------------|------------------------------|
| `--erp-primary`        | `#2563EB`       | `#3B82F6`       | Nút chính, link, badge       |
| `--erp-primary-hover`  | `#1D4ED8`       | `#60A5FA`       | Hover nút chính              |
| `--erp-primary-subtle` | `rgba(37,99,235,0.08)` | `rgba(59,130,246,0.15)` | Nền nhẹ  |

### 1.2 Trạng thái (Semantic)

| Token              | Light     | Dark      | Dùng cho                       |
|--------------------|-----------|-----------|--------------------------------|
| `--erp-success`    | `#10B981` | `#34D399` | Thành công, tăng trưởng        |
| `--erp-danger`     | `#EF4444` | `#F87171` | Lỗi, cảnh báo nghiêm trọng    |
| `--erp-warning`    | `#F59E0B` | `#FBBF24` | Cảnh báo, chờ duyệt           |
| `--erp-info`       | `#06B6D4` | `#22D3EE` | Thông tin, gợi ý               |

### 1.3 Nền & Viền

| Token                  | Mô tả                          |
|------------------------|---------------------------------|
| `--erp-bg-body`        | Nền trang chính                 |
| `--erp-bg-card`        | Nền card / bảng                 |
| `--erp-bg-sidebar`     | Nền sidebar (gradient tối)      |
| `--erp-border-color`   | Viền mặc định                   |
| `--erp-text-primary`   | Màu chữ chính                   |
| `--erp-text-muted`     | Màu chữ phụ                     |

### 1.4 Shadow

| Token                   | Dùng cho                       |
|-------------------------|--------------------------------|
| `--erp-card-shadow`     | Shadow mặc định cho card       |
| `--erp-elevated-shadow` | Shadow nâng cao (hover-lift)   |

---

## 2. Font chữ

```css
--erp-font-main: 'Be Vietnam Pro', -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
--erp-font-mono: 'JetBrains Mono', 'Fira Code', monospace;
```

- **Be Vietnam Pro** — Font chính, hỗ trợ tiếng Việt đầy đủ.
- **JetBrains Mono** — Font monospace cho số liệu, code.
- Weight sử dụng: 300 (light), 400 (regular), 500 (medium), 600 (semibold), 700 (bold), 800 (extrabold).

---

## 3. Layout — Sidebar + Topbar

### 3.1 Cấu trúc HTML

```
<html lang="vi" [data-bs-theme] [class="sidebar-collapsed"]>
  <body>
    <aside class="erp-sidebar">         ← 260px, thu gọn 68px
    <div class="erp-main-content">
      <header class="erp-topbar">        ← sticky, 60px
      <div class="erp-content-wrapper">
        <main class="erp-page-content"> ← @RenderBody()
      </div>
      <footer class="erp-footer">
    </div>
  </body>
</html>
```

### 3.2 Sidebar toggle

- **Desktop**: Nút `#sidebarCollapseBtn` ở đáy sidebar → class `sidebar-collapsed` trên `<html>`.
- **Mobile** (≤991px): Nút hamburger `#sidebarMobileBtn` → class `show` trên sidebar + overlay.
- Trạng thái lưu `localStorage('erp_sidebar_collapsed')`.
- Sidebar collapsed: icon tooltip hiện tên menu via `data-tooltip`.

### 3.3 Active sidebar item

JS tự phát hiện URL path hiện tại, so khớp `data-path` trên `.erp-nav-item`, chọn best-match (path dài nhất khớp).

### 3.4 Dark / Light mode

- Toggle qua `window.toggleTheme()` (enterprise-theme.js).
- Lưu `localStorage('erp_theme')` + cookie `erp_theme` (cho server-side).
- Anti-FOUC: inline `<script>` trong `<head>` đọc theme trước render.
- CSS: `[data-bs-theme="dark"]` + `.dark-mode` class.

---

## 4. Shared Components

### 4.1 Stat Card (Thẻ thống kê)

```html
<div class="stat-card hover-lift">
    <div class="stat-card-body">
        <div class="stat-card-label">Doanh thu tháng</div>
        <div class="stat-card-value">
            <span class="count-up" data-target="125000000" data-suffix=" đ">0</span>
        </div>
        <div class="stat-card-trend up">
            <i class="bi bi-arrow-up-right me-1"></i>12% vs tháng trước
        </div>
    </div>
    <div class="stat-card-icon primary">
        <i class="bi bi-currency-dollar"></i>
    </div>
</div>
```

**Thuộc tính `count-up`:**

| Attribute       | Mô tả                          | Mặc định |
|-----------------|---------------------------------|----------|
| `data-target`   | Giá trị đích (số)              | bắt buộc |
| `data-prefix`   | Tiền tố (VD: "$")              | `""`     |
| `data-suffix`   | Hậu tố (VD: " đ")             | `""`     |
| `data-decimals` | Số chữ số thập phân            | `0`      |
| `data-duration` | Thời gian animation (ms)       | `1200`   |

**Biến thể icon:** `primary`, `success`, `warning`, `danger`, `info`, `purple`

**Trend:** `.stat-card-trend.up` (xanh) / `.stat-card-trend.down` (đỏ)

### 4.2 Data Table

```html
<div class="erp-table-wrapper">
    <div class="erp-table-toolbar">
        <span class="erp-table-toolbar-title">Tiêu đề</span>
        <div><!-- Nút hành động --></div>
    </div>
    <div class="table-responsive">
        <table class="table table-hover align-middle mb-0">
            <thead><tr><th>...</th></tr></thead>
            <tbody>...</tbody>
        </table>
    </div>
</div>
```

### 4.3 Empty State

```html
<div class="erp-empty-state">
    <div class="erp-empty-state-icon"><i class="bi bi-inbox"></i></div>
    <div class="erp-empty-state-title">Chưa có dữ liệu</div>
    <div class="erp-empty-state-desc">Mô tả ngắn gọn</div>
</div>
```

### 4.4 Floating Label Form

```html
<div class="erp-floating-group">
    <input type="text" class="erp-floating-input" id="ten" placeholder=" " required />
    <label for="ten" class="erp-floating-label">Họ và tên</label>
</div>
```

Label tự nổi lên khi input focus hoặc có giá trị (`:not(:placeholder-shown)`).

### 4.5 Button Loading

```html
<button class="btn btn-primary ripple" id="saveBtn">
    <i class="bi bi-check-lg me-1"></i>Lưu
</button>
```

```javascript
// Bật loading
ERP.btnLoading(document.getElementById('saveBtn'));
// Tắt loading
ERP.btnReset(document.getElementById('saveBtn'));
```

### 4.6 Confirm Modal

```javascript
ERP.confirm({
    title: 'Xóa bản ghi?',
    message: 'Hành động này không thể hoàn tác.',
    confirmText: 'Xóa',
    cancelText: 'Hủy',
    variant: 'danger'   // 'danger' | 'warning'
}).then(function(ok) {
    if (ok) { /* thực hiện xóa */ }
});
```

### 4.7 Toast (Thông báo nổi)

**Server-side** (TempData):
```csharp
TempData["SuccessMessage"] = "Lưu thành công!";
TempData["ErrorMessage"] = "Có lỗi xảy ra.";
```

**Client-side** (JavaScript):
```javascript
ERP.toast('Đã lưu thành công!', 'success');   // success | danger | warning | info
```

Toast tự biến mất sau 5 giây.

### 4.8 Badges

```html
<span class="erp-badge erp-badge-success">Hoạt động</span>
<span class="erp-badge erp-badge-danger erp-badge-dot">Nghỉ việc</span>
```

Biến thể: `erp-badge-primary`, `erp-badge-success`, `erp-badge-danger`, `erp-badge-warning`, `erp-badge-info`, `erp-badge-neutral`

Thêm `erp-badge-dot` để có dấu chấm tròn phía trước.

### 4.9 Skeleton Loading

```html
<div class="skeleton skeleton-card"></div>
<div class="skeleton skeleton-line" style="width:80%"></div>
<div class="skeleton skeleton-line" style="width:60%"></div>
<div class="skeleton skeleton-circle"></div>
```

### 4.10 Tabs

```html
<div class="erp-tabs">
    <button class="erp-tab active">Tab 1</button>
    <button class="erp-tab">Tab 2</button>
</div>
```

---

## 5. Animation Classes

Tất cả animation tôn trọng `prefers-reduced-motion: reduce`.

### 5.1 Entrance

| Class          | Hiệu ứng                       |
|----------------|---------------------------------|
| `.fade-in`     | Mờ → rõ (0.4s)                 |
| `.slide-up`    | Trượt lên + mờ → rõ (0.5s)     |
| `.slide-up-sm` | Trượt lên nhẹ (0.35s)           |

### 5.2 Stagger (Hiệu ứng so le)

Thêm class `stagger-list` vào container. Các phần tử con sẽ xuất hiện lần lượt (delay 60ms mỗi item, tối đa 8 item).

```html
<div class="row stagger-list">
    <div class="col-md-3"><!-- card 1, delay 0ms --></div>
    <div class="col-md-3"><!-- card 2, delay 60ms --></div>
    <div class="col-md-3"><!-- card 3, delay 120ms --></div>
    <div class="col-md-3"><!-- card 4, delay 180ms --></div>
</div>
```

### 5.3 Hover

| Class          | Hiệu ứng                        |
|----------------|----------------------------------|
| `.hover-lift`  | Nâng lên 3px + shadow khi hover  |
| `.hover-grow`  | Phóng to 1.02x khi hover         |

### 5.4 Ripple (Hiệu ứng sóng nhấn)

Thêm class `ripple` vào button. Các `.btn-primary`, `.btn-success`, `.btn-danger`, `.btn-warning` tự động có ripple.

### 5.5 Scroll Reveal

```html
<div class="reveal">Xuất hiện khi cuộn tới</div>
<div class="reveal-left">Trượt từ trái</div>
<div class="reveal-right">Trượt từ phải</div>
<div class="reveal-scale">Phóng to</div>
```

JS (IntersectionObserver) sẽ thêm `.revealed` khi phần tử vào viewport.

### 5.6 Chart Container

```html
<div class="chart-container">
    <canvas id="myChart"></canvas>
</div>
```

Tự động fade-in khi tải.

### 5.7 Utility

| Class       | Hiệu ứng                  |
|-------------|----------------------------|
| `.pulse`    | Nhấp nháy liên tục         |
| `.spin`     | Xoay 360° liên tục         |

---

## 6. JavaScript API — `window.ERP`

| Method                     | Mô tả                                       |
|----------------------------|----------------------------------------------|
| `ERP.btnLoading(btn)`      | Bật loading cho button                       |
| `ERP.btnReset(btn)`        | Tắt loading cho button                       |
| `ERP.confirm({...})`       | Hiện modal xác nhận, trả về Promise<boolean> |
| `ERP.toast(msg, variant)`  | Hiện toast thông báo                         |

Các hàm global từ `enterprise-theme.js`:

| Method                     | Mô tả                                       |
|----------------------------|----------------------------------------------|
| `window.toggleTheme()`     | Đổi dark/light mode                         |
| `window.openCommandPalette()` | Mở command palette (Ctrl+K)              |

---

## 7. File Structure

```
wwwroot/
├── css/
│   ├── enterprise-theme.css    ← Biến CSS, base styles, dark mode
│   ├── admin-components.css    ← Sidebar, topbar, stat cards, forms, tables, badges
│   ├── admin-animations.css    ← Keyframes, stagger, hover, ripple, scroll reveal
│   └── site.css                ← Legacy styles
├── js/
│   ├── security.js             ← XSS escape (HRM.escapeHtml)
│   ├── enterprise-theme.js     ← Theme toggle, command palette
│   ├── enterprise-notifications.js ← SignalR notifications
│   ├── admin-ui.js             ← Sidebar, ripple, count-up, scroll reveal, toast, confirm
│   └── site.js                 ← Legacy scripts
Views/Shared/
├── _Layout.cshtml              ← Layout chính: sidebar + topbar (Dashboard, mặc định)
├── _HRMLayout.cshtml           ← Layout HR (có sidebar riêng)
├── _AdminLayout.cshtml         ← Layout CMS/Permissions
├── _SalesLayout.cshtml         ← Layout bán hàng
├── _WarehouseLayout.cshtml     ← Layout kho
└── _PublicLayout.cshtml        ← Layout trang công khai
```

---

## 8. Responsive Breakpoints

| Breakpoint    | Hành vi                                          |
|---------------|--------------------------------------------------|
| ≥1200px (xl)  | Sidebar mở rộng 260px, 6 cột stat cards         |
| ≥992px (lg)   | Sidebar hiển thị, có thể thu gọn                |
| ≤991.98px     | Sidebar ẩn, mở qua hamburger + overlay           |
| ≤575.98px     | Topbar compact, stat card full-width              |

---

## 9. Quy ước đặt tên

- **CSS class prefix**: `erp-` cho component (VD: `erp-sidebar`, `erp-toast-alert`)
- **CSS variable prefix**: `--erp-` cho tất cả custom properties
- **JS namespace**: `window.ERP` cho API mới, `window.HRM` cho security utils cũ
- **HTML data attributes**: `data-path`, `data-target`, `data-tooltip`, `data-bs-theme`
- **BEM-lite**: `stat-card-body`, `stat-card-icon`, `stat-card-trend`

---

## 10. Accessibility

- **Focus visible**: Tất cả interactive elements có focus ring (`outline: 2px solid var(--erp-primary)`)
- **prefers-reduced-motion**: Tất cả animation/transition bị tắt khi người dùng bật giảm chuyển động
- **aria-label**: Nút mobile toggle, breadcrumb navigation
- **Semantic HTML**: `<nav>`, `<main>`, `<header>`, `<footer>`, `<aside>`
- **Color contrast**: Đảm bảo tỷ lệ tương phản ≥4.5:1 cho text thường
