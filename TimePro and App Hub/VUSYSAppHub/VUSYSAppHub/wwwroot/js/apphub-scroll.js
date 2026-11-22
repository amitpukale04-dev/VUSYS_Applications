window.appHubScroll = (function () {
    let scrollHandler = null;
    let dotNetRef = null;

    function getScrollTop() {
        return window.pageYOffset
            || document.documentElement.scrollTop
            || document.body.scrollTop
            || 0;
    }

    function notifyVisibility() {
        if (!dotNetRef) return;

        const shouldShow = getScrollTop() > 220;
        dotNetRef.invokeMethodAsync('UpdateScrollToTopVisibility', shouldShow);
    }

    return {
        registerScrollListener: function (ref) {
            dotNetRef = ref;

            if (scrollHandler) {
                window.removeEventListener('scroll', scrollHandler);
            }

            scrollHandler = function () {
                notifyVisibility();
            };

            window.addEventListener('scroll', scrollHandler, { passive: true });
            notifyVisibility();
        },

        unregisterScrollListener: function () {
            if (scrollHandler) {
                window.removeEventListener('scroll', scrollHandler);
                scrollHandler = null;
            }

            dotNetRef = null;
        },

        scrollToTop: function () {
            window.scrollTo({
                top: 0,
                behavior: 'smooth'
            });
        }
    };
})();




/* ======================================================
   OTP Helper for Deployment Version Verification
   ====================================================== */

window.appHubOtp = (function () {

    function storeOtp(key, payload) {
        if (!key) return;

        try {
            localStorage.setItem(key, JSON.stringify(payload));
        }
        catch (err) {
            console.error("Failed to store OTP in localStorage", err);
        }
    }

    function getOtp(key) {
        if (!key) return null;

        try {
            const raw = localStorage.getItem(key);

            if (!raw)
                return null;

            return JSON.parse(raw);
        }
        catch (err) {
            console.error("Failed to read OTP from localStorage", err);
            return null;
        }
    }

    function removeOtp(key) {
        if (!key) return;

        try {
            localStorage.removeItem(key);
        }
        catch (err) {
            console.error("Failed to remove OTP from localStorage", err);
        }
    }

    function focusOtpBox(elementId) {
        if (!elementId)
            return;

        const element = document.getElementById(elementId);

        if (!element)
            return;

        setTimeout(function () {
            element.focus();

            if (element.select) {
                element.select();
            }
        }, 10);
    }

    return {
        storeOtp: storeOtp,
        getOtp: getOtp,
        removeOtp: removeOtp,
        focusOtpBox: focusOtpBox
    };

})();