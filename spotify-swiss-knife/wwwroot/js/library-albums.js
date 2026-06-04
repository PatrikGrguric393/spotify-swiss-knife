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

            const tdArtists = document.createElement('td');
            tdArtists.setAttribute('data-label', 'Artists');
            tdArtists.textContent = r.artists;

            const tdDate = document.createElement('td');
            tdDate.setAttribute('data-label', 'Release Date');
            tdDate.textContent = r.releaseDate;

            const tdDetails = document.createElement('td');
            tdDetails.setAttribute('data-label', 'Details');
            tdDetails.className = 'cell-actions';
            const btn = document.createElement('button');
            btn.type = 'button';
            btn.className = 'entity-detail-trigger btn';
            btn.textContent = 'View';

            const detailsId = 'album-details-' + r.id;
            btn.setAttribute('data-details-id', detailsId);
            btn.setAttribute('data-entity-type', 'Album');

            const script = document.createElement('script');
            script.type = 'application/json';
            script.id = detailsId;
            script.textContent = JSON.stringify({ Name: r.name, Artists: r.artists, ReleaseDate: r.releaseDate });

            tdDetails.appendChild(btn);
            tdDetails.appendChild(script);

            const tdActions = document.createElement('td');
            tdActions.setAttribute('data-label', 'Actions');
            tdActions.className = 'cell-actions';
            const edit = document.createElement('a');
            edit.className = 'btn';
            edit.href = '/lib/albums/edit/' + encodeURIComponent(r.id);
            edit.textContent = 'Edit';
            const del = document.createElement('a');
            del.className = 'btn btn-danger';
            del.href = '/lib/albums/delete/' + encodeURIComponent(r.id);
            del.textContent = 'Delete';
            tdActions.appendChild(edit);
            tdActions.appendChild(del);

            tr.appendChild(tdName);
            tr.appendChild(tdArtists);
            tr.appendChild(tdDate);
            tr.appendChild(tdDetails);
            tr.appendChild(tdActions);

            tbody.appendChild(tr);
        });
        if (rows.length === 0) {
            const empty = document.createElement('tr');
            const td = document.createElement('td');
            td.setAttribute('colspan', '5');
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
            td.setAttribute('colspan', '5');
            td.style.padding = '0.6rem 0.75rem';
            td.style.color = '#b6ffb6';
            td.textContent = 'Loading...';
            loadingRow.appendChild(td);
            tbody.appendChild(loadingRow);
        }

        const params = new URLSearchParams();
        params.set('q', q || '');

        const dateFrom = document.getElementById('albumDateFrom');
        const dateTo = document.getElementById('albumDateTo');
        if (dateFrom && dateFrom.value) params.set('dateFrom', dateFrom.value);
        if (dateTo && dateTo.value) params.set('dateTo', dateTo.value);

        fetch('/lib/albums/search?' + params.toString(), { headers: { 'Accept': 'application/json' } })
            .then(r => r.json())
            .then(data => {
                renderRows(data.map(item => ({ id: item.id, name: item.name, artists: item.artists, releaseDate: item.releaseDate })));
                if (status) status.textContent = data.length === 0 ? 'No results' : '';
                const firstDetail = document.querySelector('.entity-table tbody .entity-detail-trigger');
                if (firstDetail) {
                    firstDetail.setAttribute('tabindex', '0');
                }
            })
            .catch(err => console.error('Album search failed', err));
    }

    document.addEventListener('DOMContentLoaded', function () {
        const input = document.getElementById('albumSearch');
        if (!input) return;

        let timer = 0;

        input.addEventListener('input', function () {
            clearTimeout(timer);
            timer = setTimeout(() => fetchSearch(input.value), 250);
        });

        input.addEventListener('keydown', function (e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                const first = document.querySelector('.entity-table tbody .entity-detail-trigger');
                if (first && typeof first.focus === 'function') first.focus();
            }
        });

        const dateFrom = document.getElementById('albumDateFrom');
        const dateTo = document.getElementById('albumDateTo');
        const clearBtn = document.getElementById('albumDateClear');

        if (dateFrom) {
            dateFrom.addEventListener('input', function () {
                clearTimeout(timer);
                timer = setTimeout(() => fetchSearch(input.value), 250);
            });
        }

        if (dateTo) {
            dateTo.addEventListener('input', function () {
                clearTimeout(timer);
                timer = setTimeout(() => fetchSearch(input.value), 250);
            });
        }

        if (clearBtn) {
            clearBtn.addEventListener('click', function () {
                if (dateFrom) dateFrom.value = '';
                if (dateTo) dateTo.value = '';
                clearTimeout(timer);
                fetchSearch(input.value);
            });
        }

        fetchSearch('');
    });
})();
