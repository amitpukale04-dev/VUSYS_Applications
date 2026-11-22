import { registerSW } from "virtual:pwa-register";

/**
 * Registers the TimePro service worker immediately so the application
 * satisfies the browser PWA installability requirements on the Login page
 * as well as after authentication.
 */
export const registerTimeProPwa = () => {
  if (!("serviceWorker" in navigator)) {
    return;
  }

  registerSW({
    immediate: true,
    onRegisteredSW(_serviceWorkerUrl, registration) {
      if (!registration) return;

      // Keep the installed PWA up to date while preserving the existing
      // autoUpdate behaviour configured in vite.config.js.
      registration.update().catch(() => {
        // Registration/update failures are intentionally silent here.
        // They can still be inspected from DevTools > Application.
      });
    },
  });
};

export default registerTimeProPwa;
