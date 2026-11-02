/**
 * Titan Cobalt & Nordic Clean - Enterprise Theme & Command Palette Script
 * ERP_HRM_CMS Platform
 */

(function () {
    // ----------------------------------------------------
    // 1. QUẢN LÝ THEME DARK / LIGHT MODE
    // ----------------------------------------------------
    function getThemeCookie() {
        const match = document.cookie.match(new RegExp('(^| )erp_theme=([^;]+)'));
        return match ? match[2] : null;
    }

    function initTheme() {
        const savedTheme = localStorage.getItem('erp_theme') || getThemeCookie() || 'light';
        applyTheme(savedTheme);
    }

    function applyTheme(theme) {
        document.documentElement.setAttribute('data-bs-theme', theme);
        if (theme === 'dark') {
            document.body.classList.add('dark-mode');
            document.documentElement.classList.add('dark-mode');
        } else {
            document.body.classList.remove('dark-mode');
            document.documentElement.classList.remove('dark-mode');
        }
        localStorage.setItem('erp_theme', theme);
        document.cookie = "erp_theme=" + encodeURIComponent(theme) + "; path=/; max-age=31536000; SameSite=Lax";
        updateThemeToggleIcons(theme);
    }

    function updateThemeToggleIcons(theme) {
        const icons = document.querySelectorAll('.theme-toggle-icon');
        icons.forEach(icon => {
            if (theme === 'dark') {
                icon.className = 'bi bi-sun-fill theme-toggle-icon text-warning fs-5';
            } else {
                icon.className = 'bi bi-moon-stars-fill theme-toggle-icon text-secondary fs-5';
            }
        });
    }

    window.toggleTheme = function () {
        const currentTheme = document.documentElement.getAttribute('data-bs-theme') || 'light';
        const newTheme = currentTheme === 'dark' ? 'light' : 'dark';
        applyTheme(newTheme);
    };

    // Tự động nhận diện và làm nổi bật mục Menu / Sidebar đang hoạt động
    function highlightActiveSidebar() {
        const currentPath = window.location.pathname.toLowerCase();
        const navLinks = document.querySelectorAll('.erp-sidebar .erp-nav-item, .erp-sidebar a.nav-link, header .navbar-nav a.nav-link');
        navLinks.forEach(link => {
            const href = link.getAttribute('href');
            if (href && href !== '#' && !href.startsWith('javascript:')) {
                const linkPath = href.toLowerCase().split('?')[0];
                if (currentPath === linkPath || (linkPath.length > 1 && currentPath.startsWith(linkPath) && linkPath !== '/')) {
                    link.classList.add('active');
                }
            }
        });
    }

    // ----------------------------------------------------
    // 2. QUICK COMMAND PALETTE (CTRL + K / CMD + K)
    // ----------------------------------------------------
    let searchDebounceTimeout = null;
    let selectedItemIndex = 0;
    let currentResultItems = [];

    function injectCommandPaletteModal() {
        if (document.getElementById('commandPaletteModal')) return;

        const modalHtml = `
        <div class="modal fade command-palette-modal" id="commandPaletteModal" tabindex="-1" aria-hidden="true">
            <div class="modal-dialog modal-dialog-centered modal-lg">
                <div class="modal-content shadow-lg border-0">
                    <div class="p-3 border-bottom d-flex align-items-center gap-3 bg-body">
                        <i class="bi bi-search text-primary fs-5"></i>
                        <input type="text" id="commandPaletteInput" class="form-control command-palette-input flex-grow-1" 
                               placeholder="Gõ tên nhân sự, sản phẩm, mã đơn hoặc chức năng... (↑↓ để chọn, Enter để mở)" autocomplete="off" />
                        <span class="badge bg-light text-secondary border px-2 py-1"><kbd>ESC</kbd> Đóng</span>
                    </div>
                    <div class="command-palette-results py-2" id="commandPaletteResults">
                        <div class="text-center py-4 text-muted small">
                            <i class="bi bi-lightning-charge fs-3 text-warning d-block mb-2"></i>
                            Gõ từ khóa để tra cứu tức thì toàn bộ hệ thống
                        </div>
                    </div>
                    <div class="p-2 bg-body-tertiary border-top d-flex justify-content-between align-items-center px-3 small text-muted">
                        <div>
                            <kbd>↑</kbd> <kbd>↓</kbd> Di chuyển • <kbd>Enter</kbd> Chọn • <kbd>ESC</kbd> Đóng
                        </div>
                        <span class="fw-semibold text-primary" style="font-size: 11px;">TIEN PHONG SPOTLIGHT 2026</span>
                    </div>
                </div>
            </div>
        </div>`;

        document.body.insertAdjacentHTML('beforeend', modalHtml);

        const input = document.getElementById('commandPaletteInput');
        input.addEventListener('input', function (e) {
            clearTimeout(searchDebounceTimeout);
            const val = e.target.value;
            searchDebounceTimeout = setTimeout(() => {
                executeQuickSearch(val);
            }, 200);
        });

        input.addEventListener('keydown', handlePaletteKeydown);
    }

    window.openCommandPalette = function () {
        injectCommandPaletteModal();
        const modalEl = document.getElementById('commandPaletteModal');
        const bsModal = bootstrap.Modal.getOrCreateInstance(modalEl);
        bsModal.show();
        setTimeout(() => {
            const input = document.getElementById('commandPaletteInput');
            if (input) {
                input.focus();
                input.select();
                executeQuickSearch(input.value);
            }
        }, 150);
    };

    function executeQuickSearch(keyword) {
        const container = document.getElementById('commandPaletteResults');
        if (!container) return;

        fetch('/Home/GlobalQuickSearch?keyword=' + encodeURIComponent(keyword || ''))
            .then(res => res.json())
            .then(data => {
                renderSearchResults(data);
            })
            .catch(err => {
                console.debug('Search error', err);
            });
    }

    function renderSearchResults(data) {
        const container = document.getElementById('commandPaletteResults');
        if (!container) return;

        currentResultItems = [];
        let html = '';

        // 1. Navigation items
        if (data.navigation && data.navigation.length > 0) {
            html += `<div class="command-palette-group-title"><i class="bi bi-compass me-1"></i> Lối tắt nhanh</div>`;
            data.navigation.forEach(item => {
                const idx = currentResultItems.length;
                currentResultItems.push(item);
                html += `
                    <div class="command-palette-item ${idx === 0 ? 'active' : ''}" data-index="${idx}" onclick="window.location.href='${HRM.escapeHtml(HRM.safeUrl(item.url))}'">
                        <div class="d-flex align-items-center gap-3">
                            <i class="bi ${HRM.escapeHtml(item.icon || 'bi-link-45deg')} fs-5 text-primary"></i>
                            <div>
                                <div class="fw-semibold small">${HRM.escapeHtml(item.title)}</div>
                                <div class="text-muted" style="font-size: 11px;">${HRM.escapeHtml(item.subtitle)}</div>
                            </div>
                        </div>
                        <i class="bi bi-arrow-return-left text-muted small"></i>
                    </div>`;
            });
        }

        // 2. Employees
        if (data.employees && data.employees.length > 0) {
            html += `<div class="command-palette-group-title"><i class="bi bi-people me-1"></i> Hồ sơ Nhân sự</div>`;
            data.employees.forEach(item => {
                const idx = currentResultItems.length;
                currentResultItems.push(item);
                html += `
                    <div class="command-palette-item ${idx === 0 ? 'active' : ''}" data-index="${idx}" onclick="window.location.href='${HRM.escapeHtml(HRM.safeUrl(item.url))}'">
                        <div class="d-flex align-items-center gap-3">
                            <i class="bi ${HRM.escapeHtml(item.icon)} fs-5 text-info"></i>
                            <div>
                                <div class="fw-semibold small">${HRM.escapeHtml(item.title)}</div>
                                <div class="text-muted" style="font-size: 11px;">${HRM.escapeHtml(item.subtitle)}</div>
                            </div>
                        </div>
                        <span class="badge bg-info-subtle text-info border font-monospace" style="font-size: 10px;">HRM</span>
                    </div>`;
            });
        }

        // 3. Products
        if (data.products && data.products.length > 0) {
            html += `<div class="command-palette-group-title"><i class="bi bi-box-seam me-1"></i> Hàng hóa & Tồn kho</div>`;
            data.products.forEach(item => {
                const idx = currentResultItems.length;
                currentResultItems.push(item);
                html += `
                    <div class="command-palette-item ${idx === 0 ? 'active' : ''}" data-index="${idx}" onclick="window.location.href='${HRM.escapeHtml(HRM.safeUrl(item.url))}'">
                        <div class="d-flex align-items-center gap-3">
                            <i class="bi ${HRM.escapeHtml(item.icon)} fs-5 text-success"></i>
                            <div>
                                <div class="fw-semibold small">${HRM.escapeHtml(item.title)}</div>
                                <div class="text-muted" style="font-size: 11px;">${HRM.escapeHtml(item.subtitle)}</div>
                            </div>
                        </div>
                        <span class="badge bg-success-subtle text-success border font-monospace" style="font-size: 10px;">KHO</span>
                    </div>`;
            });
        }

        // 4. Orders
        if (data.orders && data.orders.length > 0) {
            html += `<div class="command-palette-group-title"><i class="bi bi-receipt me-1"></i> Đơn hàng & Hóa đơn</div>`;
            data.orders.forEach(item => {
                const idx = currentResultItems.length;
                currentResultItems.push(item);
                html += `
                    <div class="command-palette-item ${idx === 0 ? 'active' : ''}" data-index="${idx}" onclick="window.location.href='${HRM.escapeHtml(HRM.safeUrl(item.url))}'">
                        <div class="d-flex align-items-center gap-3">
                            <i class="bi ${HRM.escapeHtml(item.icon)} fs-5 text-warning"></i>
                            <div>
                                <div class="fw-semibold small">${HRM.escapeHtml(item.title)}</div>
                                <div class="text-muted" style="font-size: 11px;">${HRM.escapeHtml(item.subtitle)}</div>
                            </div>
                        </div>
                        <span class="badge bg-warning-subtle text-warning border font-monospace" style="font-size: 10px;">ORDER</span>
                    </div>`;
            });
        }

        // 5. Purchase Orders (PO) & SCM
        if (data.purchaseOrders && data.purchaseOrders.length > 0) {
            html += `<div class="command-palette-group-title"><i class="bi bi-truck me-1"></i> Đơn Mua Hàng & Nhà Cung Cấp</div>`;
            data.purchaseOrders.forEach(item => {
                const idx = currentResultItems.length;
                currentResultItems.push(item);
                html += `
                    <div class="command-palette-item ${idx === 0 ? 'active' : ''}" data-index="${idx}" onclick="window.location.href='${HRM.escapeHtml(HRM.safeUrl(item.url))}'">
                        <div class="d-flex align-items-center gap-3">
                            <i class="bi ${HRM.escapeHtml(item.icon)} fs-5 text-danger"></i>
                            <div>
                                <div class="fw-semibold small">${HRM.escapeHtml(item.title)}</div>
                                <div class="text-muted" style="font-size: 11px;">${HRM.escapeHtml(item.subtitle)}</div>
                            </div>
                        </div>
                        <span class="badge bg-danger-subtle text-danger border font-monospace" style="font-size: 10px;">PO</span>
                    </div>`;
            });
        }

        if (currentResultItems.length === 0) {
            html = `<div class="text-center py-5 text-muted small"><i class="bi bi-inbox fs-3 d-block mb-2"></i>Không tìm thấy kết quả phù hợp</div>`;
        }

        container.innerHTML = html;
        selectedItemIndex = 0;
    }

    function handlePaletteKeydown(e) {
        if (currentResultItems.length === 0) return;

        if (e.key === 'ArrowDown') {
            e.preventDefault();
            selectedItemIndex = (selectedItemIndex + 1) % currentResultItems.length;
            highlightSelectedItem();
        } else if (e.key === 'ArrowUp') {
            e.preventDefault();
            selectedItemIndex = (selectedItemIndex - 1 + currentResultItems.length) % currentResultItems.length;
            highlightSelectedItem();
        } else if (e.key === 'Enter') {
            e.preventDefault();
            if (currentResultItems[selectedItemIndex]) {
                window.location.href = currentResultItems[selectedItemIndex].url;
            }
        }
    }

    function highlightSelectedItem() {
        const items = document.querySelectorAll('.command-palette-item');
        items.forEach((el, idx) => {
            if (idx === selectedItemIndex) {
                el.classList.add('active');
                el.scrollIntoView({ block: 'nearest' });
            } else {
                el.classList.remove('active');
            }
        });
    }

    // Lắng nghe phím tắt Ctrl + K hoặc Cmd + K trên toàn trang
    document.addEventListener('keydown', function (e) {
        if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k') {
            e.preventDefault();
            window.openCommandPalette();
        }
    });

    // Khởi tạo khi load
    document.addEventListener('DOMContentLoaded', function () {
        initTheme();
        injectCommandPaletteModal();
        highlightActiveSidebar();
    });
})();
