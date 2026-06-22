(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        var form = document.getElementById('bulkSaveForm');
        if (!form) {
            return;
        }

        // --- DOM references ---
        var albumSearch = document.getElementById('albumSearch');
        var albumSearchStatus = document.getElementById('albumSearchStatus');
        var albumTableBody = document.getElementById('albumTableBody');
        var albumTableWrap = albumTableBody.closest('.bulk-save-table-wrap');
        var albumEmpty = document.getElementById('albumEmpty');
        var albumCount = document.getElementById('albumCount');

        var albumSearchClear = document.getElementById('albumSearchClear');

        var playlistSearch = document.getElementById('playlistSearch');
        var playlistSearchStatus = document.getElementById('playlistSearchStatus');
        var playlistTableBody = document.getElementById('playlistTableBody');
        var playlistSearchClear = document.getElementById('playlistSearchClear');
        var playlistEmpty = document.getElementById('playlistEmpty');
        var playlistCount = document.getElementById('playlistCount');

        var submit = document.getElementById('bulkSaveSubmit');
        var submitLabel = submit.querySelector('.shuffle-submit-label');
        var hint = document.getElementById('bulkSaveHint');
        var status = document.getElementById('bulkSaveStatus');
        var hiddenInputs = document.getElementById('bulkSaveHiddenInputs');

        // --- State: id -> entity object (persists across searches) ---
        var selectedAlbums = {};
        var selectedPlaylists = {};

        // Preloaded playlists (full catalog for client-side filtering).
        var allPlaylists = [];
        var dataEl = document.getElementById('bulkSavePlaylistData');
        if (dataEl) {
            try {
                allPlaylists = JSON.parse(dataEl.textContent) || [];
            } catch (e) {
                allPlaylists = [];
            }
        }

        var inFlight = false;
        var SEARCH_DEBOUNCE = 300;
        var MIN_QUERY = 2;

        // ---------- Helpers ----------
        function countOf(map) {
            return Object.keys(map).length;
        }

        function escapeHtml(value) {
            if (value === null || value === undefined) {
                return '';
            }
            return String(value)
                .replace(/&/g, '&amp;')
                .replace(/</g, '&lt;')
                .replace(/>/g, '&gt;')
                .replace(/"/g, '&quot;')
                .replace(/'/g, '&#39;');
        }

        function formatDate(value) {
            if (!value) {
                return '—';
            }
            var formatted = window.DateFmt.formatDateOnly(value);
            return formatted === value ? escapeHtml(value) : formatted;
        }

        function coverCell(imageUrl) {
            if (imageUrl) {
                return '<img class="bulk-save-cover" src="' + escapeHtml(imageUrl) +
                    '" alt="" loading="lazy" onerror="this.style.display=\'none\';this.parentNode.classList.add(\'bulk-save-cover--placeholder\');this.parentNode.textContent=\'\\u266B\';">';
            }
            return '<span class="bulk-save-cover bulk-save-cover--placeholder" aria-hidden="true">♫</span>';
        }

        // ---------- Album rendering ----------
        function albumRowHtml(album, checked) {
            var id = escapeHtml(album.id);
            var rawRelease = album.releaseDate || '';
            return '<tr class="' + (checked ? 'is-selected' : '') + '" data-id="' + id +
                '" data-release="' + escapeHtml(rawRelease) +
                '" data-image="' + escapeHtml(album.imageUrl || '') + '">' +
                '<td data-label="Cover">' + coverCell(album.imageUrl) + '</td>' +
                '<td data-label="Album"><span class="bulk-save-name">' + escapeHtml(album.name) + '</span></td>' +
                '<td data-label="Artists"><span class="bulk-save-sub">' + escapeHtml(album.artists) + '</span></td>' +
                '<td data-label="Released" class="bulk-save-col-released"><span class="bulk-save-muted">' + formatDate(rawRelease) + '</span></td>' +
                '</tr>';
        }

        function renderAlbums(list) {
            var html = '';
            for (var i = 0; i < list.length; i++) {
                html += albumRowHtml(list[i], !!selectedAlbums[list[i].id]);
            }
            albumTableBody.innerHTML = html;
        }

        function renderSelectedAlbums() {
            var list = Object.keys(selectedAlbums).map(function (k) { return selectedAlbums[k]; });
            renderAlbums(list);
            updateAlbumEmptyState(list.length, '');
        }

        function updateAlbumEmptyState(rowCount, query) {
            if (rowCount > 0) {
                albumEmpty.hidden = true;
                return;
            }
            albumEmpty.hidden = false;
            if (query && query.length >= MIN_QUERY) {
                albumEmpty.textContent = 'No albums found.';
            } else if (countOf(selectedAlbums) === 0) {
                albumEmpty.textContent = 'Search for albums above to get started.';
            } else {
                albumEmpty.textContent = 'No albums selected yet.';
            }
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

        // Multi-field filter: name, description, track count.
        function filterPlaylists(query) {
            var q = query.trim().toLowerCase();
            return allPlaylists.filter(function (p) {
                var haystack = [
                    p.name || '',
                    p.description || '',
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

        // ---------- Selection counts + validation ----------
        function updateCounts() {
            var a = countOf(selectedAlbums);
            var p = countOf(selectedPlaylists);
            albumCount.textContent = a + (a === 1 ? ' album selected' : ' albums selected');
            playlistCount.textContent = p + (p === 1 ? ' playlist selected' : ' playlists selected');

            var ready = a > 0 && p > 0;
            if (!inFlight) {
                submit.disabled = !ready;
            }
            hint.hidden = ready;
        }

        // ---------- Album search (debounced AJAX) ----------
        var albumSearchTimer = null;
        var albumSearchSeq = 0;

        function runAlbumSearch(query) {
            var seq = ++albumSearchSeq;
            albumTableWrap.classList.add('is-loading');
            albumSearchStatus.textContent = 'Searching…';
            albumEmpty.hidden = true;

            fetch('/bulk-album-save/search-albums?q=' + encodeURIComponent(query), {
                headers: { 'Accept': 'application/json' }
            })
                .then(function (res) { return res.json(); })
                .then(function (results) {
                    if (seq !== albumSearchSeq) {
                        return;
                    }
                    var list = Array.isArray(results) ? results : [];
                    renderAlbums(list);
                    updateAlbumEmptyState(list.length, query);
                    albumSearchStatus.textContent = list.length + (list.length === 1 ? ' result' : ' results');
                })
                .catch(function () {
                    if (seq !== albumSearchSeq) {
                        return;
                    }
                    albumTableBody.innerHTML = '';
                    albumEmpty.hidden = false;
                    albumEmpty.textContent = 'Album search failed. Please try again.';
                    albumSearchStatus.textContent = '';
                })
                .finally(function () {
                    if (seq === albumSearchSeq) {
                        albumTableWrap.classList.remove('is-loading');
                    }
                });
        }

        function onAlbumSearchInput() {
            var query = albumSearch.value.trim();
            albumSearchClear.hidden = albumSearch.value === '';

            if (albumSearchTimer) {
                clearTimeout(albumSearchTimer);
            }

            if (query.length < MIN_QUERY) {
                albumSearchSeq++;
                albumTableWrap.classList.remove('is-loading');
                albumSearchStatus.textContent = '';
                renderSelectedAlbums();
                return;
            }

            albumSearchTimer = setTimeout(function () {
                runAlbumSearch(query);
            }, SEARCH_DEBOUNCE);
        }

        // ---------- Click-to-toggle selection (event delegation) ----------
        function albumFromRow(row, id) {
            return {
                id: id,
                name: textOf(row, '[data-label="Album"]'),
                artists: textOf(row, '[data-label="Artists"]'),
                releaseDate: row.dataset.release || '',
                imageUrl: row.dataset.image || null
            };
        }

        function textOf(row, selector) {
            var el = row.querySelector(selector);
            return el ? el.textContent.trim() : '';
        }

        albumTableBody.addEventListener('click', function (e) {
            if (e.target.tagName === 'A') {
                return;
            }
            var row = e.target.closest('tr[data-id]');
            if (!row) {
                return;
            }
            var id = row.dataset.id;
            if (selectedAlbums[id]) {
                delete selectedAlbums[id];
                row.classList.remove('is-selected');
                if (albumSearch.value.trim().length < MIN_QUERY) {
                    renderSelectedAlbums();
                }
            } else {
                selectedAlbums[id] = albumFromRow(row, id);
                row.classList.add('is-selected');
            }
            updateCounts();
        });

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
            updateCounts();
        });

        // ---------- Submit ----------
        function setLoading(loading) {
            inFlight = loading;
            submit.disabled = loading;
            if (loading) {
                submit.setAttribute('aria-busy', 'true');
                submit.classList.add('is-shuffling');
                submitLabel.textContent = 'Saving…';
            } else {
                submit.removeAttribute('aria-busy');
                submit.classList.remove('is-shuffling');
                submitLabel.textContent = 'Confirm bulk add';
            }
        }

        function buildHiddenInputs() {
            hiddenInputs.innerHTML = '';
            Object.keys(selectedAlbums).forEach(function (id) {
                var input = document.createElement('input');
                input.type = 'hidden';
                input.name = 'AlbumIds';
                input.value = id;
                hiddenInputs.appendChild(input);
            });
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
            if (countOf(selectedAlbums) === 0 || countOf(selectedPlaylists) === 0) {
                hint.hidden = false;
                status.classList.add('is-error');
                status.textContent = 'Select at least one album and one playlist.';
                return;
            }

            buildHiddenInputs();
            setLoading(true);
            status.classList.remove('is-error');
            status.textContent = 'Saving albums to playlists…';

            fetch(form.action, { method: 'POST', body: new FormData(form) })
                .then(function (res) { return res.json(); })
                .then(function (result) {
                    status.textContent = result.message || (result.success ? 'Done.' : 'Something went wrong.');
                    status.classList.toggle('is-error', result.success !== true);
                })
                .catch(function () {
                    status.textContent = 'Bulk add failed. Please try again.';
                    status.classList.add('is-error');
                })
                .finally(function () {
                    setLoading(false);
                    hiddenInputs.innerHTML = '';
                    updateCounts();
                });
        });

        // ---------- Clear button handlers ----------
        albumSearchClear.addEventListener('click', function () {
            albumSearch.value = '';
            albumSearchClear.hidden = true;
            albumSearch.focus();
            onAlbumSearchInput();
        });

        playlistSearchClear.addEventListener('click', function () {
            playlistSearch.value = '';
            playlistSearchClear.hidden = true;
            playlistSearch.focus();
            renderPlaylistView();
        });

        // ---------- Search input bindings ----------
        playlistSearch.addEventListener('input', renderPlaylistView);
        albumSearch.addEventListener('input', onAlbumSearchInput);

        // ---------- Initial render ----------
        renderSelectedAlbums();
        renderPlaylistView();
        updateCounts();
    });
})();
