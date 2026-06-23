(function () {
    function makeStatusRow(colspan, text) {
        var tr = document.createElement('tr');
        var td = document.createElement('td');
        td.setAttribute('colspan', colspan);
        td.style.padding = '0.6rem 0.75rem';
        td.style.color = '#b6ffb6';
        td.textContent = text;
        tr.appendChild(td);
        return tr;
    }

    // Shared search wiring for library list pages. Each page supplies what
    // differs; common behavior (debounced input, Enter->focus first row,
    // initial fetch, auto-refresh) is centralized here. Returns the debounce
    // timer holder so pages with extra filter inputs can share the same timer.
    function setup(config) {
        var searchInput = document.getElementById(config.searchInputId);
        if (!searchInput) return null;

        var debounceMs = config.debounceMs || 250;
        var ctx = { input: searchInput, timer: 0 };

        function fetchSearch(q) { return config.doSearch(q, {}); }

        searchInput.addEventListener('input', function () {
            clearTimeout(ctx.timer);
            ctx.timer = setTimeout(function () { fetchSearch(searchInput.value); }, debounceMs);
        });

        searchInput.addEventListener('keydown', function (e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                var first = document.querySelector(config.rowSelector);
                if (first && typeof first.focus === 'function') first.focus();
            }
        });

        ctx.fetchSearch = fetchSearch;

        if (typeof config.onReady === 'function') config.onReady(ctx);

        fetchSearch('');

        if (window.LibraryAutoRefresh) {
            window.LibraryAutoRefresh.start(function () {
                return config.doSearch(searchInput.value, { quiet: true });
            }, 5000);
        }

        return ctx;
    }

    window.LibraryList = {
        makeStatusRow: makeStatusRow,
        setup: setup
    };
})();
