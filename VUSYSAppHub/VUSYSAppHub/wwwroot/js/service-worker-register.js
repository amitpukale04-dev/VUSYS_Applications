(function () {
    if (!("serviceWorker" in navigator)) {
        console.warn("Service Worker is not supported in this browser.");
        return;
    }

    let isReloading = false;

    function showUpdateToast() {
        if (window.appHubToast && window.appHubToast.show) {
            window.appHubToast.show("New version available. Updating App Hub...", "info");
        }
    }

    window.addEventListener("load", function () {
        navigator.serviceWorker.register("/service-worker.js", { scope: "/" })
            .then((registration) => {
                console.log("App Hub Service Worker Registered:", registration.scope);

                // Check for update immediately
                registration.update();

                // If already waiting (refresh scenario)
                if (registration.waiting && navigator.serviceWorker.controller) {
                    console.log("New version already waiting");

                    showUpdateToast();

                    setTimeout(() => {
                        registration.waiting.postMessage({ action: "skipWaiting" });
                    }, 800);
                }

                // Detect new SW
                registration.onupdatefound = () => {
                    const newWorker = registration.installing;

                    if (!newWorker) return;

                    newWorker.onstatechange = () => {
                        if (
                            newWorker.state === "installed" &&
                            navigator.serviceWorker.controller
                        ) {
                            console.log("New App Hub version found");

                            showUpdateToast();

                            setTimeout(() => {
                                newWorker.postMessage({ action: "skipWaiting" });
                            }, 800);
                        }
                    };
                };
            })
            .catch((err) => {
                console.error("App Hub Service Worker Registration Failed:", err);
            });

        // Reload ONCE after activation
        navigator.serviceWorker.addEventListener("controllerchange", () => {
            if (isReloading) return;

            isReloading = true;

            console.log("New service worker activated → Reloading...");

            setTimeout(() => {
                window.location.reload();
            }, 600);
        });
    });
})();