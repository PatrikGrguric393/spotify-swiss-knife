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

        var playlistSearch = document.getElementById('playlistSearch');
        var playlistSearchStatus = document.getElementById('playlistSearchStatus');
        var playlistTableBody = document.getElementById('playlistTableBody');
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
            // Spotify release dates can be year-only or yyyy-MM-dd; the helper localizes
            // full dates and returns the raw string for partials, which must be escaped
            // since this value is injected into an HTML string template.
            var formatted = window.DateFmt.formatDateOnly(value);
            return formatted === value ? escapeHtml(value) : formatted;
        }

        function coverCell(imageUrl, label) {
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
                '<td class="bulk-save-pick-cell" data-label="Pick">' +
                    '<input type="checkbox" class="bulk-save-check" ' + (checked ? 'checked' : '') +
                    ' aria-label="Select album ' + escapeHtml(album.name) + '"></td>' +
                '<td data-label="Cover">' + coverCell(album.imageUrl) + '</td>' +
                '<td data-label="Album"><span class="bulk-save-name">' + escapeHtml(album.name) + '</span></td>' +
                '<td data-label="Artists"><span class="bulk-save-sub">' + escapeHtml(album.artists) + '</span></td>' +
                '<td data-label="Type"><span class="bulk-save-muted">' + escapeHtml(album.albumType) + '</span></td>' +
                '<td data-label="Released"><span class="bulk-save-muted">' + formatDate(rawRelease) + '</span></td>' +
                '<td data-label="Tracks"><span class="bulk-save-muted">' + escapeHtml(album.totalTracks) + '</span></td>' +
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
            var desc = playlist.description
                ? '<span class="bulk-save-desc">' + escapeHtml(playlist.description) + '</span>'
                : '<span class="bulk-save-muted">—</span>';
            return '<tr class="' + (checked ? 'is-selected' : '') + '" data-id="' + id + '">' +
                '<td class="bulk-save-pick-cell" data-label="Pick">' +
                    '<input type="checkbox" class="bulk-save-check" ' + (checked ? 'checked' : '') +
                    ' aria-label="Select playlist ' + escapeHtml(playlist.name) + '"></td>' +
                '<td data-label="Cover">' + coverCell(playlist.imageUrl) + '</td>' +
                '<td data-label="Name"><span class="bulk-save-name">' + escapeHtml(playlist.name) + '</span></td>' +
                '<td data-label="Owner"><span class="bulk-save-sub">' + escapeHtml(playlist.owner || 'Unknown') + '</span></td>' +
                '<td data-label="Tracks"><span class="bulk-save-muted">' + escapeHtml(playlist.tracks) + '</span></td>' +
                '<td data-label="Description">' + desc + '</td>' +
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
            } else if (countOf(selectedPlaylists) === 0) {
                playlistEmpty.textContent = 'No playlists selected yet.';
            } else {
                playlistEmpty.textContent = 'No playlists selected yet.';
            }
        }

        // Multi-field filter: name, owner, description, track count.
        function filterPlaylists(query) {
            var q = query.trim().toLowerCase();
            return allPlaylists.filter(function (p) {
                var haystack = [
                    p.name || '',
                    p.owner || '',
                    p.description || '',
                    String(p.tracks)
                ].join('  ').toLowerCase();
                return haystack.indexOf(q) !== -1;
            });
        }

        function renderPlaylistView() {
            var query = playlistSearch.value.trim();
            if (query === '') {
                var selected = Object.keys(selectedPlaylists).map(function (k) { return selectedPlaylists[k]; });
                renderPlaylists(selected);
                updatePlaylistEmptyState(selected.length, '');
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
                        return; // a newer search superseded this one
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
            if (albumSearchTimer) {
                clearTimeout(albumSearchTimer);
            }

            if (query.length < MIN_QUERY) {
                // Empty / too-short: show selected-only without a request.
                albumSearchSeq++; // cancel any pending render
                albumTableWrap.classList.remove('is-loading');
                albumSearchStatus.textContent = '';
                renderSelectedAlbums();
                return;
            }

            albumSearchTimer = setTimeout(function () {
                runAlbumSearch(query);
            }, SEARCH_DEBOUNCE);
        }

        // ---------- Toggle handlers (event delegation) ----------
        albumTableBody.addEventListener('change', function (e) {
            var checkbox = e.target.closest('.bulk-save-check');
            if (!checkbox) {
                return;
            }
            var row = checkbox.closest('tr[data-id]');
            if (!row) {
                return;
            }
            var id = row.dataset.id;
            if (checkbox.checked) {
                // Re-derive the album object from the row so selection survives searches.
                var album = albumFromRow(row, id);
                selectedAlbums[id] = album;
                row.classList.add('is-selected');
            } else {
                delete selectedAlbums[id];
                row.classList.remove('is-selected');
                // If the search bar is empty we are viewing selected-only: drop the row.
                if (albumSearch.value.trim().length < MIN_QUERY) {
                    renderSelectedAlbums();
                }
            }
            updateCounts();
        });

        // Reconstruct the album object from a rendered row (used when checking in search results).
        // Raw release date + image are stashed on the row so re-rendering stays lossless.
        function albumFromRow(row, id) {
            return {
                id: id,
                name: textOf(row, '[data-label="Album"]'),
                artists: textOf(row, '[data-label="Artists"]'),
                albumType: textOf(row, '[data-label="Type"]'),
                releaseDate: row.dataset.release || '',
                totalTracks: textOf(row, '[data-label="Tracks"]'),
                imageUrl: row.dataset.image || null
            };
        }

        function textOf(row, selector) {
            var el = row.querySelector(selector);
            return el ? el.textContent.trim() : '';
        }

        // Clicking anywhere on a row (not the checkbox itself) toggles it.
        function rowClickToggle(body) {
            body.addEventListener('click', function (e) {
                if (e.target.closest('.bulk-save-check') || e.target.tagName === 'A') {
                    return;
                }
                var row = e.target.closest('tr[data-id]');
                if (!row) {
                    return;
                }
                var checkbox = row.querySelector('.bulk-save-check');
                if (!checkbox) {
                    return;
                }
                checkbox.checked = !checkbox.checked;
                checkbox.dispatchEvent(new Event('change', { bubbles: true }));
            });
        }
        rowClickToggle(albumTableBody);
        rowClickToggle(playlistTableBody);

        playlistTableBody.addEventListener('change', function (e) {
            var checkbox = e.target.closest('.bulk-save-check');
            if (!checkbox) {
                return;
            }
            var row = checkbox.closest('tr[data-id]');
            if (!row) {
                return;
            }
            var id = row.dataset.id;
            var playlist = allPlaylists.find(function (p) { return p.id === id; });
            if (checkbox.checked) {
                if (playlist) {
                    selectedPlaylists[id] = playlist;
                }
                row.classList.add('is-selected');
            } else {
                delete selectedPlaylists[id];
                row.classList.remove('is-selected');
                if (playlistSearch.value.trim() === '') {
                    renderPlaylistView();
                }
            }
            updateCounts();
        });

        // ---------- Submit (mirrors Shuffle/Index) ----------
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

        // ---------- Playlist search input ----------
        playlistSearch.addEventListener('input', renderPlaylistView);
        albumSearch.addEventListener('input', onAlbumSearchInput);

        // ---------- Initial render ----------
        renderSelectedAlbums();
        renderPlaylistView();
        updateCounts();
    });
})();
