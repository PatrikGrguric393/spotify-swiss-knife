(function () {
    let activeRequest = null;

    function renderRows(rows) {
        const tbody = document.getElementById('artistResultsBody');
        if (!tbody) return;
        tbody.innerHTML = '';
        rows.forEach(r => {
            const tr = document.createElement('tr');

            const tdName = document.createElement('td');
            tdName.setAttribute('data-label', 'Name');
            tdName.textContent = r.name;

            const tdUrl = document.createElement('td');
            tdUrl.setAttribute('data-label', 'Spotify URL');
            const a = document.createElement('a');
            a.href = r.spotifyUrl || '#';
            a.target = '_blank';
            a.rel = 'noopener noreferrer';
            a.textContent = 'Open';
            tdUrl.appendChild(a);

            const tdDetails = document.createElement('td');
            tdDetails.setAttribute('data-label', 'Details');
            tdDetails.className = 'cell-actions';
            const btn = document.createElement('button');
            btn.type = 'button';
            btn.className = 'entity-detail-trigger btn';
            btn.textContent = 'View';

            // create details payload and script so the detail modal can read it
            const detailsId = 'artist-details-' + r.id;
            btn.setAttribute('data-details-id', detailsId);
            btn.setAttribute('data-entity-type', 'Artist');

            const script = document.createElement('script');
            script.type = 'application/json';
            script.id = detailsId;
            script.textContent = JSON.stringify({ Name: r.name, Id: r.id, SpotifyUrl: r.spotifyUrl });

            tdDetails.appendChild(btn);
            tdDetails.appendChild(script);

            const tdActions = document.createElement('td');
            tdActions.setAttribute('data-label', 'Actions');
            tdActions.className = 'cell-actions';
            const edit = document.createElement('a');
            edit.className = 'btn';
            edit.href = '/lib/artists/edit/' + encodeURIComponent(r.id);
            edit.textContent = 'Edit';
            const del = document.createElement('a');
            del.className = 'btn btn-danger';
            del.href = '/lib/artists/delete/' + encodeURIComponent(r.id);
            del.textContent = 'Delete';
            tdActions.appendChild(edit);
            tdActions.appendChild(del);

            tr.appendChild(tdName);
            tr.appendChild(tdUrl);
            tr.appendChild(tdDetails);
            tr.appendChild(tdActions);

            tbody.appendChild(tr);
        });
        // If no results, render an empty state row
        if (rows.length === 0) {
            const empty = document.createElement('tr');
            const td = document.createElement('td');
            td.setAttribute('colspan', '4');
            td.style.padding = '0.6rem 0.75rem';
            td.style.color = '#b6ffb6';
            td.textContent = 'No results';
            empty.appendChild(td);
            tbody.appendChild(empty);
        }
    }

    function renderError(message) {
        const status = document.getElementById('artistSearchStatus');
        if (status) {
            status.textContent = message;
        }

        const tbody = document.getElementById('artistResultsBody');
        if (!tbody) return;

        const existingErrorRow = tbody.querySelector('.artist-search-error-row');
        if (existingErrorRow) {
            existingErrorRow.remove();
        }

        const empty = document.createElement('tr');
        empty.className = 'artist-search-error-row';
        const td = document.createElement('td');
        td.setAttribute('colspan', '4');
        td.style.padding = '0.6rem 0.75rem';
        td.style.color = '#ffb3b3';
        td.textContent = message;
        empty.appendChild(td);
        tbody.appendChild(empty);
    }

    function setBusy(isBusy) {
        const table = document.querySelector('.entity-table');
        if (table) {
            table.setAttribute('aria-busy', isBusy ? 'true' : 'false');
        }
    }

    function restoreInitialRows(initialHtml) {
        const tbody = document.getElementById('artistResultsBody');
        if (!tbody) return;
        tbody.innerHTML = initialHtml;
    }

    function fetchSearch(q) {
        const status = document.getElementById('artistSearchStatus');
        const tbody = document.getElementById('artistResultsBody');
        if (!tbody) return;

        const query = (q || '').trim();

        if (activeRequest) {
            activeRequest.abort();
            activeRequest = null;
        }

        if (query.length === 0) {
            if (status) {
                status.textContent = '';
            }
            setBusy(false);
            return;
        }

        if (status) {
            status.textContent = 'Searching artists...';
        }
        setBusy(true);

        const controller = new AbortController();
        activeRequest = controller;

        const url = '/lib/artists/search?q=' + encodeURIComponent(query);
        fetch(url, { headers: { 'Accept': 'application/json' }, signal: controller.signal })
            .then(r => r.json())
            .then(data => {
                if (activeRequest !== controller) return;
                renderRows(data.map(item => ({ id: item.id, name: item.name, spotifyUrl: item.spotifyUrl })));
                if (status) {
                    status.textContent = data.length === 0 ? 'No artists found' : '';
                }
                setBusy(false);
            })
            .catch(err => {
                if (err && err.name === 'AbortError') return;
                console.error('Artist search failed', err);
                if (activeRequest !== controller) return;
                renderError('Search failed. Showing the current results.');
                setBusy(false);
            })
            .finally(() => {
                if (activeRequest === controller) {
                    activeRequest = null;
                }
            });
    }

    document.addEventListener('DOMContentLoaded', function () {
        const input = document.getElementById('artistSearch');
        const tbody = document.getElementById('artistResultsBody');
        if (!input) return;

        const initialRowsHtml = tbody ? tbody.innerHTML : '';

        let timer = 0;
        input.addEventListener('input', function () {
            clearTimeout(timer);
            const value = input.value;
            if ((value || '').trim().length === 0) {
                const status = document.getElementById('artistSearchStatus');
                if (status) {
                    status.textContent = '';
                }
                setBusy(false);
                restoreInitialRows(initialRowsHtml);
                return;
            }
            timer = setTimeout(() => fetchSearch(value), 250);
        });
    });
})();
