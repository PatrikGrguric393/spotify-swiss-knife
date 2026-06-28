(function () {
    function buildDetailPayload(r) {
        var tracks = (r.tracks || []).map(function (t, index) {
            return {
                '#': index + 1,
                Song: t.song,
                Artists: t.artists,
                Duration: t.duration
            };
        });

        var payload = {
            Name: r.name,
            Owner: r.owner,
            TrackCount: r.tracksCount,
            LastShuffled: r.lastShuffled || null,
            Tracks: tracks
        };

        if (r.description) {
            payload.Description = r.description;
        }

        return payload;
    }

    function renderRows(rows) {
        var tbody = document.querySelector('.entity-table tbody');
        if (!tbody) return;
        tbody.innerHTML = '';
        rows.forEach(function(r) {
            var detailsId = 'playlist-details-' + r.id;
            var tr = document.createElement('tr');
            tr.setAttribute('tabindex', '0');
            tr.setAttribute('data-details-id', detailsId);
            tr.setAttribute('data-entity-type', 'Playlist');
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
            tdShuffled.textContent = r.lastShuffled ? window.DateFmt.formatTimestamp(r.lastShuffled) : 'Never';

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

            var payload = document.createElement('script');
            payload.type = 'application/json';
            payload.id = detailsId;
            payload.textContent = JSON.stringify(buildDetailPayload(r));

            tdActions.appendChild(edit);
            tdActions.appendChild(del);
            tdActions.appendChild(payload);
            tr.appendChild(tdName);
            tr.appendChild(tdOwner);
            tr.appendChild(tdTracks);
            tr.appendChild(tdShuffled);
            tr.appendChild(tdActions);
            tbody.appendChild(tr);
        });

        if (rows.length === 0) {
            tbody.appendChild(window.LibraryList.makeStatusRow(5, 'No results'));
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
                tbody.appendChild(window.LibraryList.makeStatusRow(5, 'Loading...'));
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

    document.addEventListener('DOMContentLoaded', function() {
        window.LibraryList.setup({
            searchInputId: 'playlistSearch',
            rowSelector: '.entity-table tbody tr[data-details-id]',
            doSearch: doSearch,
            onReady: function (ctx) {
                var searchInput = ctx.input;
                var onDateSelect = function() { ctx.fetchSearch(searchInput.value); };
                window.Dpc.initDateRange(onDateSelect, {
                    fromHiddenId: 'playlistDateFrom',
                    toHiddenId: 'playlistDateTo',
                    clearBtnId: 'playlistDateClear',
                    triggerFromId: 'triggerFrom',
                    triggerToId: 'triggerTo',
                    timerCtx: ctx
                });
            }
        });
    });
})();
