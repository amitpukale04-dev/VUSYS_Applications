const CACHE_VERSION = "v15" // Change this version when updating
const CACHE_NAME = `vusys-timepro-${CACHE_VERSION}`;

const STATIC_ASSETS = [
    /*'/Offline',*/
    '/Offline/Offline', // Offline page
    "/font-awesome/css/all.min.css",
    "/font-awesome/css/fontawesome.min.css",
    "/Admin/admin-lte/css/adminlte.css",
    "/Admin/summernote/summernote.css",
    "/Admin/select2/css/select2.css",
    "/Admin/datatables/css/dataTables.bootstrap4.min.css",
    "/Admin/toastr/toastr.css",
    "/Admin/CustomStyle.css",
    "/Admin/CustomHeader.css",
    "/css/splash.css",
    "/Admin/LoginChangePassword.css",
    "/Admin/ForgotPassword.css",
    "/Admin/OTPAuthentication.css",
    "/Admin/CustomJS/CommonEvents.js",
    "/lib/jquery/dist/jquery.min.js",
    "/Admin/select2/js/select2.min.js",
    "/lib/jquery/dist/jquery.blockUI.js",
    "/lib/jquery/dist/jquery.noty.packaged.min.js",
    "/Admin/datatables/js/jquery.dataTables.min.js",
    "/Admin/datatables/js/dataTables.bootstrap4.min.js",
    "/Admin/bootstrap/dist/js/bootstrap.bundle.min.js",
    "/Admin/bootbox.js/bootbox.min.js",
    "/lib/jquery-validation/dist/jquery.validate.min.js",
    "/lib/jquery-validation/dist/additional-methods.min.js",
    "/lib/jquery-validation-unobtrusive/jquery.validate.unobtrusive.min.js",
    "/Admin/admin-lte/js/adminlte.js",
    "/Admin/toastr/toastr.min.js",
    "/Admin/CustomJS/ShowNotification.js",
    "/Admin/summernote/summernote.js",
    "/Admin/CustomJS/PreventUserBack.js",
    "/Admin/CustomJS/FormPlugins/UserLogin.js",
    "/Admin/CustomJS/FormPlugins/TimesheetApprovalRejection.js",
    "/Admin/CustomJS/FormPlugins/Timesheet.js",
    "/Admin/CustomJS/FormPlugins/EmployeeProjectMapping.js",
    "/Admin/CustomJS/FormPlugins/TimesheetSubmission.js",
    "/Admin/CustomJS/FormPlugins/OTPAuthentication.js",
    "/js/NetworkStatus.js",

    /*"/js/splash.js",*/
    '/Icons/192x192.png',
    '/Icons/512x512.png',
    '/Icons/192x192_Maskable.png',
    '/Icons/512x512_Maskable.png'
];



//// Pre-cache static assets
//self.addEventListener('install', (event) => {
//    event.waitUntil(
//        caches.open(CACHE_NAME).then((cache) => cache.addAll(ASSETS_TO_CACHE))
//    );
//});

//// Intercept fetch requests
//self.addEventListener('fetch', (event) => {
//    if (event.request.mode === 'navigate') {
//        // Try fetching from the network first, then fallback to cache
//        event.respondWith(
//            fetch(event.request)
//                .then((response) => {
//                    // Store a copy in cache dynamically
//                    return caches.open(CACHE_NAME).then((cache) => {
//                        cache.put(event.request, response.clone());
//                        return response;
//                    });
//                })
//                .catch(() => caches.match(event.request) || caches.match('/Offline'))
//        );
//    } else {
//        // Cache-first strategy for static assets
//        event.respondWith(
//            caches.match(event.request).then((response) => {
//                return response || fetch(event.request).then((fetchResponse) => {
//                    return caches.open(CACHE_NAME).then((cache) => {
//                        cache.put(event.request, fetchResponse.clone());
//                        return fetchResponse;
//                    });
//                });
//            })
//        );
//    }
//});

//// Cleanup old caches
//self.addEventListener('activate', (event) => {
//    event.waitUntil(
//        caches.keys().then((keys) =>
//            Promise.all(keys.filter((key) => key !== CACHE_NAME).map((key) => caches.delete(key)))
//        )
//    );
//});

//// Install event - Cache Static Assets
//self.addEventListener('install', (event) => {
//    event.waitUntil(
//        caches.open(CACHE_NAME).then((cache) => {
//            return cache.addAll(STATIC_ASSETS);
//        })
//    );
//    self.skipWaiting(); // Activate new service worker immediately
//});

//// Fetch event - Dynamic caching for pages & assets
//self.addEventListener('fetch', (event) => {
//    if (event.request.mode === 'navigate') {
//        event.respondWith(
//            fetch(event.request)
//                .then((response) => {
//                    return caches.open(CACHE_NAME).then((cache) => {
//                        cache.put(event.request, response.clone());
//                        return response;
//                    });
//                })
//                .catch(() => caches.match(event.request) || caches.match('/Offline/Offline'))
//        );
//    } else {
//        event.respondWith(
//            caches.match(event.request).then((response) => {
//                return response || fetch(event.request).then((fetchResponse) => {
//                    return caches.open(CACHE_NAME).then((cache) => {
//                        cache.put(event.request, fetchResponse.clone());
//                        return fetchResponse;
//                    });
//                });
//            })
//        );
//    }
//});

//// Activate event - Remove old caches & force refresh
//self.addEventListener('activate', (event) => {
//    event.waitUntil(
//        caches.keys().then((keys) =>
//            Promise.all(
//                keys
//                    .filter((key) => key !== CACHE_NAME) // Delete old caches
//                    .map((key) => caches.delete(key))
//            )
//        )
//    );
//    self.clients.claim(); // Claim control over all pages immediately
//});

//self.addEventListener('message', (event) => {
//    if (event.data && event.data.action === 'skipWaiting') {
//        self.skipWaiting();
//    }
//});

// Install event - Cache Static Assets
self.addEventListener('install', (event) => {
    event.waitUntil(
        caches.open(CACHE_NAME).then((cache) => cache.addAll(STATIC_ASSETS))
    );
    self.skipWaiting(); // Activate new service worker immediately
});

// Fetch event - Handle Navigation Requests and Asset Caching
self.addEventListener('fetch', (event) => {
    if (event.request.method !== 'GET') {
        return; // Ignore non-GET requests (e.g., POST, PUT, DELETE)
    }

    if (event.request.mode === 'navigate') {
        // Handle page navigation (e.g., clicking links)
        event.respondWith(
            fetch(event.request)
                .then((response) => {
                    return caches.open(CACHE_NAME).then((cache) => {
                        cache.put(event.request, response.clone());
                        return response;
                    });
                })
                .catch(() => caches.match('/Offline/Offline'))
        );
    } else {
        // Cache static assets
        event.respondWith(
            caches.match(event.request).then((response) => {
                return response || fetch(event.request).then((fetchResponse) => {
                    return caches.open(CACHE_NAME).then((cache) => {
                        cache.put(event.request, fetchResponse.clone());
                        return fetchResponse;
                    });
                });
            })
        );
    }
});

// Activate event - Remove old caches & force refresh
self.addEventListener('activate', (event) => {
    event.waitUntil(
        caches.keys().then((keys) =>
            Promise.all(
                keys
                    .filter((key) => key !== CACHE_NAME) // Delete old caches
                    .map((key) => caches.delete(key))
            )
        )
    );
    self.clients.claim(); // Claim control over all pages immediately
});

// Listen for messages from the main thread (skipWaiting)
self.addEventListener('message', (event) => {
    if (event.data && event.data.action === 'skipWaiting') {
        self.skipWaiting();
    }
});


