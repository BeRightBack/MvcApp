// Ads client-side banner rotation
// Cycles banners inside zones that have more banners than slots (data-promo-rotate).
// Fires an impression beacon for each banner as it rotates into view.

(function () {
    'use strict';

    if (window.__adRotateLoaded) return;
    window.__adRotateLoaded = true;

    const DEFAULT_INTERVAL = 6000;

    function init() {
        document.querySelectorAll('[data-promo-rotate]').forEach(zone => {
            const items = Array.from(zone.querySelectorAll('.promo-rotate-item'));
            if (items.length <= 1) return;

            const interval = parseInt(zone.dataset.promoRotate, 10) || DEFAULT_INTERVAL;
            const visibleCount = Math.max(1, parseInt(zone.dataset.promoRotateCount, 10) || 1);
            let start = 0;

            setInterval(() => {
                items.forEach(el => {
                    el.style.display = 'none';
                    el.classList.remove('promo-rotate-visible');
                });

                start = (start + 1) % items.length;

                for (let i = 0; i < visibleCount; i++) {
                    const el = items[(start + i) % items.length];
                    el.style.display = '';
                    // restart the fade-in animation
                    el.classList.remove('promo-rotate-visible');
                    void el.offsetWidth;
                    el.classList.add('promo-rotate-visible');
                    fireImpression(el, zone);
                }
            }, interval);
        });
    }

    function fireImpression(itemEl, zoneEl) {
        const bannerEl = itemEl.querySelector('[data-promo-id]');
        if (!bannerEl) return;

        const payload = JSON.stringify({
            pageSlug: pageSlug(),
            zoneKey: zoneEl.dataset.promoSlot || '',
            culture: document.documentElement.lang || ''
        });
        const url = '/promo/impression/' + encodeURIComponent(bannerEl.dataset.promoId);

        // sendBeacon survives page unload/navigation, so the request is never
        // truncated mid-flight (prevents Kestrel "Unexpected end of request
        // content" errors from beacons aborted on tab close).
        if (navigator.sendBeacon) {
            navigator.sendBeacon(url, new Blob([payload], { type: 'application/json' }));
            return;
        }
        fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: payload,
            keepalive: true
        }).catch(() => { /* tracking must never break the page */ });
    }

    function pageSlug() {
        const p = location.pathname;
        return p === '/' ? 'home' : p.replace(/^\/+|\/+$/g, '');
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
