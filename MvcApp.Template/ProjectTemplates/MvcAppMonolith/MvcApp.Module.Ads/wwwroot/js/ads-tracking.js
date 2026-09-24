// Ads client-side tracking
// Auto-tracks impressions when banners enter viewport, handles click tracking

(function () {
    'use strict';

    const IMPRESSION_ENDPOINT = '/ads/impression/';
    const CLICK_SELECTOR = '.ad-banner-link, .ad-banner-image a, .ad-banner-text a';
    const BANNER_SELECTOR = '[data-ad-banner]';
    const ZONE_SELECTOR = '[data-ad-zone]';

    let impressionSent = new Set();
    let observer = null;

    function init() {
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', init);
            return;
        }
        setupClickTracking();
        setupImpressionTracking();
    }

    function setupClickTracking() {
        document.addEventListener('click', function (e) {
            const link = e.target.closest(CLICK_SELECTOR);
            if (!link) return;

            const bannerEl = link.closest(BANNER_SELECTOR);
            if (!bannerEl) return;

            const bannerId = bannerEl.dataset.adBanner;
            const zoneEl = bannerEl.closest(ZONE_SELECTOR);
            const zoneKey = zoneEl?.dataset.adZone || '';
            const pageSlug = getPageSlug();

            // Click is tracked server-side via the redirect endpoint
            // No client-side action needed here
        });
    }

    function setupImpressionTracking() {
        if (!('IntersectionObserver' in window)) {
            // Fallback: track all visible banners immediately
            trackAllVisible();
            return;
        }

        observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) {
                    const bannerEl = entry.target;
                    const bannerId = bannerEl.dataset.adBanner;
                    if (bannerId && !impressionSent.has(bannerId)) {
                        sendImpression(bannerId, bannerEl);
                    }
                }
            });
        }, {
            root: null,
            rootMargin: '50px',
            threshold: 0.1
        });

        document.querySelectorAll(BANNER_SELECTOR).forEach(function (el) {
            observer.observe(el);
        });
    }

    function trackAllVisible() {
        document.querySelectorAll(BANNER_SELECTOR).forEach(function (el) {
            const bannerId = el.dataset.adBanner;
            if (bannerId && !impressionSent.has(bannerId)) {
                sendImpression(bannerId, el);
            }
        });
    }

    function sendImpression(bannerId, bannerEl) {
        impressionSent.add(bannerId);

        const zoneEl = bannerEl.closest(ZONE_SELECTOR);
        const zoneKey = zoneEl?.dataset.adZone || '';
        const pageSlug = getPageSlug();
        const culture = document.documentElement.lang || 'en';

        fetch(IMPRESSION_ENDPOINT + bannerId, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiForgeryToken()
            },
            body: JSON.stringify({
                pageSlug: pageSlug,
                zoneKey: zoneKey,
                culture: culture
            }),
            credentials: 'same-origin'
        }).catch(function (err) {
            console.warn('Ad impression tracking failed:', err);
        });

        // Stop observing this banner
        if (observer) {
            observer.unobserve(bannerEl);
        }
    }

    function getPageSlug() {
        // Try to get from a meta tag or body data attribute
        const meta = document.querySelector('meta[name="page-slug"]');
        if (meta) return meta.content;
        const body = document.body;
        if (body.dataset.pageSlug) return body.dataset.pageSlug;
        // Fallback: derive from URL
        const path = window.location.pathname;
        const parts = path.split('/').filter(Boolean);
        return parts[parts.length - 1] || 'home';
    }

    function getAntiForgeryToken() {
        const input = document.querySelector('input[name="__RequestVerificationToken"]');
        return input ? input.value : '';
    }

    // Re-init on dynamic content (e.g., Blazor components, AJAX)
    if (typeof MutationObserver !== 'undefined') {
        const mutObserver = new MutationObserver(function (mutations) {
            let hasNewBanners = false;
            mutations.forEach(function (mutation) {
                mutation.addedNodes.forEach(function (node) {
                    if (node.nodeType === 1) { // Element
                        if (node.matches(BANNER_SELECTOR) || node.querySelector(BANNER_SELECTOR)) {
                            hasNewBanners = true;
                        }
                    }
                });
            });
            if (hasNewBanners) {
                setupImpressionTracking();
            }
        });
        mutObserver.observe(document.body, { childList: true, subtree: true });
    }

    init();
})();