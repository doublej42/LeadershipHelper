const CACHE_VERSION = "leadership-helper-v2";
const SHELL_CACHE = `${CACHE_VERSION}-shell`;
const PAGE_CACHE = `${CACHE_VERSION}-pages`;
const ASSET_CACHE = `${CACHE_VERSION}-assets`;

const APP_SHELL_URLS = [
    "/",
    "/situations",
    "/offline.html",
    "/lib/bootstrap/dist/css/bootstrap.min.css",
    "/css/site.css",
    "/lib/bootstrap/dist/js/bootstrap.bundle.min.js",
    "/lib/jquery/dist/jquery.min.js",
    "/js/site.js",
    "/icons/icon-192.png",
    "/icons/icon-512.png",
    "/icons/apple-touch-icon.png"
];

self.addEventListener("install", (event) => {
    event.waitUntil(
        caches.open(SHELL_CACHE).then((cache) => cache.addAll(APP_SHELL_URLS))
    );

    self.skipWaiting();
});

self.addEventListener("activate", (event) => {
    event.waitUntil(
        caches.keys().then((keys) =>
            Promise.all(
                keys
                    .filter((key) => !key.startsWith(CACHE_VERSION))
                    .map((key) => caches.delete(key))
            )
        )
    );

    self.clients.claim();
});

function isAnonymousNavigation(url) {
    const path = url.pathname.toLowerCase();

    if (path === "/" || path === "/home" || path === "/home/index") {
        return true;
    }

    if (path === "/situations") {
        return true;
    }

    // Situation details are frequently updated and can be personalized for the current user,
    // so they should always come from the network instead of the navigation cache.
    return false;
}

function isStaticAsset(request, url) {
    if (url.origin !== self.location.origin) {
        return false;
    }

    return ["style", "script", "image", "font"].includes(request.destination);
}

async function handleNavigation(request) {
    const pageCache = await caches.open(PAGE_CACHE);

    try {
        const response = await fetch(request);
        if (response && response.ok) {
            pageCache.put(request, response.clone());
        }

        return response;
    } catch {
        const cached = await pageCache.match(request);
        if (cached) {
            return cached;
        }

        const shellCache = await caches.open(SHELL_CACHE);
        return shellCache.match("/offline.html");
    }
}

async function handleStaticAsset(request) {
    const cache = await caches.open(ASSET_CACHE);
    const cached = await cache.match(request);
    if (cached) {
        return cached;
    }

    const response = await fetch(request);
    if (response && response.ok) {
        cache.put(request, response.clone());
    }

    return response;
}

self.addEventListener("fetch", (event) => {
    if (event.request.method !== "GET") {
        return;
    }

    const url = new URL(event.request.url);

    if (event.request.mode === "navigate" && isAnonymousNavigation(url)) {
        event.respondWith(handleNavigation(event.request));
        return;
    }

    if (isStaticAsset(event.request, url)) {
        event.respondWith(handleStaticAsset(event.request));
    }
});
