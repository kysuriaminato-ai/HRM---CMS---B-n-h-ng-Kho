/*
 * Bảo mật phía trình duyệt, dùng chung cho mọi layout.
 * 1) Tự gửi mã chống CSRF (header RequestVerificationToken) cho các yêu cầu POST/PUT/DELETE cùng nguồn.
 * 2) Cung cấp HRM.escapeHtml / HRM.safeUrl để chèn dữ liệu vào innerHTML an toàn.
 */
(function (global) {
    'use strict';

    var TOKEN_HEADER = 'RequestVerificationToken';

    function getToken() {
        var input = document.querySelector('input[name="__RequestVerificationToken"]');
        return input ? input.value : null;
    }

    function isSameOrigin(url) {
        try {
            return new URL(url, global.location.href).origin === global.location.origin;
        } catch (e) {
            return false;
        }
    }

    if (typeof global.fetch === 'function') {
        var originalFetch = global.fetch;
        global.fetch = function (input, init) {
            init = init || {};
            var url = typeof input === 'string' ? input : (input && input.url) || '';
            var method = String(init.method || (input && input.method) || 'GET').toUpperCase();

            if (method !== 'GET' && method !== 'HEAD' && isSameOrigin(url)) {
                var token = getToken();
                if (token) {
                    var headers = new Headers(init.headers || (input && input.headers) || undefined);
                    if (!headers.has(TOKEN_HEADER)) {
                        headers.set(TOKEN_HEADER, token);
                    }
                    init = Object.assign({}, init, { headers: headers });
                }
            }
            return originalFetch.call(global, input, init);
        };
    }

    function escapeHtml(value) {
        return String(value === null || value === undefined ? '' : value)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    // Chỉ cho phép http(s), mailto, đường dẫn nội bộ "/..." và neo "#..."; còn lại trả về "javascript:void(0)".
    function safeUrl(value) {
        var s = String(value === null || value === undefined ? '' : value).replace(/[\u0000- \u007F]/g, '');
        if (/^(https?:|mailto:|\/(?!\/)|#)/i.test(s)) {
            return s;
        }
        return 'javascript:void(0)';
    }

    global.HRM = global.HRM || {};
    global.HRM.escapeHtml = escapeHtml;
    global.HRM.safeUrl = safeUrl;
})(window);
