self.addEventListener("install", (event) => {
    // Keep the service worker minimal: installability only, no offline cache.
    self.skipWaiting();
});

self.addEventListener("activate", (event) => {
    event.waitUntil(caches.keys().then((keys) => Promise.all(keys.map((key) => caches.delete(key)))));

    self.clients.claim();
});
