const CACHE_VERSION = "v6"; // Increment on every deployment
const CACHE_NAME = `vusys-apphub-${CACHE_VERSION}`;

const OFFLINE_URL = "/offline";

const STATIC_ASSETS = [
    "/offline",
    "/images/apphub-icon-192.png",
    "/images/apphub-icon-512.png",

    "/lib/bootstrap/css/bootstrap.min.css",
    "/lib/bootstrap-icons/font/bootstrap-icons.css",
    "/css/styles.css",

    "/lib/bootstrap/js/bootstrap.bundle.min.js",
    "/js/theme.js",
    "/js/apphub-scroll.js",
    "/js/apphub-pdf-viewer.js",
    "/js/apphub-install.js",
    "/js/apphub-refresh.js",
    "/js/apphub-splash.js"
];

const NEVER_CACHE_PATHS = [
    "/_blazor",
    "/_framework",
    "/api"
];

const NEVER_CACHE_FILES = [
    "/service-worker.js",
    "/js/service-worker-register.js",
    "/_framework/blazor.web.js",
    "/blazor.web.js"
];

function shouldBypassCache(url) {
    if (url.origin !== self.location.origin) {
        return true;
    }

    if (NEVER_CACHE_PATHS.some(path => url.pathname.startsWith(path))) {
        return true;
    }

    if (NEVER_CACHE_FILES.some(file => url.pathname === file || url.pathname.endsWith(file))) {
        return true;
    }

    if (
        url.pathname.includes("browserLink") ||
        url.pathname.includes("hotreload") ||
        url.pathname.includes("_framework/aspnetcore-browser-refresh.js")
    ) {
        return true;
    }

    return false;
}

// INSTALL
self.addEventListener("install", (event) => {
    console.log("Installing Service Worker:", CACHE_NAME);

    event.waitUntil(
        caches.open(CACHE_NAME)
            .then((cache) => cache.addAll(STATIC_ASSETS))
            .catch((error) => {
                console.error("App Hub service worker install cache failed:", error);
            })
    );

    self.skipWaiting();
});

// FETCH
self.addEventListener("fetch", (event) => {
    if (event.request.method !== "GET") return;

    const url = new URL(event.request.url);

    if (shouldBypassCache(url)) {
        event.respondWith(fetch(event.request));
        return;
    }

    // Navigation pages: always network first
    if (event.request.mode === "navigate") {
        event.respondWith(
            fetch(event.request)
                .catch(() => caches.match(OFFLINE_URL))
        );
        return;
    }

    // Static files: cache first, then network
    event.respondWith(
        caches.match(event.request)
            .then((cached) => {
                if (cached) return cached;

                return fetch(event.request)
                    .then((network) => {
                        if (!network || network.status !== 200 || network.type !== "basic") {
                            return network;
                        }

                        const clone = network.clone();

                        caches.open(CACHE_NAME)
                            .then((cache) => cache.put(event.request, clone))
                            .catch((error) => {
                                console.error("App Hub runtime cache failed:", error);
                            });

                        return network;
                    })
                    .catch(() => caches.match(OFFLINE_URL));
            })
    );
});

// ACTIVATE
self.addEventListener("activate", (event) => {
    console.log("Activating Service Worker:", CACHE_NAME);

    event.waitUntil(
        caches.keys().then((keys) =>
            Promise.all(
                keys
                    .filter((key) => key !== CACHE_NAME)
                    .map((key) => caches.delete(key))
            )
        ).then(() => self.clients.claim())
    );
});

// MESSAGE
self.addEventListener("message", (event) => {
    if (!event.data) return;

    if (event.data.action === "skipWaiting" || event.data.type === "SKIP_WAITING") {
        console.log("Skipping waiting and activating new SW");
        self.skipWaiting();
    }
});