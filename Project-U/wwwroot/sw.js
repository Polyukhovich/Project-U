const CACHE_NAME = 'projectu-v1';

// Файли які кешуємо для offline
const STATIC_ASSETS = [
    '/',
    '/css/site.css',
    '/css/sidebar.css',
    '/lib/bootstrap/dist/css/bootstrap.min.css',
    '/lib/jquery/dist/jquery.min.js',
    '/lib/bootstrap/dist/js/bootstrap.bundle.min.js',
    '/offline.html'
];

// Встановлення Service Worker
self.addEventListener('install', event => {
    event.waitUntil(
        caches.open(CACHE_NAME)
            .then(cache => cache.addAll(STATIC_ASSETS))
            .then(() => self.skipWaiting())
    );
});

// Активація — видаляємо старий кеш
self.addEventListener('activate', event => {
    event.waitUntil(
        caches.keys().then(keys =>
            Promise.all(keys
                .filter(key => key !== CACHE_NAME)
                .map(key => caches.delete(key))
            )
        ).then(() => self.clients.claim())
    );
});

// Обробка запитів
self.addEventListener('fetch', event => {
    const url = new URL(event.request.url);

    // Cache-First для статичних файлів
    if (event.request.method === 'GET' && (
        url.pathname.startsWith('/css/') ||
        url.pathname.startsWith('/js/') ||
        url.pathname.startsWith('/lib/') ||
        url.pathname.startsWith('/icons/')
    )) {
        event.respondWith(
            caches.match(event.request)
                .then(cached => cached || fetch(event.request))
        );
        return;
    }

    // Network-First для розкладу та оцінок
    if (event.request.method === 'GET' && (
        url.pathname.startsWith('/Schedules') ||
        url.pathname.startsWith('/Grades')
    )) {
        event.respondWith(
            fetch(event.request)
                .then(response => {
                    const clone = response.clone();
                    caches.open(CACHE_NAME)
                        .then(cache => cache.put(event.request, clone));
                    return response;
                })
                .catch(() => caches.match(event.request)
                    .then(cached => cached || caches.match('/offline.html'))
                )
        );
        return;
    }

    // Для решти — Network з fallback
    event.respondWith(
        fetch(event.request)
            .catch(() => caches.match('/offline.html'))
    );
});