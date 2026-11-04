/**
 * Admin UI - Sidebar, Ripple, Count-Up, Scroll Reveal, Toast
 * ERP HRM - CMS - Bán hàng Kho (Phiên 3)
 */
(function () {
    'use strict';

    // ──────────────────────────────────────────────────────────
    // 1. SIDEBAR TOGGLE (thu gọn / mở rộng)
    // ──────────────────────────────────────────────────────────
    function initSidebar() {
        var collapseBtn = document.getElementById('sidebarCollapseBtn');
        var mobileBtn = document.getElementById('sidebarMobileBtn');
        var overlay = document.getElementById('sidebarOverlay');
        var sidebar = document.getElementById('erpSidebar');

        if (collapseBtn) {
            collapseBtn.addEventListener('click', function () {
                document.documentElement.classList.toggle('sidebar-collapsed');
                var collapsed = document.documentElement.classList.contains('sidebar-collapsed');
                localStorage.setItem('erp_sidebar_collapsed', collapsed);
            });
        }

        // Mobile toggle
        if (mobileBtn && sidebar) {
            mobileBtn.addEventListener('click', function () {
                sidebar.classList.toggle('show');
            });
        }

        if (overlay && sidebar) {
            overlay.addEventListener('click', function () {
                sidebar.classList.remove('show');
            });
        }

        // Đặt tooltip cho sidebar collapsed
        var navItems = document.querySelectorAll('.erp-nav-item[data-path]');
        navItems.forEach(function (item) {
            var label = item.querySelector('.nav-label');
            if (label) {
                item.setAttribute('data-tooltip', label.textContent.trim());
            }
        });

        // Active sidebar item dựa trên URL
        highlightActiveSidebarItem();
    }

    function highlightActiveSidebarItem() {
        var path = window.location.pathname.toLowerCase();
        var navItems = document.querySelectorAll('.erp-nav-item[data-path]');
        var bestMatch = null;
        var bestLength = 0;

        navItems.forEach(function (item) {
            var itemPath = (item.getAttribute('data-path') || '').toLowerCase();
            if (itemPath && path.indexOf(itemPath) === 0 && itemPath.length > bestLength) {
                bestMatch = item;
                bestLength = itemPath.length;
            }
        });

        // Dashboard đặc biệt: chỉ khớp khi path = "/"
        if (path === '/' || path === '') {
            navItems.forEach(function (item) {
                if (item.getAttribute('data-path') === '/') {
                    bestMatch = item;
                }
            });
        }

        if (bestMatch) {
            bestMatch.classList.add('active');
        }
    }

    // ──────────────────────────────────────────────────────────
    // 2. RIPPLE EFFECT
    // ──────────────────────────────────────────────────────────
    function initRipple() {
        document.addEventListener('click', function (e) {
            var btn = e.target.closest('.ripple, .btn-primary, .btn-success, .btn-danger, .btn-warning');
            if (!btn) return;

            // Kiểm tra prefers-reduced-motion
            if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;

            var rect = btn.getBoundingClientRect();
            var size = Math.max(rect.width, rect.height);
            var x = e.clientX - rect.left - size / 2;
            var y = e.clientY - rect.top - size / 2;

            var ripple = document.createElement('span');
            ripple.className = 'ripple-effect';
            ripple.style.width = ripple.style.height = size + 'px';
            ripple.style.left = x + 'px';
            ripple.style.top = y + 'px';

            // Đảm bảo btn có position relative
            if (getComputedStyle(btn).position === 'static') {
                btn.style.position = 'relative';
            }
            btn.style.overflow = 'hidden';
            btn.appendChild(ripple);

            ripple.addEventListener('animationend', function () {
                ripple.remove();
            });
        });
    }

    // ──────────────────────────────────────────────────────────
    // 3. COUNT-UP ANIMATION
    // ──────────────────────────────────────────────────────────
    function animateCountUp(el) {
        var target = parseFloat(el.getAttribute('data-target') || el.textContent.replace(/[^0-9.-]/g, ''));
        if (isNaN(target)) return;

        var prefix = el.getAttribute('data-prefix') || '';
        var suffix = el.getAttribute('data-suffix') || '';
        var decimals = parseInt(el.getAttribute('data-decimals') || '0', 10);
        var duration = parseInt(el.getAttribute('data-duration') || '1200', 10);
        var start = 0;
        var startTime = null;

        // Kiểm tra prefers-reduced-motion
        if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
            el.textContent = prefix + formatNumber(target, decimals) + suffix;
            return;
        }

        function formatNumber(n, d) {
            return n.toLocaleString('vi-VN', { minimumFractionDigits: d, maximumFractionDigits: d });
        }

        function step(ts) {
            if (!startTime) startTime = ts;
            var progress = Math.min((ts - startTime) / duration, 1);
            // Ease-out cubic
            var eased = 1 - Math.pow(1 - progress, 3);
            var current = start + (target - start) * eased;
            el.textContent = prefix + formatNumber(current, decimals) + suffix;
            if (progress < 1) {
                requestAnimationFrame(step);
            }
        }

        requestAnimationFrame(step);
    }

    function initCountUp() {
        var els = document.querySelectorAll('.count-up[data-target]');
        if (els.length === 0) return;

        var observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) {
                    animateCountUp(entry.target);
                    observer.unobserve(entry.target);
                }
            });
        }, { threshold: 0.3 });

        els.forEach(function (el) {
            el.textContent = '0';
            observer.observe(el);
        });
    }

    // ──────────────────────────────────────────────────────────
    // 4. SCROLL REVEAL (IntersectionObserver)
    // ──────────────────────────────────────────────────────────
    function initScrollReveal() {
        if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;

        var revealEls = document.querySelectorAll('.reveal, .reveal-left, .reveal-right, .reveal-scale');
        if (revealEls.length === 0) return;

        var observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) {
                    entry.target.classList.add('revealed');
                    observer.unobserve(entry.target);
                }
            });
        }, { threshold: 0.15, rootMargin: '0px 0px -40px 0px' });

        revealEls.forEach(function (el) {
            observer.observe(el);
        });
    }

    // ──────────────────────────────────────────────────────────
    // 5. TOAST AUTO-DISMISS
    // ──────────────────────────────────────────────────────────
    function initToastAutoDismiss() {
        var toasts = document.querySelectorAll('.erp-toast-alert');
        toasts.forEach(function (toast) {
            setTimeout(function () {
                toast.classList.add('erp-toast-dismiss');
                toast.addEventListener('animationend', function () {
                    toast.remove();
                });
            }, 5000);
        });
    }

    // ──────────────────────────────────────────────────────────
    // 6. BUTTON LOADING STATE
    // ──────────────────────────────────────────────────────────
    // Sử dụng: thêm data-loading="true" vào form, hoặc gọi ERP.btnLoading(btn)
    window.ERP = window.ERP || {};
    window.ERP.btnLoading = function (btn) {
        btn.classList.add('btn-loading');
        btn.disabled = true;
    };
    window.ERP.btnReset = function (btn) {
        btn.classList.remove('btn-loading');
        btn.disabled = false;
    };

    // ──────────────────────────────────────────────────────────
    // 7. CONFIRM MODAL
    // ──────────────────────────────────────────────────────────
    // Gọi: ERP.confirm({ title, message, confirmText, cancelText, variant }).then(ok => ...)
    window.ERP.confirm = function (opts) {
        opts = opts || {};
        var title = opts.title || 'Xác nhận';
        var message = opts.message || 'Bạn có chắc chắn muốn thực hiện?';
        var confirmText = opts.confirmText || 'Xác nhận';
        var cancelText = opts.cancelText || 'Hủy';
        var variant = opts.variant || 'danger';

        var iconClass = variant === 'warning' ? 'bi-exclamation-triangle-fill' : 'bi-trash3-fill';

        return new Promise(function (resolve) {
            var id = 'erpConfirm_' + Date.now();
            var html = '<div class="modal fade erp-confirm-modal" id="' + id + '" tabindex="-1">' +
                '<div class="modal-dialog modal-dialog-centered modal-sm">' +
                '<div class="modal-content">' +
                '<div class="modal-body">' +
                '<div class="erp-confirm-icon ' + HRM.escapeHtml(variant) + '"><i class="bi ' + HRM.escapeHtml(iconClass) + '"></i></div>' +
                '<h6 class="fw-bold mb-2">' + HRM.escapeHtml(title) + '</h6>' +
                '<p class="text-muted small mb-0">' + HRM.escapeHtml(message) + '</p>' +
                '</div>' +
                '<div class="modal-footer border-0 pt-0 justify-content-center gap-2">' +
                '<button type="button" class="btn btn-sm btn-outline-secondary px-3" data-bs-dismiss="modal">' + HRM.escapeHtml(cancelText) + '</button>' +
                '<button type="button" class="btn btn-sm btn-' + HRM.escapeHtml(variant) + ' px-3" id="' + id + '_ok">' + HRM.escapeHtml(confirmText) + '</button>' +
                '</div></div></div></div>';

            document.body.insertAdjacentHTML('beforeend', html);
            var modalEl = document.getElementById(id);
            var bsModal = new bootstrap.Modal(modalEl);
            bsModal.show();

            document.getElementById(id + '_ok').addEventListener('click', function () {
                bsModal.hide();
                resolve(true);
            });

            modalEl.addEventListener('hidden.bs.modal', function () {
                modalEl.remove();
                resolve(false);
            }, { once: true });
        });
    };

    // ──────────────────────────────────────────────────────────
    // 8. TOAST PROGRAMMATIC
    // ──────────────────────────────────────────────────────────
    // Gọi: ERP.toast('Thông báo thành công', 'success')
    window.ERP.toast = function (message, variant) {
        variant = variant || 'info';
        var icons = {
            success: 'bi-check-circle-fill',
            danger: 'bi-exclamation-triangle-fill',
            warning: 'bi-exclamation-circle-fill',
            info: 'bi-info-circle-fill'
        };
        var icon = icons[variant] || icons.info;

        var wrapper = document.querySelector('.erp-content-wrapper');
        if (!wrapper) wrapper = document.body;

        var toast = document.createElement('div');
        toast.className = 'erp-toast-alert erp-toast-' + variant + ' fade-in';
        toast.setAttribute('role', 'alert');
        toast.innerHTML = '<i class="bi ' + icon + '"></i><span>' + HRM.escapeHtml(message) + '</span>' +
            '<button type="button" class="btn-close btn-close-sm" style="margin-left:auto" onclick="this.parentElement.remove()"></button>';

        wrapper.insertBefore(toast, wrapper.firstChild);

        setTimeout(function () {
            toast.classList.add('erp-toast-dismiss');
            toast.addEventListener('animationend', function () { toast.remove(); });
        }, 5000);
    };

    // ──────────────────────────────────────────────────────────
    // KHỞI TẠO
    // ──────────────────────────────────────────────────────────
    document.addEventListener('DOMContentLoaded', function () {
        initSidebar();
        initRipple();
        initCountUp();
        initScrollReveal();
        initToastAutoDismiss();
    });
})();
