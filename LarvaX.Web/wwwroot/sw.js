// LarvaX Service Worker — provides offline support while ensuring dynamic pages are always fresh.
// Strategy: Network-first for HTML navigation (so user authentication/state is always live);
// Cache-first for versioned static assets (CSS, JS, fonts).

const CACHE_NAME = 'larvax-v2';

const PRECACHE_STATIC = [
  '/FluidManagement',
  '/FluidManagement/Planner',
  '/lib/bootstrap/dist/css/bootstrap.min.css',
  '/lib/bootstrap/dist/js/bootstrap.bundle.min.js',
  '/lib/jquery/dist/jquery.min.js',
  '/css/site.css',
  '/js/site.js'
];

// Install: pre-populate cache with static offline resources
self.addEventListener('install', event => {
  event.waitUntil(
    caches.open(CACHE_NAME).then(cache => cache.addAll(PRECACHE_STATIC))
  );
  self.skipWaiting();
});

// Activate: purge any old caches (e.g. larvax-v1) and claim clients immediately
self.addEventListener('activate', event => {
  event.waitUntil(
    caches.keys().then(keys =>
      Promise.all(keys.filter(k => k !== CACHE_NAME).map(k => caches.delete(k)))
    )
  );
  self.clients.claim();
});

// Fetch handler
self.addEventListener('fetch', event => {
  const url = new URL(event.request.url);

  // Skip non-GET, cross-origin, SignalR hubs, and API requests
  if (event.request.method !== 'GET') return;
  if (!url.origin.includes(self.location.origin)) return;
  if (url.pathname.startsWith('/api/') || 
      url.pathname.startsWith('/alertshub') || 
      url.pathname.startsWith('/videoconsulthub')) {
    return;
  }

  // 1. Navigation & HTML requests: ALWAYS NETWORK-FIRST
  // Ensures user authentication status, top-right profile name, and dynamic views are never stale
  if (event.request.mode === 'navigate' || event.request.headers.get('accept')?.includes('text/html')) {
    event.respondWith(
      fetch(event.request)
        .then(response => {
          if (response && response.status === 200 && response.type === 'basic') {
            const responseClone = response.clone();
            caches.open(CACHE_NAME).then(cache => cache.put(event.request, responseClone));
          }
          return response;
        })
        .catch(() => caches.match(event.request))
    );
    return;
  }

  // 2. Static Assets (CSS, JS, libraries): CACHE-FIRST with network fallback
  const isStaticAsset = url.pathname.startsWith('/lib/') || 
                        url.pathname.startsWith('/css/') || 
                        url.pathname.startsWith('/js/') ||
                        url.pathname.startsWith('/images/') ||
                        url.pathname.endsWith('.css') ||
                        url.pathname.endsWith('.js');

  if (isStaticAsset) {
    event.respondWith(
      caches.match(event.request).then(cached => cached || fetch(event.request))
    );
    return;
  }

  // 3. Other requests: Network with cache fallback
  event.respondWith(
    fetch(event.request).catch(() => caches.match(event.request))
  );
});
