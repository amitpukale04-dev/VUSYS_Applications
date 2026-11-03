let deferredPrompt = null;

const APPHUB_INSTALLED_KEY = "VUSYS_APPHUB_INSTALLED";

function getInstallButton() {
    return document.getElementById("btnInstallAppHub");
}

function showAppHubToast(message, type = "success") {
    let toast = document.getElementById("appHubPwaToast");

    if (!toast) {
        toast = document.createElement("div");
        toast.id = "appHubPwaToast";
        toast.className = "apphub-pwa-toast";
        document.body.appendChild(toast);
    }

    toast.className = `apphub-pwa-toast ${type}`;
    toast.innerHTML = `<i class="bi ${type === "success" ? "bi-check-circle-fill" : "bi-info-circle-fill"}"></i><span>${message}</span>`;

    setTimeout(() => toast.classList.add("show"), 50);

    setTimeout(() => {
        toast.classList.remove("show");
    }, 3200);
}

function isAppInstalled() {
    return localStorage.getItem(APPHUB_INSTALLED_KEY) === "true";
}

function markAppInstalled() {
    localStorage.setItem(APPHUB_INSTALLED_KEY, "true");
}

function hideInstallButton() {
    const btn = getInstallButton();

    if (!btn) return;

    btn.classList.remove("apphub-install-visible", "install-pulse");
    btn.classList.add("apphub-install-hidden");
}

function showInstallButton() {
    const btn = getInstallButton();

    if (!btn) return;

    btn.classList.remove("apphub-install-hidden");
    btn.classList.add("apphub-install-visible", "install-pulse");
}

function applyInstallButtonVisibility() {
    if (isAppInstalled()) {
        hideInstallButton();
        return;
    }

    if (deferredPrompt) {
        showInstallButton();
        return;
    }

    hideInstallButton();
}

window.addEventListener("load", function () {
    applyInstallButtonVisibility();
});

window.addEventListener("beforeinstallprompt", function (event) {
    event.preventDefault();
    deferredPrompt = event;
    applyInstallButtonVisibility();
});

window.addEventListener("appinstalled", function () {
    markAppInstalled();
    deferredPrompt = null;
    hideInstallButton();
    showAppHubToast("App Hub installed successfully.", "success");
});

window.appHubPwaInstall = {
    install: async function () {
        if (isAppInstalled()) {
            hideInstallButton();
            return;
        }

        if (!deferredPrompt) {
            showAppHubToast("Install is not available right now. Try browser menu → Install App.", "info");
            return;
        }

        deferredPrompt.prompt();

        const choiceResult = await deferredPrompt.userChoice;

        if (choiceResult && choiceResult.outcome === "accepted") {
            markAppInstalled();
            showAppHubToast("App Hub installed successfully.", "success");
        }

        deferredPrompt = null;
        applyInstallButtonVisibility();
    },

    refreshUI: function () {
        applyInstallButtonVisibility();
    },

    resetInstallStatus: function () {
        localStorage.removeItem(APPHUB_INSTALLED_KEY);
        deferredPrompt = null;
        applyInstallButtonVisibility();
    }
};