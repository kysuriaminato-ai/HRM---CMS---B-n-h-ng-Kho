/**
 * Admin UI & Interactive Components - Giai đoạn A
 * Bao gồm: Sidebar + Accordion Menu, Sliding Tab Indicator, AJAX DataTable + FilterBar,
 * Drawer, Confirm Modal + [data-erp-confirm], Toast + Progress Bar (hover pause),
 * Shake Error, Success Tick SVG, Stagger Rows, View Transition Helper, Theme Color Reader.
 * Tôn trọng đầy đủ prefers-reduced-motion: reduce.
 */
(function () {
    'use strict';

    window.ERP = window.ERP || {};

    function reducedMotion() {
        return window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    }

    function escapeHtml(val) {
        if (window.HRM && typeof window.HRM.escapeHtml === 'function') {
            return window.HRM.escapeHtml(val);
        }
        return String(val === null || val === undefined ? '' : val)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    // ──────────────────────────────────────────────────────────
    // 0. THEME TOKEN READER & VIEW TRANSITION HELPER
    // ──────────────────────────────────────────────────────────
    ERP.themeColor = function (tokenName, fallback) {
        var val = getComputedStyle(document.documentElement).getPropertyValue(tokenName).trim();
        return val || fallback || '';
    };

    ERP.withViewTransition = function (mutateCallback) {
        if (typeof mutateCallback !== 'function') return Promise.resolve();
        if (!document.startViewTransition || reducedMotion()) {
            mutateCallback();
            return Promise.resolve();
        }
        try {
            var vt = document.startViewTransition(mutateCallback);
            return vt.finished.catch(function () { });
        } catch (e) {
            mutateCallback();
            return Promise.resolve();
        }
    };

    // ──────────────────────────────────────────────────────────
    // 1. SIDEBAR TOGGLE & ACCORDION MENU
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

        var navItems = document.querySelectorAll('.erp-nav-item');
        navItems.forEach(function (item) {
            var label = item.querySelector('.nav-label');
            var text = label ? label.textContent.trim() : item.textContent.trim();
            if (text && !item.getAttribute('data-tooltip')) {
                item.setAttribute('data-tooltip', text);
            }
        });

        highlightActiveSidebarItem();
        initAccordionSidebar();
    }

    function highlightActiveSidebarItem() {
        var path = window.location.pathname.toLowerCase();
        var navItems = document.querySelectorAll('.erp-nav-item');
        var bestMatch = null;
        var bestLength = 0;

        navItems.forEach(function (item) {
            item.classList.remove('active');
            var href = (item.getAttribute('href') || '').toLowerCase().split('?')[0];
            var dataPath = (item.getAttribute('data-path') || '').toLowerCase();

            // 1) Khớp chính xác đường dẫn href trước
            if (href && href === path && href.length >= bestLength) {
                bestMatch = item;
                bestLength = href.length + 100;
                return;
            }

            // 2) Khớp tiền tố data-path hoặc controller
            var candidate = dataPath || href;
            if (candidate && candidate !== '/' && path.indexOf(candidate) === 0 && candidate.length > bestLength) {
                bestMatch = item;
                bestLength = candidate.length;
            }
        });

        if ((path === '/' || path === '/home' || path === '/home/index') && !bestMatch) {
            navItems.forEach(function (item) {
                if (item.getAttribute('data-path') === '/' || item.getAttribute('href') === '/') {
                    bestMatch = item;
                }
            });
        }

        if (bestMatch) {
            bestMatch.classList.add('active');
        }
    }

    function initAccordionSidebar() {
        var groups = document.querySelectorAll('.erp-nav-group[data-group]');
        if (groups.length === 0) return;

        var savedState = {};
        try {
            savedState = JSON.parse(localStorage.getItem('erp_sidebar_groups') || '{}') || {};
        } catch (e) {
            savedState = {};
        }

        var currentModule = (document.documentElement.getAttribute('data-module') || '').toLowerCase();

        groups.forEach(function (group) {
            var key = group.getAttribute('data-group');
            var toggle = group.querySelector('.erp-nav-group-toggle');
            var hasActiveChild = !!group.querySelector('.erp-nav-item.active');
            var isCurrentModuleGroup = key && key.toLowerCase() === currentModule;

            var shouldOpen = hasActiveChild || isCurrentModuleGroup ||
                (savedState[key] !== undefined ? !!savedState[key] : group.classList.contains('open'));

            setAccordionGroupState(group, shouldOpen);

            if (toggle && !toggle.dataset.erpBound) {
                toggle.dataset.erpBound = '1';
                toggle.addEventListener('click', function () {
                    var nextOpen = !group.classList.contains('open');
                    setAccordionGroupState(group, nextOpen);
                    savedState[key] = nextOpen;
                    try {
                        localStorage.setItem('erp_sidebar_groups', JSON.stringify(savedState));
                    } catch (e) { /* Ignore storage quota */ }
                });
            }
        });
    }

    function setAccordionGroupState(group, isOpen) {
        group.classList.toggle('open', isOpen);
        var toggle = group.querySelector('.erp-nav-group-toggle');
        if (toggle) {
            toggle.setAttribute('aria-expanded', isOpen ? 'true' : 'false');
        }
    }

    ERP.initAccordionSidebar = initAccordionSidebar;

    // ──────────────────────────────────────────────────────────
    // 2. RIPPLE EFFECT
    // ──────────────────────────────────────────────────────────
    function initRipple() {
        document.addEventListener('click', function (e) {
            var btn = e.target.closest('.ripple, .btn-primary, .btn-success, .btn-danger, .btn-warning');
            if (!btn || btn.disabled || reducedMotion()) return;

            var rect = btn.getBoundingClientRect();
            var size = Math.max(rect.width, rect.height);
            var x = (e.clientX || (rect.left + rect.width / 2)) - rect.left - size / 2;
            var y = (e.clientY || (rect.top + rect.height / 2)) - rect.top - size / 2;

            var ripple = document.createElement('span');
            ripple.className = 'ripple-effect';
            ripple.style.width = ripple.style.height = size + 'px';
            ripple.style.left = x + 'px';
            ripple.style.top = y + 'px';

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
        var duration = parseInt(el.getAttribute('data-duration') || '1000', 10);
        var startTime = null;

        function formatNumber(n, d) {
            return n.toLocaleString('vi-VN', { minimumFractionDigits: d, maximumFractionDigits: d });
        }

        if (reducedMotion()) {
            el.textContent = prefix + formatNumber(target, decimals) + suffix;
            return;
        }

        function step(ts) {
            if (!startTime) startTime = ts;
            var progress = Math.min((ts - startTime) / duration, 1);
            var eased = 1 - Math.pow(1 - progress, 3);
            var current = target * eased;
            el.textContent = prefix + formatNumber(current, decimals) + suffix;
            if (progress < 1) {
                requestAnimationFrame(step);
            }
        }

        requestAnimationFrame(step);
    }

    function initCountUp(scope) {
        var root = scope || document;
        var els = root.querySelectorAll('.count-up[data-target]');
        if (els.length === 0) return;

        if (!('IntersectionObserver' in window) || reducedMotion()) {
            els.forEach(animateCountUp);
            return;
        }

        var observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) {
                    animateCountUp(entry.target);
                    observer.unobserve(entry.target);
                }
            });
        }, { threshold: 0.25 });

        els.forEach(function (el) {
            el.textContent = '0';
            observer.observe(el);
        });
    }

    // ──────────────────────────────────────────────────────────
    // 4. SCROLL REVEAL & STAGGER ROWS
    // ──────────────────────────────────────────────────────────
    function initScrollReveal() {
        var revealEls = document.querySelectorAll('.reveal, .reveal-left, .reveal-right, .reveal-scale');
        if (revealEls.length === 0) return;

        if (reducedMotion() || !('IntersectionObserver' in window)) {
            revealEls.forEach(function (el) { el.classList.add('revealed'); });
            return;
        }

        var observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) {
                    entry.target.classList.add('revealed');
                    observer.unobserve(entry.target);
                }
            });
        }, { threshold: 0.12, rootMargin: '0px 0px -30px 0px' });

        revealEls.forEach(function (el) { observer.observe(el); });
    }

    ERP.staggerRows = function (tableOrTbody) {
        var el = typeof tableOrTbody === 'string' ? document.querySelector(tableOrTbody) : tableOrTbody;
        if (!el || reducedMotion()) return;
        var tbody = el.tagName === 'TBODY' ? el : el.querySelector('tbody');
        if (!tbody) return;

        tbody.classList.remove('stagger-rows');
        void tbody.offsetWidth; // Force reflow để chạy lại stagger sau khi AJAX cập nhật
        tbody.classList.add('stagger-rows');
    };

    // ──────────────────────────────────────────────────────────
    // 5. SLIDING TAB INDICATOR & ACCESSIBLE TABS
    // ──────────────────────────────────────────────────────────
    function updateTabIndicator(tabBar) {
        var indicator = tabBar.querySelector('.erp-tab-indicator');
        var activeTab = tabBar.querySelector('.erp-tab.active');
        if (!indicator || !activeTab) return;

        tabBar.style.setProperty('--tab-x', activeTab.offsetLeft + 'px');
        tabBar.style.setProperty('--tab-w', activeTab.offsetWidth + 'px');
    }

    function initTabs(scope) {
        var root = scope || document;
        var tabBars = root.querySelectorAll('.erp-tabs');

        tabBars.forEach(function (tabBar) {
            if (tabBar.dataset.erpTabsBound) {
                updateTabIndicator(tabBar);
                return;
            }
            tabBar.dataset.erpTabsBound = '1';

            var tabs = Array.prototype.slice.call(tabBar.querySelectorAll('.erp-tab'));
            if (tabs.length === 0) return;

            var indicator = tabBar.querySelector('.erp-tab-indicator');
            if (!indicator) {
                indicator = document.createElement('span');
                indicator.className = 'erp-tab-indicator';
                indicator.setAttribute('aria-hidden', 'true');
                tabBar.appendChild(indicator);
            }
            tabBar.classList.add('has-indicator');

            function activateTab(tab, focusTab) {
                var targetId = tab.getAttribute('data-tab-target') || tab.getAttribute('aria-controls');
                var container = tabBar.closest('.erp-tabs-container') || tabBar.parentElement;

                tabs.forEach(function (t) {
                    var isCurrent = t === tab;
                    t.classList.toggle('active', isCurrent);
                    t.setAttribute('aria-selected', isCurrent ? 'true' : 'false');
                    t.setAttribute('tabindex', isCurrent ? '0' : '-1');
                });

                updateTabIndicator(tabBar);
                if (focusTab) tab.focus();

                if (targetId && container) {
                    var panels = container.querySelectorAll(':scope > .erp-tab-panels > .erp-tab-panel, :scope > .erp-tab-panel');
                    panels.forEach(function (panel) {
                        var match = panel.id === targetId;
                        panel.classList.toggle('active', match);
                        panel.hidden = !match;
                    });
                }
            }

            tabs.forEach(function (tab, index) {
                tab.addEventListener('click', function () {
                    if (tab.tagName === 'A' && tab.getAttribute('href') && tab.getAttribute('href') !== '#') {
                        return; // Cho phép tab điều hướng URL hoạt động bình thường
                    }
                    activateTab(tab, false);
                });

                tab.addEventListener('keydown', function (e) {
                    var nextIndex = -1;
                    if (e.key === 'ArrowRight') nextIndex = (index + 1) % tabs.length;
                    else if (e.key === 'ArrowLeft') nextIndex = (index - 1 + tabs.length) % tabs.length;
                    else if (e.key === 'Home') nextIndex = 0;
                    else if (e.key === 'End') nextIndex = tabs.length - 1;

                    if (nextIndex >= 0) {
                        e.preventDefault();
                        activateTab(tabs[nextIndex], true);
                    }
                });
            });

            if (!tabBar.querySelector('.erp-tab.active') && tabs[0]) {
                tabs[0].classList.add('active');
            }
            updateTabIndicator(tabBar);

            if ('ResizeObserver' in window) {
                var ro = new ResizeObserver(function () { updateTabIndicator(tabBar); });
                ro.observe(tabBar);
            }
        });
    }

    ERP.initTabs = initTabs;

    // ──────────────────────────────────────────────────────────
    // 6. DRAWER COMPONENT
    // ──────────────────────────────────────────────────────────
    ERP.drawer = {
        open: function (id) {
            var drawer = typeof id === 'string' ? document.getElementById(id) : id;
            if (!drawer) return;
            var backdrop = document.querySelector('.erp-drawer-backdrop[data-drawer-backdrop="' + drawer.id + '"]');
            if (backdrop) backdrop.classList.add('show');
            drawer.classList.add('show');
            drawer.setAttribute('aria-hidden', 'false');
            document.body.style.overflow = 'hidden';

            var focusable = drawer.querySelector('input, select, textarea, button:not([data-drawer-close])');
            if (focusable) {
                setTimeout(function () { focusable.focus(); }, 100);
            }
        },
        close: function (id) {
            var drawer = typeof id === 'string' ? document.getElementById(id) : id;
            if (!drawer) return;
            var backdrop = document.querySelector('.erp-drawer-backdrop[data-drawer-backdrop="' + drawer.id + '"]');
            if (backdrop) backdrop.classList.remove('show');
            drawer.classList.remove('show');
            drawer.setAttribute('aria-hidden', 'true');
            if (!document.querySelector('.erp-drawer.show')) {
                document.body.style.overflow = '';
            }
        }
    };

    function initDrawers() {
        document.addEventListener('click', function (e) {
            var openBtn = e.target.closest('[data-drawer-open]');
            if (openBtn) {
                e.preventDefault();
                ERP.drawer.open(openBtn.getAttribute('data-drawer-open'));
                return;
            }

            var closeBtn = e.target.closest('[data-drawer-close]');
            if (closeBtn) {
                e.preventDefault();
                var targetId = closeBtn.getAttribute('data-drawer-close');
                if (targetId) {
                    ERP.drawer.close(targetId);
                } else {
                    var parentDrawer = closeBtn.closest('.erp-drawer');
                    if (parentDrawer) ERP.drawer.close(parentDrawer);
                }
                return;
            }

            var backdrop = e.target.closest('.erp-drawer-backdrop');
            if (backdrop) {
                ERP.drawer.close(backdrop.getAttribute('data-drawer-backdrop'));
            }
        });

        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') {
                var openDrawers = document.querySelectorAll('.erp-drawer.show');
                openDrawers.forEach(function (d) { ERP.drawer.close(d); });
            }
        });
    }

    // ──────────────────────────────────────────────────────────
    // 7. SHAKE ON ERROR & SUCCESS TICK SVG
    // ──────────────────────────────────────────────────────────
    ERP.shake = function (el) {
        var target = typeof el === 'string' ? document.querySelector(el) : el;
        if (!target) return;
        target.classList.remove('erp-shake');
        if (!reducedMotion()) {
            void target.offsetWidth;
        }
        target.classList.add('erp-shake');
        setTimeout(function () {
            target.classList.remove('erp-shake');
        }, 650);
    };

    ERP.successTick = function (container, opts) {
        var target = typeof container === 'string' ? document.querySelector(container) : container;
        if (!target) return null;
        opts = opts || {};
        var sizeClass = opts.small ? ' erp-success-tick-sm' : '';
        var wrapper = document.createElement('span');
        wrapper.className = 'erp-success-tick' + sizeClass;
        wrapper.setAttribute('role', 'status');
        wrapper.setAttribute('aria-label', opts.label || 'Thành công');
        wrapper.innerHTML =
            '<svg viewBox="0 0 52 52" aria-hidden="true">' +
                '<circle class="erp-success-tick-circle" cx="26" cy="26" r="24" />' +
                '<path class="erp-success-tick-check" d="M15 27l7.5 7.5L37 19" />' +
            '</svg>';
        if (opts.replace !== false) {
            target.innerHTML = '';
        }
        target.appendChild(wrapper);
        return wrapper;
    };

    function initValidationShake() {
        document.addEventListener('invalid', function (e) {
            var field = e.target;
            if (field && field.closest('form')) {
                ERP.shake(field);
            }
        }, true);
    }

    // ──────────────────────────────────────────────────────────
    // 8. TOAST WITH PROGRESS BAR & HOVER PAUSE
    // ──────────────────────────────────────────────────────────
    function ensureToastContainer() {
        var c = document.getElementById('erpToastContainer');
        if (!c && document.body) {
            c = document.createElement('div');
            c.id = 'erpToastContainer';
            c.className = 'erp-toast-container';
            c.setAttribute('aria-live', 'polite');
            document.body.appendChild(c);
        }
        return c;
    }

    function bindToastLifecycle(toast, duration) {
        var remaining = duration || 5000;
        var startedAt = Date.now();
        var timerId = null;

        var progress = toast.querySelector('.erp-toast-progress');
        if (progress) {
            progress.style.setProperty('--toast-duration', remaining + 'ms');
        }

        function dismiss() {
            if (toast.dataset.dismissing) return;
            toast.dataset.dismissing = '1';
            clearTimeout(timerId);
            if (reducedMotion()) {
                toast.remove();
                return;
            }
            toast.classList.add('erp-toast-dismiss');
            toast.addEventListener('animationend', function () {
                toast.remove();
            }, { once: true });
            setTimeout(function () {
                if (toast.parentNode) toast.remove();
            }, 350);
        }

        function startTimer() {
            startedAt = Date.now();
            timerId = setTimeout(dismiss, remaining);
        }

        toast.addEventListener('mouseenter', function () {
            clearTimeout(timerId);
            remaining = Math.max(400, remaining - (Date.now() - startedAt));
        });

        toast.addEventListener('mouseleave', function () {
            startTimer();
        });

        var closeBtn = toast.querySelector('.btn-close');
        if (closeBtn) {
            closeBtn.addEventListener('click', function (e) {
                e.preventDefault();
                dismiss();
            });
        }

        startTimer();
    }

    function initExistingToasts() {
        var toasts = document.querySelectorAll('.erp-toast-alert');
        toasts.forEach(function (toast) {
            if (toast.dataset.erpToastBound) return;
            toast.dataset.erpToastBound = '1';
            if (!toast.querySelector('.erp-toast-progress')) {
                var bar = document.createElement('div');
                bar.className = 'erp-toast-progress';
                toast.appendChild(bar);
            }
            bindToastLifecycle(toast, 5000);
        });
    }

    ERP.toast = function (message, variant, opts) {
        variant = variant || 'info';
        opts = opts || {};
        var duration = opts.duration || 5000;
        var icons = {
            success: 'bi-check-circle-fill',
            danger: 'bi-exclamation-triangle-fill',
            warning: 'bi-exclamation-circle-fill',
            info: 'bi-info-circle-fill'
        };
        var icon = icons[variant] || icons.info;

        var container = ensureToastContainer() || document.body;
        var toast = document.createElement('div');
        toast.className = 'erp-toast-alert erp-toast-' + escapeHtml(variant);
        toast.dataset.erpToastBound = '1';
        toast.setAttribute('role', 'alert');
        toast.innerHTML =
            '<i class="bi ' + icon + ' fs-6"></i>' +
            '<span class="flex-grow-1">' + escapeHtml(message) + '</span>' +
            '<button type="button" class="btn-close btn-close-sm" aria-label="Đóng"></button>' +
            '<div class="erp-toast-progress"></div>';

        container.appendChild(toast);
        bindToastLifecycle(toast, duration);
        return toast;
    };

    // ──────────────────────────────────────────────────────────
    // 9. BUTTON LOADING & CONFIRM MODAL (+ DECLARATIVE BINDING)
    // ──────────────────────────────────────────────────────────
    ERP.btnLoading = function (btn) {
        if (!btn) return;
        btn.classList.add('btn-loading');
        btn.disabled = true;
    };

    ERP.btnReset = function (btn) {
        if (!btn) return;
        btn.classList.remove('btn-loading');
        btn.disabled = false;
    };

    ERP.confirm = function (opts) {
        opts = opts || {};
        var title = opts.title || 'Xác nhận thao tác';
        var message = opts.message || 'Bạn có chắc chắn muốn thực hiện hành động này?';
        var confirmText = opts.confirmText || 'Xác nhận';
        var cancelText = opts.cancelText || 'Hủy';
        var variant = opts.variant || 'danger';

        var iconMap = {
            danger: 'bi-trash3-fill',
            warning: 'bi-exclamation-triangle-fill',
            info: 'bi-info-circle-fill',
            primary: 'bi-question-circle-fill'
        };
        var iconClass = iconMap[variant] || iconMap.danger;

        return new Promise(function (resolve) {
            var id = 'erpConfirm_' + Date.now();
            var html =
                '<div class="modal fade erp-confirm-modal" id="' + id + '" tabindex="-1" aria-hidden="true">' +
                    '<div class="modal-dialog modal-dialog-centered modal-sm">' +
                        '<div class="modal-content">' +
                            '<div class="modal-body">' +
                                '<div class="erp-confirm-icon ' + escapeHtml(variant) + '"><i class="bi ' + escapeHtml(iconClass) + '"></i></div>' +
                                '<h6 class="fw-bold mb-2">' + escapeHtml(title) + '</h6>' +
                                '<p class="text-muted small mb-0">' + escapeHtml(message) + '</p>' +
                            '</div>' +
                            '<div class="modal-footer border-0 pt-0 pb-4 justify-content-center gap-2">' +
                                '<button type="button" class="btn btn-sm btn-outline-secondary px-3" data-bs-dismiss="modal">' + escapeHtml(cancelText) + '</button>' +
                                '<button type="button" class="btn btn-sm btn-' + escapeHtml(variant) + ' px-3" id="' + id + '_ok">' + escapeHtml(confirmText) + '</button>' +
                            '</div>' +
                        '</div>' +
                    '</div>' +
                '</div>';

            document.body.insertAdjacentHTML('beforeend', html);
            var modalEl = document.getElementById(id);
            var bsModal = new bootstrap.Modal(modalEl);
            var confirmed = false;
            bsModal.show();

            document.getElementById(id + '_ok').addEventListener('click', function () {
                confirmed = true;
                bsModal.hide();
                resolve(true);
            });

            modalEl.addEventListener('hidden.bs.modal', function () {
                modalEl.remove();
                if (!confirmed) resolve(false);
            }, { once: true });
        });
    };

    function initDeclarativeConfirm() {
        document.addEventListener('click', function (e) {
            var trigger = e.target.closest('[data-erp-confirm]');
            if (!trigger || trigger.dataset.erpConfirmed === '1') return;

            e.preventDefault();
            e.stopPropagation();

            ERP.confirm({
                title: trigger.getAttribute('data-confirm-title') || 'Xác nhận',
                message: trigger.getAttribute('data-erp-confirm'),
                confirmText: trigger.getAttribute('data-confirm-ok') || 'Đồng ý',
                cancelText: trigger.getAttribute('data-confirm-cancel') || 'Hủy',
                variant: trigger.getAttribute('data-confirm-variant') || 'danger'
            }).then(function (ok) {
                if (!ok) return;
                var form = trigger.closest('form');
                if (trigger.type === 'submit' && form) {
                    trigger.dataset.erpConfirmed = '1';
                    form.requestSubmit ? form.requestSubmit(trigger) : form.submit();
                } else if (trigger.tagName === 'A' && trigger.href) {
                    window.location.href = trigger.href;
                } else {
                    trigger.dataset.erpConfirmed = '1';
                    trigger.click();
                    delete trigger.dataset.erpConfirmed;
                }
            });
        });
    }

    // ──────────────────────────────────────────────────────────
    // 10. AJAX DATA TABLE & FILTER BAR (Lọc / Sắp xếp / Phân trang)
    // ──────────────────────────────────────────────────────────
    function initDataTables(scope) {
        var root = scope || document;
        var wrappers = root.querySelectorAll('.erp-table-wrapper[data-ajax-url]');

        wrappers.forEach(function (wrapper) {
            if (wrapper.dataset.erpTableBound) return;
            wrapper.dataset.erpTableBound = '1';

            var tableId = wrapper.id;
            var filterForm = tableId
                ? document.querySelector('.erp-filter-bar[data-target-table="' + tableId + '"]')
                : null;

            function loadTable(paramsOverride) {
                var baseUrl = wrapper.getAttribute('data-ajax-url');
                if (!baseUrl) return;

                var url = new URL(baseUrl, window.location.origin);
                if (filterForm) {
                    var fd = new FormData(filterForm);
                    fd.forEach(function (value, key) {
                        if (value !== null && String(value).trim() !== '') {
                            url.searchParams.set(key, String(value).trim());
                        }
                    });
                }

                var currentSortBy = wrapper.getAttribute('data-sort-by') || '';
                var currentSortDir = wrapper.getAttribute('data-sort-dir') || 'asc';
                var currentPage = wrapper.getAttribute('data-page-index') || '1';
                var currentPageSize = wrapper.getAttribute('data-page-size') || '';

                if (currentSortBy) url.searchParams.set('sortBy', currentSortBy);
                if (currentSortDir) url.searchParams.set('sortDir', currentSortDir);
                if (currentPage) url.searchParams.set('page', currentPage);
                if (currentPageSize) url.searchParams.set('pageSize', currentPageSize);

                if (paramsOverride) {
                    Object.keys(paramsOverride).forEach(function (k) {
                        if (paramsOverride[k] === null || paramsOverride[k] === '') {
                            url.searchParams.delete(k);
                        } else {
                            url.searchParams.set(k, paramsOverride[k]);
                        }
                    });
                }

                wrapper.classList.add('is-loading');

                fetch(url.toString(), {
                    headers: { 'X-Requested-With': 'XMLHttpRequest' }
                })
                    .then(function (res) {
                        if (!res.ok) throw new Error('HTTP ' + res.status);
                        return res.text();
                    })
                    .then(function (html) {
                        var parser = new DOMParser();
                        var doc = parser.parseFromString(html, 'text/html');
                        var nextWrapper = tableId ? doc.getElementById(tableId) : doc.querySelector('.erp-table-wrapper');
                        var bodySlot = wrapper.querySelector('[data-erp-table-slot]');

                        ERP.withViewTransition(function () {
                            if (nextWrapper) {
                                wrapper.innerHTML = nextWrapper.innerHTML;
                                ['data-sort-by', 'data-sort-dir', 'data-page-index', 'data-page-size'].forEach(function (attr) {
                                    if (nextWrapper.hasAttribute(attr)) {
                                        wrapper.setAttribute(attr, nextWrapper.getAttribute(attr));
                                    }
                                });
                            } else if (bodySlot) {
                                bodySlot.innerHTML = html;
                            } else {
                                var tbody = wrapper.querySelector('tbody');
                                if (tbody) tbody.innerHTML = html;
                            }
                            ERP.staggerRows(wrapper);
                            initCountUp(wrapper);
                        });
                    })
                    .catch(function () {
                        ERP.toast('Không thể tải dữ liệu bảng. Vui lòng thử lại.', 'danger');
                    })
                    .finally(function () {
                        wrapper.classList.remove('is-loading');
                    });
            }

            wrapper.addEventListener('click', function (e) {
                var sortTh = e.target.closest('th[data-sort]');
                if (sortTh) {
                    e.preventDefault();
                    var col = sortTh.getAttribute('data-sort');
                    var prevCol = wrapper.getAttribute('data-sort-by');
                    var prevDir = wrapper.getAttribute('data-sort-dir') || 'asc';
                    var nextDir = (prevCol === col && prevDir === 'asc') ? 'desc' : 'asc';
                    wrapper.setAttribute('data-sort-by', col);
                    wrapper.setAttribute('data-sort-dir', nextDir);
                    wrapper.setAttribute('data-page-index', '1');
                    loadTable({ sortBy: col, sortDir: nextDir, page: '1' });
                    return;
                }

                var pageBtn = e.target.closest('[data-erp-page]');
                if (pageBtn && !pageBtn.disabled && !pageBtn.classList.contains('disabled')) {
                    e.preventDefault();
                    var p = pageBtn.getAttribute('data-erp-page');
                    wrapper.setAttribute('data-page-index', p);
                    loadTable({ page: p });
                }
            });

            wrapper.addEventListener('change', function (e) {
                var sizeSelect = e.target.closest('[data-erp-page-size]');
                if (sizeSelect) {
                    wrapper.setAttribute('data-page-size', sizeSelect.value);
                    wrapper.setAttribute('data-page-index', '1');
                    loadTable({ pageSize: sizeSelect.value, page: '1' });
                }
            });

            if (filterForm && !filterForm.dataset.erpFilterBound) {
                filterForm.dataset.erpFilterBound = '1';
                var debounceTimer = null;

                filterForm.addEventListener('submit', function (e) {
                    e.preventDefault();
                    wrapper.setAttribute('data-page-index', '1');
                    loadTable({ page: '1' });
                });

                filterForm.addEventListener('input', function (e) {
                    if (e.target.matches('input[type="text"], input[type="search"]')) {
                        clearTimeout(debounceTimer);
                        debounceTimer = setTimeout(function () {
                            wrapper.setAttribute('data-page-index', '1');
                            loadTable({ page: '1' });
                        }, 300);
                    }
                });

                filterForm.addEventListener('change', function (e) {
                    if (e.target.matches('select, input[type="date"]')) {
                        wrapper.setAttribute('data-page-index', '1');
                        loadTable({ page: '1' });
                    }
                });

                var resetBtn = filterForm.querySelector('[data-filter-reset]');
                if (resetBtn) {
                    resetBtn.addEventListener('click', function (e) {
                        e.preventDefault();
                        filterForm.reset();
                        filterForm.querySelectorAll('input[type="text"], input[type="search"]').forEach(function (inp) { inp.value = ''; });
                        filterForm.querySelectorAll('select').forEach(function (sel) { sel.selectedIndex = 0; });
                        wrapper.setAttribute('data-page-index', '1');
                        loadTable({ page: '1' });
                    });
                }
            }
        });
    }

    ERP.dataTable = { init: initDataTables };

    // ──────────────────────────────────────────────────────────
    // 10. INTERACTIVE ORG CHART (SƠ ĐỒ CÂY TỔ CHỨC)
    // ──────────────────────────────────────────────────────────
    function initOrgChart() {
        var viewports = document.querySelectorAll('.erp-org-viewport');
        viewports.forEach(function (viewport) {
            var canvas = viewport.querySelector('.erp-org-canvas');
            if (!canvas) return;

            var currentScale = 1.0;
            var isPanning = false;
            var startX = 0, startY = 0;
            var currentX = 0, currentY = 0;

            function updateTransform() {
                canvas.style.transform = 'translate(' + currentX + 'px, ' + currentY + 'px) scale(' + currentScale + ')';
            }

            // Nút thu gọn / mở rộng nhánh cây
            viewport.addEventListener('click', function (e) {
                var toggleBtn = e.target.closest('.org-toggle-btn');
                if (toggleBtn) {
                    e.preventDefault();
                    e.stopPropagation();
                    var nodeLi = toggleBtn.closest('li');
                    if (nodeLi) {
                        nodeLi.classList.toggle('org-collapsed');
                    }
                }

                // Nút điều khiển Zoom
                var controlBtn = e.target.closest('.org-control-btn');
                if (controlBtn) {
                    var action = controlBtn.getAttribute('data-org-action');
                    if (action === 'zoom-in') {
                        currentScale = Math.min(currentScale + 0.15, 2.0);
                        updateTransform();
                    } else if (action === 'zoom-out') {
                        currentScale = Math.max(currentScale - 0.15, 0.5);
                        updateTransform();
                    } else if (action === 'zoom-reset') {
                        currentScale = 1.0;
                        currentX = 0;
                        currentY = 0;
                        updateTransform();
                    }
                }
            });

            // Kéo chuột để pan sơ đồ
            viewport.addEventListener('mousedown', function (e) {
                if (e.target.closest('.org-node-card') || e.target.closest('.org-controls-bar')) return;
                isPanning = true;
                startX = e.clientX - currentX;
                startY = e.clientY - currentY;
                viewport.style.cursor = 'grabbing';
            });

            window.addEventListener('mousemove', function (e) {
                if (!isPanning) return;
                currentX = e.clientX - startX;
                currentY = e.clientY - startY;
                updateTransform();
            });

            window.addEventListener('mouseup', function () {
                if (isPanning) {
                    isPanning = false;
                    viewport.style.cursor = 'grab';
                }
            });
        });
    }

    ERP.orgChart = { init: initOrgChart };

    // ──────────────────────────────────────────────────────────
    // 11. ATS KANBAN BOARD DRAG & DROP
    // ──────────────────────────────────────────────────────────
    function initKanbanBoard() {
        var boards = document.querySelectorAll('.erp-kanban-board');
        boards.forEach(function (board) {
            var cards = board.querySelectorAll('.kanban-card');
            var columns = board.querySelectorAll('.kanban-column');
            var draggedCard = null;
            var sourceCol = null;

            function updateColumnCounters() {
                columns.forEach(function (col) {
                    var counter = col.querySelector('.kanban-counter');
                    var count = col.querySelectorAll('.kanban-card').length;
                    if (counter) counter.textContent = count;
                });
            }

            cards.forEach(function (card) {
                card.setAttribute('draggable', 'true');

                card.addEventListener('dragstart', function (e) {
                    draggedCard = card;
                    sourceCol = card.closest('.kanban-column');
                    card.classList.add('is-dragging');
                    e.dataTransfer.effectAllowed = 'move';
                    e.dataTransfer.setData('text/plain', card.id || card.getAttribute('data-id') || '');
                });

                card.addEventListener('dragend', function () {
                    if (draggedCard) draggedCard.classList.remove('is-dragging');
                    draggedCard = null;
                    sourceCol = null;
                    columns.forEach(function (col) { col.classList.remove('is-drag-over'); });
                    updateColumnCounters();
                });
            });

            columns.forEach(function (col) {
                var body = col.querySelector('.kanban-column-body');
                if (!body) return;

                col.addEventListener('dragover', function (e) {
                    e.preventDefault();
                    e.dataTransfer.dropEffect = 'move';
                    col.classList.add('is-drag-over');
                });

                col.addEventListener('dragleave', function (e) {
                    if (!col.contains(e.relatedTarget)) {
                        col.classList.remove('is-drag-over');
                    }
                });

                col.addEventListener('drop', function (e) {
                    e.preventDefault();
                    col.classList.remove('is-drag-over');
                    if (draggedCard && body) {
                        body.appendChild(draggedCard);
                        updateColumnCounters();
                        window.dispatchEvent(new CustomEvent('erp:kanban:cardmoved', {
                            detail: {
                                cardId: draggedCard.getAttribute('data-id'),
                                targetColumn: col.getAttribute('data-column-id')
                            }
                        }));
                    }
                });
            });

            updateColumnCounters();
        });
    }

    ERP.kanban = { init: initKanbanBoard };

    // ──────────────────────────────────────────────────────────
    // KHỞI TẠO CHUNG
    // ──────────────────────────────────────────────────────────
    document.addEventListener('DOMContentLoaded', function () {
        initSidebar();
        initRipple();
        initCountUp();
        initScrollReveal();
        initExistingToasts();
        initTabs();
        initDrawers();
        initDeclarativeConfirm();
        initValidationShake();
        initDataTables();
        initOrgChart();
        initKanbanBoard();
    });
})();
