/**
 * Enterprise Real-Time Notification & SignalR Client
 * ERP_HRM_CMS Platform
 */

(function () {
    // 1. Web Audio API Notification Chime (Không cần file mp3 ngoài)
    function playNotificationSound() {
        try {
            const ctx = new (window.AudioContext || window.webkitAudioContext)();
            const osc = ctx.createOscillator();
            const gain = ctx.createGain();

            osc.type = 'sine';
            osc.frequency.setValueAtTime(587.33, ctx.currentTime); // D5
            osc.frequency.exponentialRampToValueAtTime(880, ctx.currentTime + 0.15); // A5

            gain.gain.setValueAtTime(0.15, ctx.currentTime);
            gain.gain.exponentialRampToValueAtTime(0.01, ctx.currentTime + 0.35);

            osc.connect(gain);
            gain.connect(ctx.destination);

            osc.start();
            osc.stop(ctx.currentTime + 0.35);
        } catch (e) {
            console.debug('AudioContext notification muted or not supported yet');
        }
    }

    // 2. Hiển thị Dynamic Toast Notification
    function showNotificationToast(item) {
        let toastContainer = document.getElementById('erpToastContainer');
        if (!toastContainer) {
            toastContainer = document.createElement('div');
            toastContainer.id = 'erpToastContainer';
            toastContainer.className = 'toast-container position-fixed bottom-0 end-0 p-3';
            toastContainer.style.zIndex = '9999';
            document.body.appendChild(toastContainer);
        }

        const iconClass = item.type === 'ORDER' ? 'bi-cart-check-fill text-success' :
            (item.type === 'STOCK_ALERT' ? 'bi-exclamation-triangle-fill text-danger' : 'bi-bell-fill text-primary');

        const toastEl = document.createElement('div');
        toastEl.className = 'toast align-items-center border-0 shadow-lg mb-2';
        toastEl.setAttribute('role', 'alert');
        toastEl.setAttribute('aria-live', 'assertive');
        toastEl.setAttribute('aria-atomic', 'true');

        toastEl.innerHTML = `
            <div class="toast-header bg-dark text-white py-2">
                <i class="bi ${iconClass} me-2 fs-6"></i>
                <strong class="me-auto text-white" style="font-size: 13px;">${HRM.escapeHtml(item.title || 'Thông Báo Mới')}</strong>
                <small class="text-white-50" style="font-size: 11px;">Vừa xong</small>
                <button type="button" class="btn-close btn-close-white" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
            <div class="toast-body bg-white text-dark py-2 px-3 small">
                <p class="mb-2">${HRM.escapeHtml(item.content)}</p>
                ${item.linkUrl ? `<a href="${HRM.escapeHtml(HRM.safeUrl(item.linkUrl))}" class="btn btn-sm btn-primary py-0 px-2" style="font-size: 11px;">Xem chi tiết</a>` : ''}
            </div>
        `;

        toastContainer.appendChild(toastEl);
        const bsToast = new bootstrap.Toast(toastEl, { delay: 6000 });
        bsToast.show();

        toastEl.addEventListener('hidden.bs.toast', () => {
            toastEl.remove();
        });
    }

    // 3. Tải danh sách thông báo ban đầu
    window.refreshNotificationDropdown = function () {
        fetch('/Notification/GetRecentNotifications')
            .then(res => res.json())
            .then(data => {
                if (data.success) {
                    updateNotificationUI(data.unreadCount, data.items);
                }
            })
            .catch(err => console.debug('Cannot fetch notifications', err));
    };

    function updateNotificationUI(unreadCount, items) {
        const badge = document.getElementById('notificationBadge');
        if (badge) {
            if (unreadCount > 0) {
                badge.textContent = unreadCount > 99 ? '99+' : unreadCount;
                badge.style.display = 'inline-block';
            } else {
                badge.style.display = 'none';
            }
        }

        const listContainer = document.getElementById('notificationDropdownList');
        if (listContainer && items) {
            if (items.length === 0) {
                listContainer.innerHTML = '<div class="text-center py-4 text-muted small"><i class="bi bi-bell-slash fs-4 d-block mb-1"></i>Không có thông báo mới</div>';
                return;
            }

            listContainer.innerHTML = items.map(n => {
                const icon = n.type === 'ORDER' ? 'bi-cart-check text-success' :
                    (n.type === 'STOCK_ALERT' ? 'bi-exclamation-triangle text-danger' : 'bi-info-circle text-primary');
                const unreadDot = !n.isRead ? '<span class="p-1 bg-primary rounded-circle ms-auto"></span>' : '';

                return `
                    <a href="${HRM.escapeHtml(HRM.safeUrl(n.linkUrl || 'javascript:void(0)'))}" class="dropdown-item py-2 px-3 border-bottom d-flex align-items-start gap-2 ${!n.isRead ? 'bg-light' : ''}" style="white-space: normal;">
                        <i class="bi ${icon} fs-5 mt-1"></i>
                        <div class="flex-grow-1">
                            <div class="d-flex align-items-center justify-content-between mb-1">
                                <span class="fw-semibold small text-dark">${HRM.escapeHtml(n.title)}</span>
                                <span class="text-muted" style="font-size: 10px;">${HRM.escapeHtml(n.createdAt)}</span>
                            </div>
                            <p class="text-muted mb-0" style="font-size: 12px; line-height: 1.3;">${HRM.escapeHtml(n.content)}</p>
                        </div>
                        ${unreadDot}
                    </a>
                `;
            }).join('');
        }
    }

    // 4. Khởi tạo SignalR Connection
    function initSignalR() {
        if (typeof signalR === 'undefined') {
            console.debug('SignalR library not loaded yet');
            return;
        }

        const connection = new signalR.HubConnectionBuilder()
            .withUrl('/hub/notifications')
            .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
            .build();

        connection.on('ReceiveNotification', function (notif) {
            // Phát âm thanh
            playNotificationSound();

            // Hiển thị Toast
            showNotificationToast(notif);

            // Cập nhật số badge
            const badge = document.getElementById('notificationBadge');
            if (badge) {
                let current = parseInt(badge.textContent || '0') || 0;
                current += 1;
                badge.textContent = current > 99 ? '99+' : current;
                badge.style.display = 'inline-block';
            }

            // Tải lại danh sách dropdown nếu đang mở
            window.refreshNotificationDropdown();
        });

        connection.start()
            .then(() => {
                console.log('SignalR Notification Hub Connected.');
            })
            .catch(err => {
                console.debug('SignalR Connection Error: ', err);
            });
    }

    // Tự động nạp khi trang tải xong
    document.addEventListener('DOMContentLoaded', function () {
        window.refreshNotificationDropdown();
        initSignalR();
    });
})();
