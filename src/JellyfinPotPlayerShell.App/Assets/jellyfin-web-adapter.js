(() => {
    "use strict";

    if (window.top !== window) return;
    if (window.__jppsJellyfinAdapter) return;

    const PLAYABLE_ITEM_TYPES = new Set(["movie", "episode", "video"]);
    const NON_PLAYABLE_ITEM_TYPES = new Set([
        "series",
        "season",
        "folder",
        "audio",
        "audioalbum",
        "musicartist"
    ]);

    const selectors = Object.freeze({
        detailPages: [
            ".itemDetailPage:not(.hide)",
            "[data-role='itemDetailPage']:not(.hide)",
            ".itemDetailPage",
            "[data-role='itemDetailPage']",
            "main"
        ],
        actionContainers: [
            ".mainDetailButtons",
            ".detailButtonContainer",
            ".detailPagePrimaryContainer .buttons",
            ".itemDetailPage .buttons"
        ],
        playButtons: [
            "button.btnPlay",
            "button.btnResume",
            "button[data-action='play']",
            "button[data-testid='play-button']",
            "button:has(.material-icons.play_arrow)",
            "button:has(.material-icons.play_circle_filled)"
        ],
        typeCarriers: [
            "[data-item-type]",
            "[data-itemtype]",
            "[data-type]"
        ]
    });

    const itemTypePromises = new Map();

    function isItemId(value) {
        return /^[a-fA-F0-9-]{16,}$/.test(value ?? "");
    }

    function isVisible(element) {
        return Boolean(element) &&
            !element.classList.contains("hide") &&
            element.getAttribute("aria-hidden") !== "true";
    }

    function queryFirst(scope, candidates) {
        for (const selector of candidates) {
            const match = scope.querySelector(selector);
            if (match && isVisible(match)) return match;
        }

        return null;
    }

    function getItemId() {
        const url = new URL(window.location.href);
        const directId = url.searchParams.get("id") ??
            url.searchParams.get("itemId");
        if (isItemId(directId)) return directId;

        const hashQueryIndex = url.hash.indexOf("?");
        if (hashQueryIndex >= 0) {
            const hashParameters = new URLSearchParams(
                url.hash.slice(hashQueryIndex + 1));
            const hashId = hashParameters.get("id") ??
                hashParameters.get("itemId");
            if (isItemId(hashId)) return hashId;
        }

        const patterns = [
            /[?&#](?:id|itemId)=([a-fA-F0-9-]{16,})/,
            /\/details\/([a-fA-F0-9-]{16,})/
        ];

        for (const pattern of patterns) {
            const match = window.location.href.match(pattern);
            if (match) return match[1];
        }

        return null;
    }

    function getDetailPage() {
        return queryFirst(document, selectors.detailPages);
    }

    function readItemType(detailPage) {
        const carriers = [
            detailPage,
            ...detailPage.querySelectorAll(selectors.typeCarriers.join(","))
        ];

        for (const carrier of carriers) {
            const itemType = carrier.dataset.itemType ??
                carrier.dataset.itemtype ??
                carrier.dataset.type;
            const normalizedItemType = itemType?.toLowerCase();
            if (PLAYABLE_ITEM_TYPES.has(normalizedItemType) ||
                NON_PLAYABLE_ITEM_TYPES.has(normalizedItemType)) {
                return normalizedItemType;
            }
        }

        return null;
    }

    function getApiClient() {
        return window.ApiClient ??
            window.ServerConnections?.currentApiClient ??
            window.ServerConnections?.getCurrentApiClient?.() ??
            null;
    }

    async function requestItemType(itemId) {
        const apiClient = getApiClient();
        const userId = apiClient?.getCurrentUserId?.();
        if (!apiClient?.getItem || !userId) return null;

        try {
            const item = await apiClient.getItem(userId, itemId);
            return item?.Type?.toLowerCase() ?? null;
        } catch {
            return null;
        }
    }

    function getItemType(itemId, detailPage) {
        const domItemType = readItemType(detailPage);
        if (domItemType) return Promise.resolve(domItemType);

        if (!itemTypePromises.has(itemId)) {
            const itemTypePromise = requestItemType(itemId).then(itemType => {
                if (!itemType) itemTypePromises.delete(itemId);
                return itemType;
            });
            itemTypePromises.set(itemId, itemTypePromise);
        }

        return itemTypePromises.get(itemId);
    }

    function findPlacement(detailPage) {
        const actionContainer = queryFirst(
            detailPage,
            selectors.actionContainers);
        const playButton = queryFirst(
            actionContainer ?? detailPage,
            selectors.playButtons);

        if (actionContainer && playButton) {
            return { actionContainer, playButton };
        }

        const pagePlayButton = queryFirst(detailPage, selectors.playButtons);
        if (pagePlayButton?.parentElement) {
            return {
                actionContainer: pagePlayButton.parentElement,
                playButton: pagePlayButton
            };
        }

        return null;
    }

    async function getPlayableDetailContext() {
        const itemId = getItemId();
        const detailPage = getDetailPage();
        if (!itemId || !detailPage) return null;

        const itemType = await getItemType(itemId, detailPage);
        if (NON_PLAYABLE_ITEM_TYPES.has(itemType)) return null;
        if (!PLAYABLE_ITEM_TYPES.has(itemType)) return null;

        const placement = findPlacement(detailPage);
        if (!placement) return null;

        return {
            itemId,
            itemType,
            ...placement
        };
    }

    window.__jppsJellyfinAdapter = Object.freeze({
        getPlayableDetailContext
    });
})();
