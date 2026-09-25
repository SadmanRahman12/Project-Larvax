// LarvaX Service Worker — provides offline support for the Fluid Management calculator.
// Strategy: Cache-first for static assets; network-first for API/dynamic routes.

const CACHE_NAME = 'larvax-v1';

// Precache these assets so the Fluid Management tool works fully offline
const PRECACHE_URLS = [
  '/',
  '/FluidManagement',
  '/FluidManagement/Planner',
  '/lib/bootstrap/dist/css/bootstrap.min.css',
  '/lib/bootstrap/dist/js/bootstrap.bundle.min.js',
  '/lib/jquery/dist/jquery.min.js',
  '/css/site.css',
  '/js/site.js'
];

// Install: pre-populate the cache
self.addEventListener('install', event => {
  event.waitUntil(
    caches.open(CACHE_NAME).then(cache => cache.addAll(PRECACHE_URLS))
  );
  self.skipWaiting();
});

// Activate: clean up old caches
self.addEventListener('activate', event => {
  event.waitUntil(
    caches.keys().then(keys =>
      Promise.all(keys.filter(k => k !== CACHE_NAME).map(k => caches.delete(k)))
    )
  );
  self.clients.claim();
});

// Fetch: network-first for API / dynamic; cache-first for static
self.addEventListener('fetch', event => {
  const url = new URL(event.request.url);

  // Skip non-GET, cross-origin, and API/hub requests
  if (event.request.method !== 'GET') return;
  if (!url.origin.includes(self.location.origin)) return;
  if (url.pathname.startsWith('/api/') || url.pathname.startsWith('/alertshub') || url.pathname.startsWith('/videoconsulthub')) return;

  // Network-first for dynamic Razor pages (except precached static paths)
  const isStaticAsset = PRECACHE_URLS.some(p => url.pathname === p || url.pathname.startsWith('/lib/') || url.pathname.startsWith('/css/') || url.pathname.startsWith('/js/'));

  if (isStaticAsset) {
    event.respondWith(
      caches.match(event.request).then(cached => cached || fetch(event.request))
    );
  } else {
    event.respondWith(
      fetch(event.request).catch(() => caches.match(event.request))
    );
  }
});
