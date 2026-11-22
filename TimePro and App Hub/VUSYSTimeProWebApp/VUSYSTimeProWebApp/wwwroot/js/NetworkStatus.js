(function () {
    document.addEventListener("DOMContentLoaded", function () {
        const isOffline = () => !navigator.onLine;

        // Show offline banner immediately if already offline
        if (isOffline()) {
            showSlowNetworkWarning("🚫 You are offline.");
            return;
        }

        // Detect slow network if connection API is supported
        if ('connection' in navigator) {
            const connection = navigator.connection || navigator.mozConnection || navigator.webkitConnection;

            checkNetworkStatus(); // Initial check

            // Watch for network speed changes
            connection.addEventListener('change', checkNetworkStatus);

            function checkNetworkStatus() {
                const effectiveType = connection.effectiveType || '';
                const downlink = connection.downlink || 10;

                console.log("EffectiveType:", effectiveType, "| Downlink:", downlink);

                if ((effectiveType === 'slow-2g' || effectiveType === '2g') || downlink < 0.5) {
                    showSlowNetworkWarning("⚠️ Slow internet connection detected.");
                } else {
                    hideSlowNetworkWarning();
                }
            }
        }

        // Handle online/offline events
        window.addEventListener('offline', () => {
            showSlowNetworkWarning("🚫 You are offline.");
        });

        window.addEventListener('online', () => {
            hideSlowNetworkWarning();
        });

        function showSlowNetworkWarning(message) {
            if (document.getElementById('slow-network-warning')) return;

            const banner = document.createElement('div');
            banner.id = 'slow-network-warning';
            banner.innerText = message;
            Object.assign(banner.style, {
                position: 'fixed',
                top: '0',
                left: '0',              // ensure alignment starts from left
                right: '0',             // ensure it stretches to right
                width: '100vw',         // full viewport width
                backgroundColor: '#d4a017',
                color: '#1a1a1a',
                padding: '12px',
                margin: '0',
                textAlign: 'center',
                fontWeight: 'bold',
                zIndex: '9999',
                boxSizing: 'border-box', // ensure padding doesn't overflow width
            });

            document.body.prepend(banner);
        }

        function hideSlowNetworkWarning() {
            const banner = document.getElementById('slow-network-warning');
            if (banner) banner.remove();
        }
    });
})();
