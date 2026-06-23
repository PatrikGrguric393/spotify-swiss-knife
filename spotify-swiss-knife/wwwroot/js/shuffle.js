(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        var form = document.getElementById('shuffleForm');
        if (!form) {
            return;
        }

        // --- DOM references ---
        var playlistSearch = document.getElementById('shufflePlaylistSearch');
        var playlistSearchStatus = document.getElementById('shufflePlaylistSearchStatus');
        var playlistSearchClear = document.getElementById('shufflePlaylistSearchClear');
        var playlistTableBody = document.getElementById('shufflePlaylistTableBody');
        var playlistEmpty = document.getElementById('shufflePlaylistEmpty');
        var playlistCount = document.getElementById('shufflePlaylistCount');

        var submit = document.getElementById('shuffleSubmit');
        var submitLabel = submit.querySelector('.shuffle-submit-label');
        var hint = document.getElementById('shuffleHint');
        var status = document.getElementById('shuffleStatus');
        var lastRun = document.getElementById('shuffleLastRun');
        var timeEl = document.getElementById('shuffleTime');
        var hiddenInputs = document.getElementById('shuffleHiddenInputs');

        // --- State: id -> playlist object (persists across searches) ---
        var selectedPlaylists = {};

        var allPlaylists = [];
        var dataEl = document.getElementById('shufflePlaylistData');
        if (dataEl) {
            try {
                allPlaylists = JSON.parse(dataEl.textContent) || [];
            } catch (e) {
                allPlaylists = [];
            }
        }

        var inFlight = false;

        var escapeHtml = window.BulkGrid.escapeHtml;
        var coverCell = window.BulkGrid.coverCell;

        // ---------- Helpers ----------
        function countOf(map) {
            return Object.keys(map).length;
        }

        // ---------- Playlist rendering ----------
        function playlistRowHtml(playlist, checked) {
            var id = escapeHtml(playlist.id);
            return '<tr class="' + (checked ? 'is-selected' : '') + '" data-id="' + id + '">' +
                '<td data-label="Cover">' + coverCell(playlist.imageUrl) + '</td>' +
                '<td data-label="Name"><span class="bulk-save-name">' + escapeHtml(playlist.name) + '</span></td>' +
                '<td data-label="Tracks"><span class="bulk-save-muted">' + escapeHtml(playlist.tracks) + '</span></td>' +
                '</tr>';
        }

        function renderPlaylists(list) {
            var html = '';
            for (var i = 0; i < list.length; i++) {
                html += playlistRowHtml(list[i], !!selectedPlaylists[list[i].id]);
            }
            playlistTableBody.innerHTML = html;
        }

        function updatePlaylistEmptyState(rowCount, query) {
            if (rowCount > 0) {
                playlistEmpty.hidden = true;
                return;
            }
            playlistEmpty.hidden = false;
            if (query) {
                playlistEmpty.textContent = 'No playlists match your search.';
            } else {
                playlistEmpty.textContent = 'No playlists found.';
            }
        }

        function filterPlaylists(query) {
            var q = query.trim().toLowerCase();
            return allPlaylists.filter(function (p) {
                var haystack = [
                    p.name || '',
                    String(p.tracks)
                ].join('  ').toLowerCase();
                return haystack.indexOf(q) !== -1;
            });
        }

        function renderPlaylistView() {
            var query = playlistSearch.value.trim();
            playlistSearchClear.hidden = playlistSearch.value === '';

            if (query === '') {
                renderPlaylists(allPlaylists);
                updatePlaylistEmptyState(allPlaylists.length, '');
                playlistSearchStatus.textContent = '';
            } else {
                var matches = filterPlaylists(query);
                renderPlaylists(matches);
                updatePlaylistEmptyState(matches.length, query);
                playlistSearchStatus.textContent = matches.length + (matches.length === 1 ? ' match' : ' matches');
            }
        }

        // ---------- Selection count + submit guard ----------
        function updateCount() {
            var n = countOf(selectedPlaylists);
            playlistCount.textContent = n + (n === 1 ? ' playlist selected' : ' playlists selected');
            if (!inFlight) {
                submit.disabled = n === 0;
            }
            if (hint) {
                hint.hidden = n > 0;
            }
        }

        // ---------- Click-to-toggle selection (event delegation) ----------
        playlistTableBody.addEventListener('click', function (e) {
            if (e.target.tagName === 'A') {
                return;
            }
            var row = e.target.closest('tr[data-id]');
            if (!row) {
                return;
            }
            var id = row.dataset.id;
            if (selectedPlaylists[id]) {
                delete selectedPlaylists[id];
                row.classList.remove('is-selected');
            } else {
                var playlist = allPlaylists.find(function (p) { return p.id === id; });
                if (playlist) {
                    selectedPlaylists[id] = playlist;
                }
                row.classList.add('is-selected');
            }
            updateCount();
        });

        // ---------- Submit ----------
        function setLoading(loading) {
            inFlight = loading;
            submit.disabled = loading;
            if (loading) {
                submit.setAttribute('aria-busy', 'true');
                submit.classList.add('is-shuffling');
                submitLabel.textContent = 'Shuffling…';
            } else {
                submit.removeAttribute('aria-busy');
                submit.classList.remove('is-shuffling');
                submitLabel.textContent = 'Start shuffle';
                updateCount();
            }
        }

        function buildHiddenInputs() {
            hiddenInputs.innerHTML = '';
            Object.keys(selectedPlaylists).forEach(function (id) {
                var input = document.createElement('input');
                input.type = 'hidden';
                input.name = 'PlaylistIds';
                input.value = id;
                hiddenInputs.appendChild(input);
            });
        }

        form.addEventListener('submit', function (event) {
            event.preventDefault();
            if (inFlight) {
                return;
            }
            if (countOf(selectedPlaylists) === 0) {
                status.classList.add('is-error');
                status.textContent = 'Select at least one playlist.';
                return;
            }

            buildHiddenInputs();
            setLoading(true);
            status.classList.remove('is-error');
            status.textContent = 'Shuffling…';

            fetch(form.action, { method: 'POST', body: new FormData(form) })
                .then(function (res) { return res.json(); })
                .then(function (result) {
                    if (result.success === true) {
                        status.classList.remove('is-error');
                        status.textContent = result.message || 'Shuffle complete.';
                        if (result.shuffledAtUtc && timeEl && lastRun) {
                            timeEl.setAttribute('datetime', result.shuffledAtUtc);
                            timeEl.setAttribute('data-utc', result.shuffledAtUtc);
                            timeEl.textContent = window.DateFmt.formatTimestamp(result.shuffledAtUtc);
                            lastRun.removeAttribute('hidden');
                        }
                    } else {
                        status.classList.add('is-error');
                        status.textContent = result.message || 'Shuffle failed. Please try again.';
                    }
                })
                .catch(function () {
                    status.classList.add('is-error');
                    status.textContent = 'Shuffle failed. Please try again.';
                })
                .finally(function () {
                    hiddenInputs.innerHTML = '';
                    setLoading(false);
                });
        });

        // ---------- Clear button ----------
        playlistSearchClear.addEventListener('click', function () {
            playlistSearch.value = '';
            playlistSearchClear.hidden = true;
            playlistSearch.focus();
            renderPlaylistView();
        });

        // ---------- Search input binding ----------
        playlistSearch.addEventListener('input', renderPlaylistView);

        // ---------- Initial render ----------
        renderPlaylistView();
        updateCount();
    });
})();
