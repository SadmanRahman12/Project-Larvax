// pwa.js - registers service worker and syncs queued reports when online
(function () {
    // Register service worker already done in layout; ensure additional hooks here
    if ('serviceWorker' in navigator) {
        navigator.serviceWorker.ready.then(function (reg) {
            console.debug('Service worker ready for LarvaX');
        });
    }

    function getQueue() {
        try {
            const raw = localStorage.getItem('larvax_report_queue');
            return raw ? JSON.parse(raw) : [];
        } catch (e) { return []; }
    }

    function setQueue(q) {
        localStorage.setItem('larvax_report_queue', JSON.stringify(q));
    }

    async function syncQueuedReports() {
        if (!navigator.onLine) return;
        const queue = getQueue();
        if (!queue.length) return;

        for (let i = 0; i < queue.length; i++) {
            const item = queue[i];
            try {
                const resp = await fetch('/api/reports', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(item),
                    credentials: 'same-origin'
                });

                if (resp.ok) {
                    // remove item from queue
                    queue.splice(i, 1);
                    i--; // adjust index
                    setQueue(queue);
                }
            } catch (e) {
                console.debug('PWA sync failed', e);
            }
        }
    }

    window.addEventListener('online', function () {
        console.debug('Browser online - attempting to sync queued reports');
        syncQueuedReports();
    });

    // Expose helper for form to queue reports
    window.LarvaXPwa = {
        queueReport: function (obj) {
            const q = getQueue();
            q.push(obj);
            setQueue(q);
            // attempt immediate sync
            syncQueuedReports();
        }
    };

    // Try sync on load
    setTimeout(syncQueuedReports, 3000);
})();
