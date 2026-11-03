import * as pdfjsLib from "/js/pdfjs/pdf.mjs";

pdfjsLib.GlobalWorkerOptions.workerSrc = "/js/pdfjs/pdf.worker.mjs";

const appHubPdfViewerStore = new Map();

function clamp(value, min, max) {
    return Math.min(Math.max(value, min), max);
}

function debounce(func, wait) {
    let timeoutId = null;

    return (...args) => {
        if (timeoutId) {
            clearTimeout(timeoutId);
        }

        timeoutId = setTimeout(() => {
            func(...args);
        }, wait);
    };
}

function normalizeBase64(base64Value) {
    return (base64Value || "")
        .replace(/^data:application\/pdf;base64,/i, "")
        .replace(/\s/g, "");
}

function base64ToUint8Array(base64Value) {
    const normalizedBase64 = normalizeBase64(base64Value);
    return Uint8Array.from(atob(normalizedBase64), c => c.charCodeAt(0));
}

function buildBlockedKeyboardHandler() {
    const blockedKeys = new Set(["p", "s", "u", "c"]);

    return function (e) {
        const key = (e.key || "").toLowerCase();

        if (
            e.key === "PrintScreen" ||
            e.key === "F12" ||
            ((e.ctrlKey || e.metaKey) && blockedKeys.has(key))
        ) {
            e.preventDefault();
            e.stopPropagation();
        }
    };
}

function destroyViewer(containerId) {
    const existing = appHubPdfViewerStore.get(containerId);
    if (!existing) {
        return;
    }

    try {
        if (existing.intersectionObserver) {
            existing.intersectionObserver.disconnect();
        }
    } catch {
    }

    try {
        if (existing.resizeHandler) {
            window.removeEventListener("resize", existing.resizeHandler);
        }
    } catch {
    }

    try {
        if (existing.scrollHandler && existing.scrollHost) {
            existing.scrollHost.removeEventListener("scroll", existing.scrollHandler);
        }
    } catch {
    }

    try {
        if (existing.wrapper && existing.keydownHandler) {
            existing.wrapper.removeEventListener("keydown", existing.keydownHandler);
        }
    } catch {
    }

    try {
        if (existing.scrollHost && existing.contextMenuHandler) {
            existing.scrollHost.removeEventListener("contextmenu", existing.contextMenuHandler);
        }
    } catch {
    }

    try {
        if (existing.scrollHost && existing.dragStartHandler) {
            existing.scrollHost.removeEventListener("dragstart", existing.dragStartHandler);
        }
    } catch {
    }

    appHubPdfViewerStore.delete(containerId);
}

function createButton(className, title, ariaLabel, html) {
    const button = document.createElement("button");
    button.type = "button";
    button.className = className;
    button.title = title;
    button.setAttribute("aria-label", ariaLabel);
    button.innerHTML = html;
    return button;
}

function buildViewerShell() {
    const wrapper = document.createElement("div");
    wrapper.className = "apphub-pdf-secure-wrapper apphub-pdf-viewer-shell";
    wrapper.tabIndex = 0;

    const toolbar = document.createElement("div");
    toolbar.className = "apphub-pdf-secure-toolbar apphub-pdf-toolbar";

    const toolbarLeft = document.createElement("div");
    toolbarLeft.className = "apphub-pdf-toolbar-left apphub-pdf-toolbar-group";

    const toolbarRight = document.createElement("div");
    toolbarRight.className = "apphub-pdf-toolbar-right apphub-pdf-toolbar-group";

    toolbar.appendChild(toolbarLeft);
    toolbar.appendChild(toolbarRight);

    const body = document.createElement("div");
    body.className = "apphub-pdf-body";

    const thumbnails = document.createElement("aside");
    thumbnails.className = "apphub-pdf-thumbnails";

    const main = document.createElement("section");
    main.className = "apphub-pdf-main";

    const scrollHost = document.createElement("div");
    scrollHost.className = "apphub-pdf-canvas-container apphub-pdf-scroll-host";
    scrollHost.tabIndex = 0;

    const viewport = document.createElement("div");
    viewport.className = "apphub-pdf-pages-viewport";

    const topSpacer = document.createElement("div");
    topSpacer.className = "apphub-pdf-top-spacer";

    const pages = document.createElement("div");
    pages.className = "apphub-pdf-pages";

    const bottomSpacer = document.createElement("div");
    bottomSpacer.className = "apphub-pdf-bottom-spacer";

    viewport.appendChild(topSpacer);
    viewport.appendChild(pages);
    viewport.appendChild(bottomSpacer);

    scrollHost.appendChild(viewport);
    main.appendChild(scrollHost);

    body.appendChild(thumbnails);
    body.appendChild(main);

    wrapper.appendChild(toolbar);
    wrapper.appendChild(body);

    return {
        wrapper,
        toolbar,
        toolbarLeft,
        toolbarRight,
        body,
        thumbnails,
        main,
        scrollHost,
        viewport,
        topSpacer,
        pages,
        bottomSpacer
    };
}

async function ensurePageMetric(instance, pageNumber) {
    if (instance.pageMetrics.has(pageNumber)) {
        return instance.pageMetrics.get(pageNumber);
    }

    const page = await instance.pdf.getPage(pageNumber);
    const viewport = page.getViewport({ scale: 1 });

    const metric = {
        width: viewport.width,
        height: viewport.height
    };

    instance.pageMetrics.set(pageNumber, metric);
    return metric;
}

function getComputedScale(instance, baseWidth, baseHeight) {
    const availableWidth = Math.max(instance.scrollHost.clientWidth - 48, 320);
    const availableHeight = Math.max(instance.scrollHost.clientHeight - 48, 220);

    if (instance.zoomMode === "fit-height") {
        return clamp(availableHeight / baseHeight, instance.minScale, instance.maxScale);
    }

    if (instance.zoomMode === "actual") {
        return clamp(instance.scale, instance.minScale, instance.maxScale);
    }

    return clamp(availableWidth / baseWidth, instance.minScale, instance.maxScale);
}

async function updateComputedScale(instance) {
    const metric = await ensurePageMetric(instance, 1);
    instance.currentRenderScale = getComputedScale(instance, metric.width, metric.height);
}

async function buildPageLayouts(instance) {
    await updateComputedScale(instance);

    instance.pageLayouts = [];
    let runningTop = 0;

    for (let pageNumber = 1; pageNumber <= instance.totalPages; pageNumber++) {
        const metric = await ensurePageMetric(instance, pageNumber);

        const layout = {
            pageNumber,
            width: metric.width * instance.currentRenderScale,
            height: metric.height * instance.currentRenderScale,
            top: runningTop,
            bottom: runningTop + (metric.height * instance.currentRenderScale)
        };

        instance.pageLayouts.push(layout);
        runningTop += layout.height + instance.pageGap;
    }

    instance.totalScrollableHeight = Math.max(runningTop - instance.pageGap, 0);
}

function getPageLayout(instance, pageNumber) {
    return instance.pageLayouts.find(x => x.pageNumber === pageNumber) || null;
}

function getVisibleRange(instance) {
    const scrollTop = instance.scrollHost.scrollTop;
    const viewportHeight = instance.scrollHost.clientHeight;
    const buffer = viewportHeight * instance.virtualBufferMultiplier;

    const startY = Math.max(scrollTop - buffer, 0);
    const endY = scrollTop + viewportHeight + buffer;

    let startPage = 1;
    let endPage = instance.totalPages;

    for (const layout of instance.pageLayouts) {
        if (layout.bottom >= startY) {
            startPage = layout.pageNumber;
            break;
        }
    }

    for (const layout of instance.pageLayouts) {
        if (layout.top <= endY) {
            endPage = layout.pageNumber;
        } else {
            break;
        }
    }

    return {
        startPage: clamp(startPage, 1, instance.totalPages),
        endPage: clamp(endPage, 1, instance.totalPages)
    };
}

function getCurrentPageFromScroll(instance) {
    const middle = instance.scrollHost.scrollTop + (instance.scrollHost.clientHeight / 2);
    let currentPage = 1;

    for (const layout of instance.pageLayouts) {
        if (middle >= layout.top && middle <= layout.bottom) {
            currentPage = layout.pageNumber;
            break;
        }

        if (middle > layout.bottom) {
            currentPage = layout.pageNumber;
        }
    }

    return clamp(currentPage, 1, instance.totalPages);
}

function updateToolbarState(instance) {
    if (!instance) return;

    if (instance.pageNumberInput) {
        instance.pageNumberInput.value = instance.currentPage.toString();
    }

    if (instance.pageCountText) {
        instance.pageCountText.textContent = `of ${instance.totalPages}`;
    }

    if (instance.zoomValueText) {
        if (instance.zoomMode === "fit-width") {
            instance.zoomValueText.textContent = "Fit Width";
        } else if (instance.zoomMode === "fit-height") {
            instance.zoomValueText.textContent = "Fit Height";
        } else {
            instance.zoomValueText.textContent = `${Math.round(instance.currentRenderScale * 100)}%`;
        }
    }

    if (instance.zoomPresetSelect) {
        instance.zoomPresetSelect.value = instance.zoomMode;
    }

    if (instance.prevButton) {
        instance.prevButton.disabled = instance.currentPage <= 1;
    }

    if (instance.nextButton) {
        instance.nextButton.disabled = instance.currentPage >= instance.totalPages;
    }

    if (instance.fitWidthButton) {
        instance.fitWidthButton.classList.toggle("active", instance.zoomMode === "fit-width");
    }

    if (instance.fitHeightButton) {
        instance.fitHeightButton.classList.toggle("active", instance.zoomMode === "fit-height");
    }

    if (instance.actualSizeButton) {
        instance.actualSizeButton.classList.toggle("active", instance.zoomMode === "actual");
    }

    if (instance.thumbnailButtons?.length) {
        for (const thumb of instance.thumbnailButtons) {
            const pageNumber = parseInt(thumb.getAttribute("data-page-number") || "1", 10);
            thumb.classList.toggle("active", pageNumber === instance.currentPage);
        }
    }
}

function scrollToPage(instance, pageNumber, smooth = true) {
    const safePage = clamp(pageNumber, 1, instance.totalPages);
    const layout = getPageLayout(instance, safePage);

    if (!layout) {
        return;
    }

    instance.currentPage = safePage;
    updateToolbarState(instance);

    instance.scrollHost.scrollTo({
        top: Math.max(layout.top - 8, 0),
        behavior: smooth ? "smooth" : "auto"
    });
}

async function renderPageToCanvas(instance, pageNumber, canvas, force = false) {
    const renderKey = `${pageNumber}_${instance.currentRenderScale.toFixed(4)}`;
    const existingKey = instance.renderedCanvasKeys.get(pageNumber);

    if (!force && existingKey === renderKey) {
        return;
    }

    const page = await instance.pdf.getPage(pageNumber);
    const viewport = page.getViewport({ scale: instance.currentRenderScale });

    const outputScale = window.devicePixelRatio || 1;
    const context = canvas.getContext("2d", { alpha: false });

    canvas.width = Math.ceil(viewport.width * outputScale);
    canvas.height = Math.ceil(viewport.height * outputScale);
    canvas.style.width = `${Math.ceil(viewport.width)}px`;
    canvas.style.height = `${Math.ceil(viewport.height)}px`;

    context.setTransform(outputScale, 0, 0, outputScale, 0, 0);

    await page.render({
        canvasContext: context,
        viewport: viewport
    }).promise;

    instance.renderedCanvasKeys.set(pageNumber, renderKey);
}

async function renderVirtualPages(instance, force = false) {
    const range = getVisibleRange(instance);

    if (
        !force &&
        instance.lastRenderedRange &&
        instance.lastRenderedRange.startPage === range.startPage &&
        instance.lastRenderedRange.endPage === range.endPage
    ) {
        return;
    }

    instance.pagesContainer.innerHTML = "";

    const startLayout = getPageLayout(instance, range.startPage);
    const endLayout = getPageLayout(instance, range.endPage);

    instance.topSpacer.style.height = `${startLayout ? startLayout.top : 0}px`;
    instance.bottomSpacer.style.height = `${endLayout ? Math.max(instance.totalScrollableHeight - endLayout.bottom, 0) : 0}px`;

    for (let pageNumber = range.startPage; pageNumber <= range.endPage; pageNumber++) {
        const pageWrap = document.createElement("div");
        pageWrap.className = "apphub-pdf-page-wrap apphub-pdf-page-card";
        pageWrap.setAttribute("data-page-number", pageNumber.toString());

        const pageLabel = document.createElement("div");
        pageLabel.className = "apphub-pdf-page-label";
        pageLabel.textContent = `Page ${pageNumber} of ${instance.totalPages}`;

        const canvasHolder = document.createElement("div");
        canvasHolder.className = "apphub-pdf-canvas-holder";

        const canvas = document.createElement("canvas");
        canvas.className = "apphub-pdf-canvas apphub-pdf-page-canvas";

        canvasHolder.appendChild(canvas);
        pageWrap.appendChild(pageLabel);
        pageWrap.appendChild(canvasHolder);
        instance.pagesContainer.appendChild(pageWrap);

        await renderPageToCanvas(instance, pageNumber, canvas, force);
    }

    instance.lastRenderedRange = range;
}

async function renderAllPages(instance, force = false) {
    instance.topSpacer.style.height = "0px";
    instance.bottomSpacer.style.height = "0px";
    instance.pagesContainer.innerHTML = "";

    for (let pageNumber = 1; pageNumber <= instance.totalPages; pageNumber++) {
        const pageWrap = document.createElement("div");
        pageWrap.className = "apphub-pdf-page-wrap apphub-pdf-page-card";
        pageWrap.setAttribute("data-page-number", pageNumber.toString());

        const pageLabel = document.createElement("div");
        pageLabel.className = "apphub-pdf-page-label";
        pageLabel.textContent = `Page ${pageNumber} of ${instance.totalPages}`;

        const canvasHolder = document.createElement("div");
        canvasHolder.className = "apphub-pdf-canvas-holder";

        const canvas = document.createElement("canvas");
        canvas.className = "apphub-pdf-canvas apphub-pdf-page-canvas";

        canvasHolder.appendChild(canvas);
        pageWrap.appendChild(pageLabel);
        pageWrap.appendChild(canvasHolder);
        instance.pagesContainer.appendChild(pageWrap);

        await renderPageToCanvas(instance, pageNumber, canvas, force);
    }
}

async function rebuildViewer(instance, preservePage = true, force = true) {
    const previousPage = preservePage ? instance.currentPage : 1;

    await buildPageLayouts(instance);
    instance.lastRenderedRange = null;

    if (instance.enableVirtualScroll) {
        await renderVirtualPages(instance, force);
    } else {
        await renderAllPages(instance, force);
    }

    instance.currentPage = clamp(previousPage, 1, instance.totalPages);
    updateToolbarState(instance);

    requestAnimationFrame(() => {
        scrollToPage(instance, instance.currentPage, false);
    });
}

async function renderThumbnail(instance, pageNumber) {
    const page = await instance.pdf.getPage(pageNumber);
    const viewport = page.getViewport({ scale: 1 });

    const targetWidth = 120;
    const thumbScale = targetWidth / viewport.width;
    const thumbViewport = page.getViewport({ scale: thumbScale });

    const thumbButton = document.createElement("button");
    thumbButton.type = "button";
    thumbButton.className = "apphub-pdf-thumb-card";
    thumbButton.setAttribute("data-page-number", pageNumber.toString());
    thumbButton.setAttribute("aria-label", `Go to page ${pageNumber}`);

    const canvas = document.createElement("canvas");
    canvas.className = "apphub-pdf-thumb-canvas";
    canvas.width = Math.ceil(thumbViewport.width);
    canvas.height = Math.ceil(thumbViewport.height);

    const context = canvas.getContext("2d", { alpha: false });

    await page.render({
        canvasContext: context,
        viewport: thumbViewport
    }).promise;

    const label = document.createElement("div");
    label.className = "apphub-pdf-thumb-label";
    label.textContent = `Page ${pageNumber}`;

    thumbButton.appendChild(canvas);
    thumbButton.appendChild(label);

    thumbButton.addEventListener("click", () => {
        scrollToPage(instance, pageNumber, true);
    });

    instance.thumbnailButtons.push(thumbButton);
    instance.thumbnails.appendChild(thumbButton);
}

async function renderThumbnails(instance) {
    if (!instance.showThumbnails) {
        instance.wrapper.classList.add("apphub-pdf-thumbs-collapsed");
        return;
    }

    instance.thumbnails.innerHTML = "";
    instance.thumbnailButtons = [];

    for (let pageNumber = 1; pageNumber <= instance.totalPages; pageNumber++) {
        await renderThumbnail(instance, pageNumber);
    }

    updateToolbarState(instance);
}

async function applyZoomPreset(instance, mode, preservePage = true) {
    instance.zoomMode = mode;

    if (mode === "actual") {
        instance.scale = clamp(instance.scale || 1, instance.minScale, instance.maxScale);
    }

    instance.renderedCanvasKeys.clear();
    await rebuildViewer(instance, preservePage, true);
}

async function zoomIn(instance) {
    instance.zoomMode = "actual";
    instance.scale = clamp(instance.currentRenderScale + instance.zoomStep, instance.minScale, instance.maxScale);
    instance.renderedCanvasKeys.clear();
    await rebuildViewer(instance, true, true);
}

async function zoomOut(instance) {
    instance.zoomMode = "actual";
    instance.scale = clamp(instance.currentRenderScale - instance.zoomStep, instance.minScale, instance.maxScale);
    instance.renderedCanvasKeys.clear();
    await rebuildViewer(instance, true, true);
}

window.appHubPdfViewer = {
    async renderPdfFromBase64(containerId, base64Value, options = {}) {
        if (!containerId) {
            throw new Error("PDF container id is required.");
        }

        if (!base64Value || !base64Value.trim()) {
            throw new Error("PDF Base64 content is empty.");
        }

        const container = document.getElementById(containerId);

        if (!container) {
            throw new Error(`PDF container '${containerId}' not found.`);
        }

        destroyViewer(containerId);
        container.innerHTML = "";

        const pdfBytes = base64ToUint8Array(base64Value);

        const loadingTask = pdfjsLib.getDocument({
            data: pdfBytes,
            isEvalSupported: false
        });

        const pdf = await loadingTask.promise;

        const parts = buildViewerShell();

        const pageInfo = document.createElement("div");
        pageInfo.className = "apphub-pdf-page-info";
        pageInfo.innerHTML = `
            <span>Total Pages:</span>
            <strong>${pdf.numPages}</strong>
        `;

        const toolbarNote = document.createElement("div");
        toolbarNote.className = "apphub-pdf-toolbar-note";
        toolbarNote.textContent = "Preview only";

        const toggleThumbButton = createButton(
            "apphub-pdf-toolbar-btn apphub-pdf-tool-btn",
            "Show or hide thumbnails",
            "Show or hide thumbnails",
            `<i class="bi bi-layout-sidebar-inset"></i><span>Pages</span>`
        );

        const prevButton = createButton(
            "apphub-pdf-toolbar-btn apphub-pdf-tool-btn",
            "Previous Page",
            "Previous Page",
            `<i class="bi bi-chevron-left"></i>`
        );

        const pageIndicator = document.createElement("div");
        pageIndicator.className = "apphub-pdf-page-indicator";

        const pageNumberInput = document.createElement("input");
        pageNumberInput.type = "number";
        pageNumberInput.className = "apphub-pdf-page-number-input apphub-pdf-page-input";
        pageNumberInput.min = "1";
        pageNumberInput.max = pdf.numPages.toString();
        pageNumberInput.value = "1";
        pageNumberInput.setAttribute("aria-label", "Current Page Number");
        pageNumberInput.title = "Current Page";

        const pageCountText = document.createElement("span");
        pageCountText.className = "apphub-pdf-page-count-text apphub-pdf-page-total";
        pageCountText.textContent = `of ${pdf.numPages}`;

        pageIndicator.appendChild(pageNumberInput);
        pageIndicator.appendChild(pageCountText);

        const nextButton = createButton(
            "apphub-pdf-toolbar-btn apphub-pdf-tool-btn",
            "Next Page",
            "Next Page",
            `<i class="bi bi-chevron-right"></i>`
        );

        const zoomOutButton = createButton(
            "apphub-pdf-toolbar-btn apphub-pdf-tool-btn",
            "Zoom Out",
            "Zoom Out",
            `<i class="bi bi-dash-lg"></i>`
        );

        const zoomValueText = document.createElement("span");
        zoomValueText.className = "apphub-pdf-zoom-value apphub-pdf-zoom-badge";
        zoomValueText.textContent = "Fit Width";

        const zoomInButton = createButton(
            "apphub-pdf-toolbar-btn apphub-pdf-tool-btn",
            "Zoom In",
            "Zoom In",
            `<i class="bi bi-plus-lg"></i>`
        );

        const zoomPresetSelect = document.createElement("select");
        zoomPresetSelect.className = "apphub-pdf-zoom-select";
        zoomPresetSelect.setAttribute("aria-label", "Zoom preset");
        zoomPresetSelect.innerHTML = `
            <option value="fit-width">Fit Width</option>
            <option value="fit-height">Fit Height</option>
            <option value="actual">Actual Size</option>
        `;

        const fitWidthButton = createButton(
            "apphub-pdf-toolbar-btn apphub-pdf-tool-btn apphub-pdf-fit-width-btn",
            "Fit to Width",
            "Fit to Width",
            `<i class="bi bi-arrows-angle-expand me-1"></i><span>Fit Width</span>`
        );

        const fitHeightButton = createButton(
            "apphub-pdf-toolbar-btn apphub-pdf-tool-btn",
            "Fit to Height",
            "Fit to Height",
            `<i class="bi bi-arrows-collapse-vertical me-1"></i><span>Fit Height</span>`
        );

        const actualSizeButton = createButton(
            "apphub-pdf-toolbar-btn apphub-pdf-tool-btn",
            "Actual Size",
            "Actual Size",
            `<i class="bi bi-aspect-ratio me-1"></i><span>Actual</span>`
        );

        const scrollTopButton = createButton(
            "apphub-pdf-toolbar-btn apphub-pdf-tool-btn",
            "Scroll to Top",
            "Scroll to Top",
            `<i class="bi bi-arrow-up"></i><span>Top</span>`
        );

        parts.toolbarLeft.appendChild(toggleThumbButton);
        parts.toolbarLeft.appendChild(pageInfo);
        parts.toolbarLeft.appendChild(toolbarNote);

        parts.toolbarRight.appendChild(prevButton);
        parts.toolbarRight.appendChild(pageIndicator);
        parts.toolbarRight.appendChild(nextButton);
        parts.toolbarRight.appendChild(zoomOutButton);
        parts.toolbarRight.appendChild(zoomValueText);
        parts.toolbarRight.appendChild(zoomInButton);
        parts.toolbarRight.appendChild(zoomPresetSelect);
        parts.toolbarRight.appendChild(fitWidthButton);
        parts.toolbarRight.appendChild(fitHeightButton);
        parts.toolbarRight.appendChild(actualSizeButton);
        parts.toolbarRight.appendChild(scrollTopButton);

        container.appendChild(parts.wrapper);

        const instance = {
            containerId,
            container,
            wrapper: parts.wrapper,
            pdf,
            totalPages: pdf.numPages,
            currentPage: 1,
            zoomMode: options.initialZoomPreset || "fit-width",
            scale: 1,
            currentRenderScale: 1,
            minScale: 0.50,
            maxScale: 3.00,
            zoomStep: 0.10,
            pageGap: 24,
            virtualBufferMultiplier: 1.75,
            enableVirtualScroll: options.enableVirtualScroll !== false,
            enableSecurityHardening: options.enableSecurityHardening !== false,
            showThumbnails: options.showThumbnails !== false,
            thumbnails: parts.thumbnails,
            scrollHost: parts.scrollHost,
            pagesContainer: parts.pages,
            topSpacer: parts.topSpacer,
            bottomSpacer: parts.bottomSpacer,
            toolbar: parts.toolbar,
            pageInfo,
            pageNumberInput,
            pageCountText,
            zoomValueText,
            zoomPresetSelect,
            prevButton,
            nextButton,
            zoomInButton,
            zoomOutButton,
            fitWidthButton,
            fitHeightButton,
            actualSizeButton,
            toggleThumbButton,
            scrollTopButton,
            pageMetrics: new Map(),
            pageLayouts: [],
            renderedCanvasKeys: new Map(),
            thumbnailButtons: [],
            totalScrollableHeight: 0,
            lastRenderedRange: null,
            intersectionObserver: null,
            resizeHandler: null,
            scrollHandler: null,
            keydownHandler: buildBlockedKeyboardHandler(),
            contextMenuHandler: e => e.preventDefault(),
            dragStartHandler: e => e.preventDefault()
        };

        appHubPdfViewerStore.set(containerId, instance);

        if (instance.enableSecurityHardening) {
            instance.wrapper.addEventListener("keydown", instance.keydownHandler);
            instance.scrollHost.addEventListener("contextmenu", instance.contextMenuHandler);
            instance.scrollHost.addEventListener("dragstart", instance.dragStartHandler);
        }

        prevButton.addEventListener("click", () => {
            scrollToPage(instance, instance.currentPage - 1, true);
        });

        nextButton.addEventListener("click", () => {
            scrollToPage(instance, instance.currentPage + 1, true);
        });

        pageNumberInput.addEventListener("change", () => {
            const page = parseInt(pageNumberInput.value || "1", 10);
            scrollToPage(instance, Number.isNaN(page) ? 1 : page, true);
        });

        pageNumberInput.addEventListener("keydown", e => {
            if (e.key === "Enter") {
                e.preventDefault();
                const page = parseInt(pageNumberInput.value || "1", 10);
                scrollToPage(instance, Number.isNaN(page) ? 1 : page, true);
            }
        });

        zoomInButton.addEventListener("click", async () => {
            await zoomIn(instance);
        });

        zoomOutButton.addEventListener("click", async () => {
            await zoomOut(instance);
        });

        fitWidthButton.addEventListener("click", async () => {
            zoomPresetSelect.value = "fit-width";
            await applyZoomPreset(instance, "fit-width", true);
        });

        fitHeightButton.addEventListener("click", async () => {
            zoomPresetSelect.value = "fit-height";
            await applyZoomPreset(instance, "fit-height", true);
        });

        actualSizeButton.addEventListener("click", async () => {
            zoomPresetSelect.value = "actual";
            instance.scale = 1;
            await applyZoomPreset(instance, "actual", true);
        });

        zoomPresetSelect.addEventListener("change", async () => {
            const selectedMode = zoomPresetSelect.value || "fit-width";

            if (selectedMode === "actual") {
                instance.scale = 1;
            }

            await applyZoomPreset(instance, selectedMode, true);
        });

        scrollTopButton.addEventListener("click", () => {
            instance.scrollHost.scrollTo({
                top: 0,
                behavior: "smooth"
            });
        });

        toggleThumbButton.addEventListener("click", () => {
            instance.wrapper.classList.toggle("apphub-pdf-thumbs-collapsed");
        });

        instance.scrollHandler = debounce(async () => {
            const active = appHubPdfViewerStore.get(containerId);
            if (!active) {
                return;
            }

            const visiblePage = getCurrentPageFromScroll(active);
            if (visiblePage !== active.currentPage) {
                active.currentPage = visiblePage;
                updateToolbarState(active);
            }

            if (active.enableVirtualScroll) {
                await renderVirtualPages(active, false);
            }
        }, 12);

        instance.scrollHost.addEventListener("scroll", instance.scrollHandler);

        instance.resizeHandler = debounce(async () => {
            const active = appHubPdfViewerStore.get(containerId);
            if (!active) {
                return;
            }

            if (active.zoomMode === "fit-width" || active.zoomMode === "fit-height") {
                active.renderedCanvasKeys.clear();
                await rebuildViewer(active, true, true);
            }
        }, 180);

        window.addEventListener("resize", instance.resizeHandler);

        await rebuildViewer(instance, false, true);
        await renderThumbnails(instance);

        updateToolbarState(instance);
        instance.scrollHost.focus();
    },

    clear(containerId) {
        if (!containerId) {
            return;
        }

        destroyViewer(containerId);

        const container = document.getElementById(containerId);
        if (container) {
            container.innerHTML = "";
        }
    }
};