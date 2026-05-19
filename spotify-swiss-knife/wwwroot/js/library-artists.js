(function () {
    function renderRows(rows) {
        const tbody = document.querySelector('.entity-table tbody');
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

    function fetchSearch(q) {
        const status = document.querySelector('.search-status');
        if (status) {
            status.textContent = 'Loading...';
        }
        const tbody = document.querySelector('.entity-table tbody');
        if (tbody) {
            tbody.innerHTML = '';
            const loadingRow = document.createElement('tr');
            const td = document.createElement('td');
            td.setAttribute('colspan', '4');
            td.style.padding = '0.6rem 0.75rem';
            td.style.color = '#b6ffb6';
            td.textContent = 'Loading...';
            loadingRow.appendChild(td);
            tbody.appendChild(loadingRow);
        }
        const url = '/lib/artists/search?q=' + encodeURIComponent(q || '');
        fetch(url, { headers: { 'Accept': 'application/json' } })
            .then(r => r.json())
            .then(data => {
                renderRows(data.map(item => ({ id: item.id, name: item.name, spotifyUrl: item.spotifyUrl })));
                if (status) status.textContent = data.length === 0 ? 'No results' : '';
                // after rendering, make first view button focusable via Enter
                const firstDetail = document.querySelector('.entity-table tbody .entity-detail-trigger');
                if (firstDetail) {
                    firstDetail.setAttribute('tabindex', '0');
                }
            })
            .catch(err => console.error('Artist search failed', err));
    }

    document.addEventListener('DOMContentLoaded', function () {
        const input = document.getElementById('artistSearch');
        if (!input) return;

        let timer = 0;
        input.addEventListener('input', function () {
            clearTimeout(timer);
            timer = setTimeout(() => fetchSearch(input.value), 250);
        });

        // Enter key focuses first result's View button (accessibility)
        input.addEventListener('keydown', function (e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                const first = document.querySelector('.entity-table tbody .entity-detail-trigger');
                if (first && typeof first.focus === 'function') first.focus();
            }
        });

        // initial load
        fetchSearch('');
    });
})();
