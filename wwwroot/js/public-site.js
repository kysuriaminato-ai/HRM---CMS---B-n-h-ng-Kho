/* Trang công khai - TIEN PHONG ERP
   Chức năng: giao diện sáng/tối, tìm kiếm trượt, nút lên đầu trang, hiệu ứng khi cuộn,
   đếm số liệu, thanh tiến độ đọc và mục lục bài viết, chia sẻ, form liên hệ (mở email).
   Mọi thao tác lưu trữ cục bộ đều có try/catch: trang vẫn chạy khi bị chặn localStorage. */
(function () {
    'use strict';

    var root = document.documentElement;
    root.classList.add('ps-js');

    var reduceMotion = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    function storageGet(key) {
        try { return window.localStorage.getItem(key); } catch (e) { return null; }
    }

    function storageSet(key, value) {
        try { window.localStorage.setItem(key, value); } catch (e) { /* bỏ qua */ }
    }

    function toast(message) {
        var host = document.getElementById('psToastHost');
        if (!host) return;
        var el = document.createElement('div');
        el.className = 'ps-toast';
        el.textContent = message;
        host.appendChild(el);
        window.requestAnimationFrame(function () { el.classList.add('is-shown'); });
        window.setTimeout(function () {
            el.classList.remove('is-shown');
            window.setTimeout(function () { el.remove(); }, 300);
        }, 3200);
    }

    function slugify(text) {
        return (text || '')
            .normalize('NFD')
            .replace(/[̀-ͯ]/g, '')
            .replace(/đ/g, 'd')
            .replace(/Đ/g, 'D')
            .toLowerCase()
            .replace(/[^a-z0-9]+/g, '-')
            .replace(/^-+|-+$/g, '');
    }

    /* ---------- Giao diện sáng / tối ---------- */
    var themeButton = document.getElementById('psThemeToggle');

    function applyTheme(theme) {
        root.setAttribute('data-bs-theme', theme);
        if (!themeButton) return;
        var dark = theme === 'dark';
        themeButton.setAttribute('aria-pressed', String(dark));
        themeButton.setAttribute('title', dark ? 'Chuyển sang giao diện sáng' : 'Chuyển sang giao diện tối');
        var icon = themeButton.querySelector('i');
        if (icon) icon.className = dark ? 'bi bi-sun' : 'bi bi-moon-stars';
    }

    applyTheme(root.getAttribute('data-bs-theme') === 'dark' ? 'dark' : 'light');

    if (themeButton) {
        themeButton.addEventListener('click', function () {
            var next = root.getAttribute('data-bs-theme') === 'dark' ? 'light' : 'dark';
            applyTheme(next);
            storageSet('ps-theme', next);
        });
    }

    /* ---------- Tìm kiếm trượt xuống ---------- */
    var searchButton = document.getElementById('psSearchToggle');
    var searchPanel = document.getElementById('psSearchPanel');

    if (searchButton && searchPanel) {
        searchButton.addEventListener('click', function () {
            var open = searchPanel.classList.toggle('is-open');
            searchButton.setAttribute('aria-expanded', String(open));
            if (open) {
                var input = document.getElementById('psSearchInput');
                if (input) input.focus();
            }
        });
    }

    /* ---------- Thanh điều hướng, nút lên đầu trang, tiến độ đọc, mục lục ---------- */
    var navWrap = document.querySelector('.ps-nav-wrap');
    var toTopButton = document.getElementById('psToTop');
    var progressBar = document.getElementById('psReadingProgress');
    var article = document.getElementById('psArticleBody');
    var tocLinks = [];

    function updateToc() {
        if (!tocLinks.length) return;
        var current = null;
        for (var i = 0; i < tocLinks.length; i++) {
            if (tocLinks[i].heading.getBoundingClientRect().top <= 140) current = tocLinks[i];
        }
        tocLinks.forEach(function (item) {
            item.link.classList.toggle('is-active', item === current);
        });
    }

    function onScroll() {
        var y = window.pageYOffset || document.documentElement.scrollTop || 0;

        if (navWrap) navWrap.classList.toggle('is-scrolled', y > 8);
        if (toTopButton) toTopButton.classList.toggle('is-visible', y > 480);

        if (progressBar && article) {
            var total = article.offsetHeight - window.innerHeight;
            var passed = -article.getBoundingClientRect().top;
            var ratio = total > 0 ? Math.min(1, Math.max(0, passed / total)) : 1;
            progressBar.style.transform = 'scaleX(' + ratio.toFixed(4) + ')';
        }

        updateToc();
    }

    window.addEventListener('scroll', onScroll, { passive: true });
    window.addEventListener('resize', onScroll);

    if (toTopButton) {
        toTopButton.addEventListener('click', function () {
            window.scrollTo({ top: 0, behavior: reduceMotion ? 'auto' : 'smooth' });
        });
    }

    if (article) {
        // Thời gian đọc ước tính (khoảng 200 từ/phút)
        var words = (article.textContent || '').trim();
        var wordCount = words ? words.split(/\s+/).length : 0;
        var readingTime = document.getElementById('psReadingTime');
        if (readingTime) readingTime.textContent = Math.max(1, Math.round(wordCount / 200)) + ' phút đọc';

        // Mục lục từ các tiêu đề h2, h3 của bài viết
        var tocContainer = document.getElementById('psToc');
        var headings = article.querySelectorAll('h2, h3');

        if (tocContainer && headings.length > 0) {
            var list = document.createElement('ul');
            headings.forEach(function (heading, index) {
                if (!heading.id) {
                    heading.id = (slugify(heading.textContent) || 'muc') + '-' + (index + 1);
                }
                var item = document.createElement('li');
                var link = document.createElement('a');
                link.href = '#' + heading.id;
                link.textContent = heading.textContent;
                if (heading.tagName === 'H3') {
                    link.className = 'ps-toc-sub';
                }
                item.appendChild(link);
                list.appendChild(item);
                tocLinks.push({ heading: heading, link: link });
            });
            tocContainer.appendChild(list);
        } else if (tocContainer) {
            var wrap = tocContainer.closest('.ps-toc-wrap');
            if (wrap) wrap.classList.add('d-none');
        }
    }

    onScroll();

    /* ---------- Hiệu ứng xuất hiện khi cuộn ---------- */
    var revealElements = document.querySelectorAll('.ps-reveal');

    if ('IntersectionObserver' in window && !reduceMotion) {
        var revealObserver = new IntersectionObserver(function (entries, observer) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) {
                    entry.target.classList.add('is-visible');
                    observer.unobserve(entry.target);
                }
            });
        }, { threshold: 0.12 });

        revealElements.forEach(function (el) { revealObserver.observe(el); });
    } else {
        revealElements.forEach(function (el) { el.classList.add('is-visible'); });
    }

    /* ---------- Đếm số liệu ---------- */
    var counters = document.querySelectorAll('[data-count]');

    function animateCount(el) {
        var target = parseFloat(el.getAttribute('data-count')) || 0;
        var decimals = parseInt(el.getAttribute('data-decimals') || '0', 10);
        var suffix = el.getAttribute('data-suffix') || '';

        if (reduceMotion) {
            el.textContent = target.toFixed(decimals) + suffix;
            return;
        }

        var start = null;
        var duration = 1400;

        function step(timestamp) {
            if (start === null) start = timestamp;
            var progress = Math.min(1, (timestamp - start) / duration);
            var eased = 1 - Math.pow(1 - progress, 3);
            el.textContent = (target * eased).toFixed(decimals) + suffix;
            if (progress < 1) window.requestAnimationFrame(step);
        }

        window.requestAnimationFrame(step);
    }

    if (counters.length) {
        counters.forEach(function (el) {
            if (!reduceMotion) el.textContent = '0' + (el.getAttribute('data-suffix') || '');
        });

        if ('IntersectionObserver' in window && !reduceMotion) {
            var counterObserver = new IntersectionObserver(function (entries, observer) {
                entries.forEach(function (entry) {
                    if (entry.isIntersecting) {
                        animateCount(entry.target);
                        observer.unobserve(entry.target);
                    }
                });
            }, { threshold: 0.5 });

            counters.forEach(function (el) { counterObserver.observe(el); });
        } else {
            counters.forEach(animateCount);
        }
    }

    /* ---------- Chia sẻ bài viết ---------- */
    function fallbackCopy(text) {
        var field = document.createElement('textarea');
        field.value = text;
        field.setAttribute('readonly', '');
        field.style.position = 'fixed';
        field.style.opacity = '0';
        document.body.appendChild(field);
        field.select();
        try { document.execCommand('copy'); } catch (e) { /* bỏ qua */ }
        field.remove();
    }

    document.querySelectorAll('[data-share]').forEach(function (button) {
        button.addEventListener('click', function () {
            var url = window.location.href;
            var type = button.getAttribute('data-share');

            if (type === 'copy') {
                if (navigator.clipboard && navigator.clipboard.writeText) {
                    navigator.clipboard.writeText(url).then(
                        function () { toast('Đã sao chép liên kết bài viết'); },
                        function () { fallbackCopy(url); toast('Đã sao chép liên kết bài viết'); }
                    );
                } else {
                    fallbackCopy(url);
                    toast('Đã sao chép liên kết bài viết');
                }
            } else if (type === 'facebook') {
                window.open('https://www.facebook.com/sharer/sharer.php?u=' + encodeURIComponent(url),
                    '_blank', 'noopener,noreferrer,width=600,height=500');
            } else if (type === 'email') {
                window.location.href = 'mailto:?subject=' + encodeURIComponent(document.title) +
                    '&body=' + encodeURIComponent(url);
            }
        });
    });

    /* ---------- Form liên hệ: chuẩn bị nội dung và mở ứng dụng email ----------
       Hệ thống chưa lưu yêu cầu liên hệ vào cơ sở dữ liệu, nên form chỉ mở email của người dùng. */
    var contactForm = document.getElementById('psContactForm');

    if (contactForm) {
        contactForm.addEventListener('submit', function (event) {
            event.preventDefault();

            if (!contactForm.checkValidity()) {
                contactForm.classList.add('was-validated');
                var firstInvalid = contactForm.querySelector(':invalid');
                if (firstInvalid) firstInvalid.focus();
                return;
            }

            var recipient = contactForm.getAttribute('data-email');
            if (!recipient) {
                toast('Chưa cấu hình email nhận liên hệ');
                return;
            }

            var field = function (name) {
                var el = contactForm.elements[name];
                return el ? String(el.value).trim() : '';
            };

            var service = field('service') || 'Tổng quát';
            var subject = '[Yêu cầu tư vấn] ' + service;
            var body = [
                'Họ và tên: ' + field('fullname'),
                'Số điện thoại: ' + field('phone'),
                'Email: ' + field('email'),
                'Phân hệ quan tâm: ' + service,
                '',
                field('message')
            ].join('\n');

            window.location.href = 'mailto:' + recipient +
                '?subject=' + encodeURIComponent(subject) +
                '&body=' + encodeURIComponent(body);

            toast('Đã mở ứng dụng email. Vui lòng nhấn Gửi để hoàn tất.');
        });
    }
})();
