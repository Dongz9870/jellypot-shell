(() => {
    "use strict";

    if (window.top !== window) return;
    if (window.__jppsLoaded) return;
    window.__jppsLoaded = true;

    const BUTTON_ID = "jpps-potplayer-button";
    const FALLBACK_INTERVAL_MS = 1500;
    let updateQueued = false;
    let updateVersion = 0;

    function removeButton() {
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
            window.chrome.webview.postMessage({
                type: "playWithPotPlayer",
                itemId: context.itemId
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
        window.setInterval(queueUpdate, FALLBACK_INTERVAL_MS);
        queueUpdate();
    }

    start();
})();
