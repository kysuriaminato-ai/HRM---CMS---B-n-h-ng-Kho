/**
 * Titan Cobalt & Nordic Clean - Enterprise Theme & Command Palette Script (Giai đoạn A)
 * Hỗ trợ: Circular Reveal Dark Mode (View Transition API), Module Switcher, Command Palette (Ctrl+K)
 */
(function () {
    'use strict';

    function prefersReducedMotion() {
        return window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    }

    // ----------------------------------------------------
    // 1. QUẢN LÝ THEME DARK / LIGHT MODE & CIRCULAR REVEAL
    // ----------------------------------------------------
    function getThemeCookie() {
        var match = document.cookie.match(/(^| )erp_theme=([^;]+)/);
        return match ? decodeURIComponent(match[2]) : null;
    }

    function initTheme() {
        var savedTheme = localStorage.getItem('erp_theme') || getThemeCookie();
        if (!savedTheme) {
            savedTheme = (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) ? 'dark' : 'light';
        }
        applyTheme(savedTheme);
    }

    function applyTheme(theme) {
        var root = document.documentElement;
        root.setAttribute('data-bs-theme', theme);
        if (theme === 'dark') {
            root.classList.add('dark-mode');
            if (document.body) document.body.classList.add('dark-mode');
        } else {
            root.classList.remove('dark-mode');
            if (document.body) document.body.classList.remove('dark-mode');
        }
        localStorage.setItem('erp_theme', theme);
        document.cookie = 'erp_theme=' + encodeURIComponent(theme) + '; path=/; max-age=31536000; SameSite=Lax';
        updateThemeToggleIcons(theme);
        window.dispatchEvent(new CustomEvent('erp:themechange', { detail: { theme: theme } }));
    }

    function updateThemeToggleIcons(theme) {
        var icons = document.querySelectorAll('.theme-toggle-icon');
        icons.forEach(function (icon) {
            if (theme === 'dark') {
                icon.className = 'bi bi-sun-fill theme-toggle-icon text-warning fs-5';
            } else {
                icon.className = 'bi bi-moon-stars-fill theme-toggle-icon text-secondary fs-5';
            }
        });
    }

    /**
     * Đổi chế độ Sáng / Tối kèm hiệu ứng Circular Reveal từ vị trí nút bấm.
     * Tự động chuyển thẳng nếu bật prefers-reduced-motion hoặc trình duyệt không hỗ trợ View Transition.
     */
    window.toggleTheme = function (evt) {
        var currentTheme = document.documentElement.getAttribute('data-bs-theme') || 'light';
        var newTheme = currentTheme === 'dark' ? 'light' : 'dark';

        if (!document.startViewTransition || prefersReducedMotion()) {
            applyTheme(newTheme);
            return;
        }

        // Xác định tâm vòng tròn lan tỏa (từ sự kiện click hoặc nút toggle trên topbar)
        var x = window.innerWidth - 120;
        var y = 30;
        if (evt && typeof evt.clientX === 'number' && (evt.clientX !== 0 || evt.clientY !== 0)) {
            x = evt.clientX;
            y = evt.clientY;
        } else {
            var triggerBtn = document.getElementById('themeToggleBtn') || document.querySelector('.theme-toggle-icon');
            if (triggerBtn) {
                var rect = triggerBtn.getBoundingClientRect();
                x = rect.left + rect.width / 2;
                y = rect.top + rect.height / 2;
            }
        }

        var endRadius = Math.hypot(
            Math.max(x, window.innerWidth - x),
            Math.max(y, window.innerHeight - y)
        );

        var root = document.documentElement;
        root.classList.add('erp-theme-reveal');

        var transition = document.startViewTransition(function () {
            applyTheme(newTheme);
        });

        transition.ready.then(function () {
            root.animate(
                {
                    clipPath: [
                        'circle(0px at ' + x + 'px ' + y + 'px)',
                        'circle(' + endRadius + 'px at ' + x + 'px ' + y + 'px)'
                    ]
                },
                {
                    duration: 420,
                    easing: 'cubic-bezier(0.16, 1, 0.3, 1)',
                    pseudoElement: '::view-transition-new(root)'
                }
            );
        }).catch(function () { /* Ignore aborted view transition */ });

        transition.finished.finally(function () {
            root.classList.remove('erp-theme-reveal');
        });
    };

    // ----------------------------------------------------
    // 1.5. QUẢN LÝ DENSITY SWITCHER (COMFORTABLE / COMPACT)
    // ----------------------------------------------------
    function getDensityCookie() {
        var match = document.cookie.match(/(^| )erp_density=([^;]+)/);
        return match ? decodeURIComponent(match[2]) : null;
    }

    function initDensity() {
        var savedDensity = localStorage.getItem('erp_density') || getDensityCookie() || 'comfortable';
        applyDensity(savedDensity);
    }

    function applyDensity(density) {
        var root = document.documentElement;
        var validDensity = (density === 'compact') ? 'compact' : 'comfortable';
        root.setAttribute('data-density', validDensity);
        if (validDensity === 'compact') {
            root.classList.add('density-compact');
            if (document.body) document.body.classList.add('density-compact');
        } else {
            root.classList.remove('density-compact');
            if (document.body) document.body.classList.remove('density-compact');
        }
        localStorage.setItem('erp_density', validDensity);
        document.cookie = 'erp_density=' + encodeURIComponent(validDensity) + '; path=/; max-age=31536000; SameSite=Lax';
        updateDensityToggleIcons(validDensity);
        window.dispatchEvent(new CustomEvent('erp:densitychange', { detail: { density: validDensity } }));
    }

    function updateDensityToggleIcons(density) {
        var btns = document.querySelectorAll('#densityToggleBtn, .density-toggle-btn');
        btns.forEach(function (btn) {
            var icon = btn.querySelector('.density-toggle-icon') || btn;
            if (density === 'compact') {
                if (icon.tagName === 'I') icon.className = 'bi bi-arrows-collapse density-toggle-icon text-primary fs-5';
                btn.setAttribute('title', 'Độ giãn cách: Thu gọn (Bấm để chuyển Thoải mái)');
                btn.setAttribute('aria-label', 'Chuyển sang chế độ Thoải mái');
            } else {
                if (icon.tagName === 'I') icon.className = 'bi bi-arrows-expand density-toggle-icon text-secondary fs-5';
                btn.setAttribute('title', 'Độ giãn cách: Thoải mái (Bấm để chuyển Thu gọn)');
                btn.setAttribute('aria-label', 'Chuyển sang chế độ Thu gọn');
            }
        });
    }

    window.toggleDensity = function () {
        var current = document.documentElement.getAttribute('data-density') || 'comfortable';
        var next = (current === 'compact') ? 'comfortable' : 'compact';
        applyDensity(next);
    };

    window.setDensity = function (density) {
        applyDensity(density);
    };

    window.setErpModule = function (moduleName) {
        if (!moduleName) return;
        document.documentElement.setAttribute('data-module', moduleName);
        window.dispatchEvent(new CustomEvent('erp:modulechange', { detail: { module: moduleName } }));
    };

    // ----------------------------------------------------
    // 2. QUICK COMMAND PALETTE (CTRL + K / CMD + K)
    // ----------------------------------------------------
    var searchDebounceTimeout = null;
    var selectedItemIndex = 0;
    var currentResultItems = [];

    function injectCommandPaletteModal() {
        if (document.getElementById('commandPaletteModal') || !document.body) return;

        var modalHtml =
            '<div class="modal fade command-palette-modal" id="commandPaletteModal" tabindex="-1" aria-hidden="true">' +
                '<div class="modal-dialog modal-dialog-centered modal-lg">' +
                    '<div class="modal-content shadow-lg border-0">' +
                        '<div class="p-3 border-bottom d-flex align-items-center gap-3 bg-body">' +
                            '<i class="bi bi-search text-primary fs-5"></i>' +
                            '<input type="text" id="commandPaletteInput" class="form-control command-palette-input flex-grow-1" ' +
                                   'placeholder="Gõ tên nhân sự, sản phẩm, mã đơn hoặc chức năng... (↑↓ để chọn, Enter để mở)" autocomplete="off" />' +
                            '<span class="badge bg-light text-secondary border px-2 py-1"><kbd>ESC</kbd> Đóng</span>' +
                        '</div>' +
                        '<div class="command-palette-results py-2" id="commandPaletteResults">' +
                            '<div class="text-center py-4 text-muted small">' +
                                '<i class="bi bi-lightning-charge fs-3 text-warning d-block mb-2"></i>' +
                                'Gõ từ khóa để tra cứu tức thì toàn bộ hệ thống' +
                            '</div>' +
                        '</div>' +
                        '<div class="p-2 bg-body-tertiary border-top d-flex justify-content-between align-items-center px-3 small text-muted">' +
                            '<div><kbd>↑</kbd> <kbd>↓</kbd> Di chuyển • <kbd>Enter</kbd> Chọn • <kbd>ESC</kbd> Đóng</div>' +
                            '<span class="fw-semibold text-primary">ERP SPOTLIGHT</span>' +
                        '</div>' +
                    '</div>' +
                '</div>' +
            '</div>';

        document.body.insertAdjacentHTML('beforeend', modalHtml);

        var input = document.getElementById('commandPaletteInput');
        input.addEventListener('input', function (e) {
            clearTimeout(searchDebounceTimeout);
            var val = e.target.value;
            searchDebounceTimeout = setTimeout(function () {
                executeQuickSearch(val);
            }, 200);
        });

        input.addEventListener('keydown', handlePaletteKeydown);
    }

    window.openCommandPalette = function () {
        injectCommandPaletteModal();
        var modalEl = document.getElementById('commandPaletteModal');
        if (!modalEl || typeof bootstrap === 'undefined') return;
        var bsModal = bootstrap.Modal.getOrCreateInstance(modalEl);
        bsModal.show();
        setTimeout(function () {
            var input = document.getElementById('commandPaletteInput');
            if (input) {
                input.focus();
                input.select();
                executeQuickSearch(input.value);
            }
        }, 150);
    };

    function executeQuickSearch(keyword) {
        var container = document.getElementById('commandPaletteResults');
        if (!container) return;

        fetch('/Home/GlobalQuickSearch?keyword=' + encodeURIComponent(keyword || ''))
            .then(function (res) { return res.json(); })
            .then(function (data) { renderSearchResults(data); })
            .catch(function () { /* Ignore network/auth search errors */ });
    }

    function buildGroupHtml(title, icon, items, badgeLabel, badgeVariant) {
        if (!items || items.length === 0) return '';
        var html = '<div class="command-palette-group-title"><i class="bi ' + icon + ' me-1"></i> ' + title + '</div>';
        items.forEach(function (item) {
            var idx = currentResultItems.length;
            currentResultItems.push(item);
            var safeHref = HRM.escapeHtml(HRM.safeUrl(item.url));
            var rightPart = badgeLabel
                ? '<span class="erp-badge erp-badge-' + badgeVariant + ' font-monospace">' + badgeLabel + '</span>'
                : '<i class="bi bi-arrow-return-left text-muted small"></i>';
            html +=
                '<div class="command-palette-item ' + (idx === 0 ? 'active' : '') + '" data-index="' + idx + '" data-url="' + safeHref + '">' +
                    '<div class="d-flex align-items-center gap-3">' +
                        '<i class="bi ' + HRM.escapeHtml(item.icon || 'bi-link-45deg') + ' fs-5 text-primary"></i>' +
                        '<div>' +
                            '<div class="fw-semibold small">' + HRM.escapeHtml(item.title) + '</div>' +
                            '<div class="text-muted small">' + HRM.escapeHtml(item.subtitle) + '</div>' +
                        '</div>' +
                    '</div>' +
                    rightPart +
                '</div>';
        });
        return html;
    }

    function renderSearchResults(data) {
        var container = document.getElementById('commandPaletteResults');
        if (!container) return;

        currentResultItems = [];
        var html = '';
        html += buildGroupHtml('Lối tắt nhanh', 'bi-compass', data.navigation, '', '');
        html += buildGroupHtml('Hồ sơ Nhân sự', 'bi-people', data.employees, 'HRM', 'info');
        html += buildGroupHtml('Hàng hóa & Tồn kho', 'bi-box-seam', data.products, 'KHO', 'success');
        html += buildGroupHtml('Đơn hàng & Hóa đơn', 'bi-receipt', data.orders, 'SALES', 'warning');
        html += buildGroupHtml('Đơn Mua Hàng & NCC', 'bi-truck', data.purchaseOrders, 'PO', 'danger');
        html += buildGroupHtml('Đơn từ & Nghỉ phép', 'bi-file-earmark-check', data.leaveRequests, 'LEAVE', 'primary');

        if (currentResultItems.length === 0) {
            html = '<div class="text-center py-5 text-muted small"><i class="bi bi-inbox fs-3 d-block mb-2"></i>Không tìm thấy kết quả phù hợp</div>';
        }

        container.innerHTML = html;
        container.querySelectorAll('.command-palette-item[data-url]').forEach(function (el) {
            el.addEventListener('click', function () {
                var targetUrl = el.getAttribute('data-url');
                if (targetUrl) window.location.href = targetUrl;
            });
        });
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
                window.location.href = HRM.safeUrl(currentResultItems[selectedItemIndex].url);
            }
        }
    }

    function highlightSelectedItem() {
        var items = document.querySelectorAll('.command-palette-item');
        items.forEach(function (el, idx) {
            if (idx === selectedItemIndex) {
                el.classList.add('active');
                el.scrollIntoView({ block: 'nearest' });
            } else {
                el.classList.remove('active');
            }
        });
    }

    document.addEventListener('keydown', function (e) {
        if ((e.ctrlKey || e.metaKey) && e.key && e.key.toLowerCase() === 'k') {
            e.preventDefault();
            window.openCommandPalette();
        }
    });

    document.addEventListener('DOMContentLoaded', function () {
        initTheme();
        initDensity();
        injectCommandPaletteModal();
    });
})();
