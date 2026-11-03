import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import { VitePWA } from "vite-plugin-pwa";

export default defineConfig({
  plugins: [
    react(),
    VitePWA({
      registerType: "autoUpdate",
      injectRegister: null,
      includeAssets: ["favicon.svg", "icons/icon-192.png", "icons/icon-192-maskable.png", "icons/icon-512.png", "icons/icon-512-maskable.png", "icons/apple-touch-icon.png"],
      manifest: false,
      workbox: {
        navigateFallback: "/index.html",
        cleanupOutdatedCaches: true,
        runtimeCaching: [
          {
            urlPattern: ({ request, url }) =>
              request.mode === "navigate" && url.origin === self.location.origin,
            handler: "NetworkFirst",
            options: {
              cacheName: "vusys-time-pro-pages",
              networkTimeoutSeconds: 3,
            },
          },
          {
            urlPattern: ({ request }) =>
              ["style", "script", "font", "image"].includes(
                request.destination,
              ),
            handler: "StaleWhileRevalidate",
            options: { cacheName: "vusys-time-pro-assets" },
          },
        ]
      }
    })
  ]
});
