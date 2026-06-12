(function () {
    function shouldSkip() {
        if (document.hidden) return true;
        if (document.body.classList.contains('entity-detail-open')) return true;
        var ae = document.activeElement;
        if (ae && ae.closest && ae.closest('.entity-table, .library-filters')) return true;
        return false;
    }

    function start(refreshFn, intervalMs) {
        var inFlight = false;
        function tick() {
            if (inFlight || shouldSkip()) return;
            var p = refreshFn();
            if (p && typeof p.finally === 'function') {
                inFlight = true;
                p.finally(function () { inFlight = false; });
            }
        }
        setInterval(tick, intervalMs || 5000);
        document.addEventListener('visibilitychange', function () {
            if (!document.hidden) tick();
        });
    }

    window.LibraryAutoRefresh = { start: start, shouldSkip: shouldSkip };
})();
