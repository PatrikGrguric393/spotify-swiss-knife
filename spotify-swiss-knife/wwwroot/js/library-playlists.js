(function () {
    var locale = navigator.language || 'en';
    var dateFmt = new Intl.DateTimeFormat(locale, { day: '2-digit', month: 'short', year: 'numeric' });

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

    function renderRows(rows) {
        var tbody = document.querySelector('.entity-table tbody');
        if (!tbody) return;
        tbody.innerHTML = '';
        rows.forEach(function(r) {
            var contentsId = 'playlist-contents-' + r.id;
            var tr = document.createElement('tr');
            tr.setAttribute('tabindex', '0');
            tr.setAttribute('data-contents-id', contentsId);
            tr.setAttribute('aria-label', 'View tracks for ' + r.name);

            var tdName = document.createElement('td');
            tdName.setAttribute('data-label', 'Name');
            tdName.textContent = r.name;

            var tdOwner = document.createElement('td');
            tdOwner.setAttribute('data-label', 'Owner');
            tdOwner.textContent = r.owner;

            var tdTracks = document.createElement('td');
            tdTracks.setAttribute('data-label', 'Tracks');
            tdTracks.textContent = r.tracksCount;

            var tdShuffled = document.createElement('td');
            tdShuffled.setAttribute('data-label', 'Last Shuffled');
            tdShuffled.textContent = r.lastShuffled ? dateFmt.format(new Date(r.lastShuffled)) : 'Never';

            var tdActions = document.createElement('td');
            tdActions.setAttribute('data-label', 'Actions');
            tdActions.className = 'cell-actions';
            var edit = document.createElement('a');
            edit.className = 'btn';
            edit.href = '/lib/playlists/edit/' + encodeURIComponent(r.id);
            edit.textContent = 'Edit';
            var del = document.createElement('a');
            del.className = 'btn btn-danger';
            del.href = '/lib/playlists/delete/' + encodeURIComponent(r.id);
            del.textContent = 'Delete';

            tdActions.appendChild(edit);
            tdActions.appendChild(del);
            tr.appendChild(tdName);
            tr.appendChild(tdOwner);
            tr.appendChild(tdTracks);
            tr.appendChild(tdShuffled);
            tr.appendChild(tdActions);
            tbody.appendChild(tr);
        });

        if (rows.length === 0) {
            tbody.appendChild(makeStatusRow(5, 'No results'));
        }
    }

    function doSearch(q, opts) {
        opts = opts || {};
        var status = document.querySelector('.search-status');
        var tbody = document.querySelector('.entity-table tbody');
        if (!opts.quiet) {
            if (status) status.textContent = 'Loading...';
            if (tbody) {
                tbody.innerHTML = '';
                tbody.appendChild(makeStatusRow(5, 'Loading...'));
            }
        }

        var params = new URLSearchParams();
        params.set('q', q || '');

        var dateFrom = document.getElementById('playlistDateFrom');
        var dateTo = document.getElementById('playlistDateTo');
        if (dateFrom && dateFrom.value) params.set('dateFrom', dateFrom.value);
        if (dateTo && dateTo.value) params.set('dateTo', dateTo.value);

        return fetch('/lib/playlists/search?' + params.toString(), { headers: { 'Accept': 'application/json' } })
            .then(function(r) { return r.json(); })
            .then(function(data) {
                var scroll = document.querySelector('.table-wrap');
                var top = scroll ? scroll.scrollTop : 0;
                renderRows(data);
                if (scroll) scroll.scrollTop = top;
                if (status) status.textContent = data.length === 0 ? 'No results' : '';
            })
            .catch(function(err) { console.error('Playlist search failed', err); });
    }

    function fetchSearch(q) { return doSearch(q, {}); }

    document.addEventListener('DOMContentLoaded', function() {
        var searchInput = document.getElementById('playlistSearch');
        if (!searchInput) return;

        var timer = 0;

        searchInput.addEventListener('input', function() {
            clearTimeout(timer);
            timer = setTimeout(function() { fetchSearch(searchInput.value); }, 250);
        });

        searchInput.addEventListener('keydown', function(e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                var first = document.querySelector('.entity-table tbody tr[data-contents-id]');
                if (first && typeof first.focus === 'function') first.focus();
            }
        });

        var onDateSelect = function() { fetchSearch(searchInput.value); };

        window.Dpc.selfInit = true;
        window.Dpc.initDpcWrappers(onDateSelect);
        document.querySelectorAll('.dpc').forEach(function(el) {
            window.Dpc.buildCalendar(el, onDateSelect);
        });

        var clearBtn = document.getElementById('playlistDateClear');
        if (clearBtn) {
            clearBtn.addEventListener('click', function() {
                var fromHidden = document.getElementById('playlistDateFrom');
                var toHidden = document.getElementById('playlistDateTo');
                if (fromHidden) fromHidden.value = '';
                if (toHidden) toHidden.value = '';
                var trigFrom = document.getElementById('triggerFrom');
                var trigTo = document.getElementById('triggerTo');
                if (trigFrom) trigFrom.textContent = 'From: —';
                if (trigTo) trigTo.textContent = 'To: —';
                document.querySelectorAll('.dpc').forEach(function(el) {
                    if (el._state) window.Dpc.renderCalendar(el, el._state, onDateSelect);
                });
                clearTimeout(timer);
                fetchSearch(searchInput.value);
            });
        }

        fetchSearch('');

        if (window.LibraryAutoRefresh) {
            window.LibraryAutoRefresh.start(function() { return doSearch(searchInput.value, { quiet: true }); }, 5000);
        }
    });
})();
