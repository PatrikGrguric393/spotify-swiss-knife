(function () {
    function parseDuration(val) {
        if (!val || !val.trim()) return null;
        var parts = val.trim().split(':');
        if (parts.length === 1) {
            var secs = parseInt(parts[0], 10);
            return isNaN(secs) ? null : secs;
        }
        if (parts.length === 2) {
            var m = parseInt(parts[0], 10);
            var s = parseInt(parts[1], 10);
            if (isNaN(m) || isNaN(s)) return null;
            return m * 60 + s;
        }
        return null;
    }

    function formatDuration(ms) {
        var s = Math.floor(ms / 1000);
        var m = Math.floor(s / 60);
        return m + ':' + String(s % 60).padStart(2, '0');
    }

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
            var detailsId = 'track-details-' + r.id;
            var tr = document.createElement('tr');
            tr.setAttribute('tabindex', '0');
            tr.setAttribute('data-details-id', detailsId);
            tr.setAttribute('data-entity-type', 'Song');
            tr.setAttribute('aria-label', 'View details for ' + r.name);

            var tdName = document.createElement('td');
            tdName.setAttribute('data-label', 'Name');
            tdName.textContent = r.name;

            var tdArtists = document.createElement('td');
            tdArtists.setAttribute('data-label', 'Artists');
            tdArtists.textContent = r.artists;

            var tdDuration = document.createElement('td');
            tdDuration.setAttribute('data-label', 'Duration');
            tdDuration.textContent = formatDuration(r.durationMs);

            var tdActions = document.createElement('td');
            tdActions.setAttribute('data-label', 'Actions');
            tdActions.className = 'cell-actions';
            var edit = document.createElement('a');
            edit.className = 'btn';
            edit.href = '/lib/tracks/edit/' + encodeURIComponent(r.id);
            edit.textContent = 'Edit';
            var del = document.createElement('a');
            del.className = 'btn btn-danger';
            del.href = '/lib/tracks/delete/' + encodeURIComponent(r.id);
            del.textContent = 'Delete';

            var script = document.createElement('script');
            script.type = 'application/json';
            script.id = detailsId;
            script.textContent = JSON.stringify({
                Name: r.name,
                Artists: r.artists,
                Duration: formatDuration(r.durationMs),
                Disc: r.discNumber,
                TrackNumber: r.trackNumber,
                Local: r.isLocal ? 'Yes' : 'No'
            });

            tdActions.appendChild(edit);
            tdActions.appendChild(del);
            tdActions.appendChild(script);
            tr.appendChild(tdName);
            tr.appendChild(tdArtists);
            tr.appendChild(tdDuration);
            tr.appendChild(tdActions);
            tbody.appendChild(tr);
        });

        if (rows.length === 0) {
            tbody.appendChild(makeStatusRow(4, 'No results'));
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
                tbody.appendChild(makeStatusRow(4, 'Loading...'));
            }
        }

        var params = new URLSearchParams();
        params.set('q', q || '');

        var minSec = parseDuration(document.getElementById('songDurationMin') ? document.getElementById('songDurationMin').value : '');
        var maxSec = parseDuration(document.getElementById('songDurationMax') ? document.getElementById('songDurationMax').value : '');
        if (minSec !== null) params.set('durationMin', minSec);
        if (maxSec !== null) params.set('durationMax', maxSec);

        return fetch('/lib/tracks/search?' + params.toString(), { headers: { 'Accept': 'application/json' } })
            .then(function(r) { return r.json(); })
            .then(function(data) {
                var scroll = document.querySelector('.table-wrap');
                var top = scroll ? scroll.scrollTop : 0;
                renderRows(data);
                if (scroll) scroll.scrollTop = top;
                if (status) status.textContent = data.length === 0 ? 'No results' : '';
            })
            .catch(function(err) { console.error('Song search failed', err); });
    }

    function fetchSearch(q) { return doSearch(q, {}); }

    document.addEventListener('DOMContentLoaded', function() {
        var searchInput = document.getElementById('songSearch');
        if (!searchInput) return;

        var timer = 0;

        searchInput.addEventListener('input', function() {
            clearTimeout(timer);
            timer = setTimeout(function() { fetchSearch(searchInput.value); }, 250);
        });

        searchInput.addEventListener('keydown', function(e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                var first = document.querySelector('.entity-table tbody tr[data-details-id]');
                if (first && typeof first.focus === 'function') first.focus();
            }
        });

        var durationMin = document.getElementById('songDurationMin');
        var durationMax = document.getElementById('songDurationMax');
        var durationClear = document.getElementById('songDurationClear');

        if (durationMin) {
            durationMin.addEventListener('input', function() {
                clearTimeout(timer);
                timer = setTimeout(function() { fetchSearch(searchInput.value); }, 250);
            });
        }
        if (durationMax) {
            durationMax.addEventListener('input', function() {
                clearTimeout(timer);
                timer = setTimeout(function() { fetchSearch(searchInput.value); }, 250);
            });
        }
        if (durationClear) {
            durationClear.addEventListener('click', function() {
                if (durationMin) durationMin.value = '';
                if (durationMax) durationMax.value = '';
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
