(() => {
    "use strict";

    if (window.top !== window) return;
    if (window.__jppsLoaded) return;
    window.__jppsLoaded = true;

    const BUTTON_ID = "jpps-potplayer-button";
    const HDR_NOTICE_ID = "jpps-hdr-notice";
    const FALLBACK_INTERVAL_MS = 1500;
    let updateQueued = false;
    let updateVersion = 0;
    let hdrNoticeTimer = null;

    function removeHdrNotice() {
        if (hdrNoticeTimer !== null) {
            window.clearTimeout(hdrNoticeTimer);
            hdrNoticeTimer = null;
        }

        document.getElementById(HDR_NOTICE_ID)?.remove();
    }

    function showHdrNotice() {
        removeHdrNotice();
        const button = document.getElementById(BUTTON_ID);
        if (!button || !document.body) return;

        const notice = document.createElement("div");
        notice.id = HDR_NOTICE_ID;
        notice.setAttribute("role", "status");
        notice.setAttribute("aria-live", "polite");

        const title = document.createElement("strong");
        title.textContent = "HDR 资源";
        const message = document.createElement("span");
        message.textContent = "建议开启屏幕 HDR 模式观看";
        notice.append(title, message);
        document.body.appendChild(notice);

        const buttonBounds = button.getBoundingClientRect();
        const noticeBounds = notice.getBoundingClientRect();
        let left = buttonBounds.right + 12;
        if (left + noticeBounds.width > window.innerWidth - 12) {
            left = Math.max(12, buttonBounds.left - noticeBounds.width - 12);
        }

        const top = Math.min(
            Math.max(
                12,
                buttonBounds.top +
                    ((buttonBounds.height - noticeBounds.height) / 2)),
            window.innerHeight - noticeBounds.height - 12);
        notice.style.left = `${left}px`;
        notice.style.top = `${top}px`;

        window.requestAnimationFrame(() => {
            notice.classList.add("jpps-hdr-notice-visible");
        });
        hdrNoticeTimer = window.setTimeout(removeHdrNotice, 8000);
    }

    function removeButton() {
        removeHdrNotice();
        document.getElementById(BUTTON_ID)?.remove();
    }

    function createButton(context) {
        const button = document.createElement("button");
        button.id = BUTTON_ID;
        button.type = "button";
        button.className = "button-flat detailButton jpps-button";
        button.dataset.itemId = context.itemId;
        button.dataset.itemType = context.itemType;
        button.setAttribute("aria-label", "使用 PotPlayer 播放");

        const icon = document.createElement("span");
        icon.className = "jpps-button-icon";
        icon.setAttribute("aria-hidden", "true");
        icon.textContent = "▶";

        const label = document.createElement("span");
        label.textContent = "PotPlayer 播放";

        button.addEventListener("click", () => {
            const apiContext = window.__jppsJellyfinAdapter.getApiContext();
            window.chrome.webview.postMessage({
                type: "playWithPotPlayer",
                itemId: context.itemId,
                serverAddress: apiContext.serverAddress,
                userId: apiContext.userId,
                accessToken: apiContext.accessToken
            });
        });

        button.append(icon, label);
        return button;
    }

    async function updateButton() {
        updateQueued = false;
        const version = ++updateVersion;
        const adapter = window.__jppsJellyfinAdapter;

        if (!window.chrome?.webview || !adapter) {
            removeButton();
            return;
        }

        const context = await adapter.getPlayableDetailContext();
        if (version !== updateVersion) return;

        if (!context) {
            removeButton();
            return;
        }

        const currentButton = document.getElementById(BUTTON_ID);
        if (currentButton?.dataset.itemId === context.itemId &&
            currentButton.parentElement === context.actionContainer) {
            return;
        }

        currentButton?.remove();
        const button = createButton(context);
        context.playButton.insertAdjacentElement("afterend", button);
    }

    function queueUpdate() {
        if (updateQueued) return;
        updateQueued = true;
        window.setTimeout(updateButton, 100);
    }

    function start() {
        if (!document.documentElement) {
            window.setTimeout(start, 0);
            return;
        }

        const observer = new MutationObserver(queueUpdate);
        observer.observe(document.documentElement, {
            childList: true,
            subtree: true
        });

        const originalPushState = history.pushState;
        history.pushState = function (...args) {
            const result = originalPushState.apply(this, args);
            queueUpdate();
            return result;
        };

        const originalReplaceState = history.replaceState;
        history.replaceState = function (...args) {
            const result = originalReplaceState.apply(this, args);
            queueUpdate();
            return result;
        };

        window.addEventListener("popstate", queueUpdate);
        window.addEventListener("hashchange", queueUpdate);
        window.chrome.webview.addEventListener("message", event => {
            if (event.data?.type === "showHdrNotice") {
                showHdrNotice();
            }
        });
        window.setInterval(queueUpdate, FALLBACK_INTERVAL_MS);
        queueUpdate();
    }

    start();
})();
