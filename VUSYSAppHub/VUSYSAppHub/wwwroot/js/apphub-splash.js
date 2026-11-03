(function () {
    function hideAppHubSplash() {
        const splash = document.getElementById("apphub-splash");

        if (!splash) {
            return;
        }

        splash.classList.add("hide");

        setTimeout(function () {
            if (splash && splash.parentNode) {
                splash.parentNode.removeChild(splash);
            }
        }, 450);
    }

    function scheduleHide() {
        setTimeout(hideAppHubSplash, 900);
        setTimeout(hideAppHubSplash, 1600);
        setTimeout(hideAppHubSplash, 2500);
    }

    window.appHubSplash = {
        hide: hideAppHubSplash,
        hideWithDelay: scheduleHide
    };

    document.addEventListener("DOMContentLoaded", scheduleHide);
    window.addEventListener("load", scheduleHide);

    // Blazor enhanced navigation event
    document.addEventListener("enhancedload", scheduleHide);

    // Extra safety: if Blazor inserts splash again during navigation, hide it
    const observer = new MutationObserver(function () {
        const splash = document.getElementById("apphub-splash");

        if (splash) {
            scheduleHide();
        }
    });

    observer.observe(document.documentElement, {
        childList: true,
        subtree: true
    });

    scheduleHide();
})();