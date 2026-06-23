(function () {
    function renderRows(rows) {
        const tbody = document.querySelector('.entity-table tbody');
        if (!tbody) return;
        tbody.innerHTML = '';
        rows.forEach(r => {
            const detailsId = 'artist-details-' + r.id;
            const tr = document.createElement('tr');
            tr.setAttribute('tabindex', '0');
            tr.setAttribute('data-details-id', detailsId);
            tr.setAttribute('data-entity-type', 'Artist');
            tr.setAttribute('aria-label', 'View details for ' + r.name);

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

            const script = document.createElement('script');
            script.type = 'application/json';
            script.id = detailsId;
            script.textContent = JSON.stringify({ Name: r.name, Id: r.id, SpotifyUrl: r.spotifyUrl });

            tdActions.appendChild(edit);
            tdActions.appendChild(del);
            tdActions.appendChild(script);
            tr.appendChild(tdName);
            tr.appendChild(tdUrl);
            tr.appendChild(tdActions);
            tbody.appendChild(tr);
        });
        if (rows.length === 0) {
            tbody.appendChild(window.LibraryList.makeStatusRow(3, 'No results'));
        }
    }

    function doSearch(q, opts) {
        opts = opts || {};
        const status = document.querySelector('.search-status');
        const tbody = document.querySelector('.entity-table tbody');
        if (!opts.quiet) {
            if (status) status.textContent = 'Loading...';
            if (tbody) {
                tbody.innerHTML = '';
                tbody.appendChild(window.LibraryList.makeStatusRow(3, 'Loading...'));
            }
        }
        const url = '/lib/artists/search?q=' + encodeURIComponent(q || '');
        return fetch(url, { headers: { 'Accept': 'application/json' } })
            .then(r => r.json())
            .then(data => {
                const scroll = document.querySelector('.table-wrap');
                const top = scroll ? scroll.scrollTop : 0;
                renderRows(data.map(item => ({ id: item.id, name: item.name, spotifyUrl: item.spotifyUrl })));
                if (scroll) scroll.scrollTop = top;
                if (status) status.textContent = data.length === 0 ? 'No results' : '';
            })
            .catch(err => console.error('Artist search failed', err));
    }

    document.addEventListener('DOMContentLoaded', function () {
        window.LibraryList.setup({
            searchInputId: 'artistSearch',
            rowSelector: '.entity-table tbody tr[data-details-id]',
            doSearch: doSearch
        });
    });
})();
