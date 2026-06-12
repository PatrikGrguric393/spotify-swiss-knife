(function () {
    var locale = navigator.language || 'en';
    var triggerFmt = new Intl.DateTimeFormat(locale, { day: '2-digit', month: 'short', year: 'numeric' });

    function formatReleaseDate(val) {
        if (!val) return '';
        var parts = val.split('-');
        if (parts.length === 3) {
            var d = new Date(parseInt(parts[0], 10), parseInt(parts[1], 10) - 1, parseInt(parts[2], 10));
            return triggerFmt.format(d);
        }
        return val;
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
            var detailsId = 'album-details-' + r.id;
            var tr = document.createElement('tr');
            tr.setAttribute('tabindex', '0');
            tr.setAttribute('data-details-id', detailsId);
            tr.setAttribute('data-entity-type', 'Album');
            tr.setAttribute('aria-label', 'View details for ' + r.name);

            var tdName = document.createElement('td');
            tdName.setAttribute('data-label', 'Name');
            var nameWrap = document.createElement('div');
            nameWrap.className = 'album-row-name';
            if (r.hasCover) {
                var thumb = document.createElement('img');
                thumb.className = 'album-row-thumb';
                thumb.src = '/lib/albums/cover/' + encodeURIComponent(r.id);
                thumb.alt = '';
                thumb.loading = 'lazy';
                nameWrap.appendChild(thumb);
            } else {
                var placeholder = document.createElement('span');
                placeholder.className = 'album-row-thumb album-row-thumb--placeholder';
                placeholder.setAttribute('aria-hidden', 'true');
                placeholder.innerHTML = '&#9835;';
                nameWrap.appendChild(placeholder);
            }
            var nameTitle = document.createElement('span');
            nameTitle.className = 'album-row-title';
            nameTitle.textContent = r.name;
            nameWrap.appendChild(nameTitle);
            tdName.appendChild(nameWrap);

            var tdArtists = document.createElement('td');
            tdArtists.setAttribute('data-label', 'Artists');
            tdArtists.textContent = r.artists;

            var tdDate = document.createElement('td');
            tdDate.setAttribute('data-label', 'Release Date');
            tdDate.textContent = formatReleaseDate(r.releaseDate);

            var tdActions = document.createElement('td');
            tdActions.setAttribute('data-label', 'Actions');
            tdActions.className = 'cell-actions';
            var edit = document.createElement('a');
            edit.className = 'btn';
            edit.href = '/lib/albums/edit/' + encodeURIComponent(r.id);
            edit.textContent = 'Edit';
            var del = document.createElement('a');
            del.className = 'btn btn-danger';
            del.href = '/lib/albums/delete/' + encodeURIComponent(r.id);
            del.textContent = 'Delete';

            var script = document.createElement('script');
            script.type = 'application/json';
            script.id = detailsId;
            var detailData = { Name: r.name, Artists: r.artists, ReleaseDate: r.releaseDate };
            if (r.hasCover) detailData.Cover = '/lib/albums/cover/' + encodeURIComponent(r.id);
            script.textContent = JSON.stringify(detailData);

            tdActions.appendChild(edit);
            tdActions.appendChild(del);
            tdActions.appendChild(script);
            tr.appendChild(tdName);
            tr.appendChild(tdArtists);
            tr.appendChild(tdDate);
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

        var dateFrom = document.getElementById('albumDateFrom');
        var dateTo = document.getElementById('albumDateTo');
        if (dateFrom && dateFrom.value) params.set('dateFrom', dateFrom.value);
        if (dateTo && dateTo.value) params.set('dateTo', dateTo.value);

        return fetch('/lib/albums/search?' + params.toString(), { headers: { 'Accept': 'application/json' } })
            .then(function(r) { return r.json(); })
            .then(function(data) {
                var scroll = document.querySelector('.table-wrap');
                var top = scroll ? scroll.scrollTop : 0;
                renderRows(data.map(function(item) {
                    return { id: item.id, name: item.name, artists: item.artists, releaseDate: item.releaseDate, hasCover: item.hasCover };
                }));
                if (scroll) scroll.scrollTop = top;
                if (status) status.textContent = data.length === 0 ? 'No results' : '';
            })
            .catch(function(err) { console.error('Album search failed', err); });
    }

    function fetchSearch(q) { return doSearch(q, {}); }

    document.addEventListener('DOMContentLoaded', function() {
        document.querySelectorAll('.entity-table td[data-date]').forEach(function(td) {
            td.textContent = formatReleaseDate(td.dataset.date);
        });

        var searchInput = document.getElementById('albumSearch');
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

        var onDateSelect = function() { fetchSearch(searchInput.value); };

        window.Dpc.selfInit = true;
        window.Dpc.initDpcWrappers(onDateSelect);
        document.querySelectorAll('.dpc').forEach(function(el) {
            window.Dpc.buildCalendar(el, onDateSelect);
        });

        var clearBtn = document.getElementById('albumDateClear');
        if (clearBtn) {
            clearBtn.addEventListener('click', function() {
                var fromHidden = document.getElementById('albumDateFrom');
                var toHidden = document.getElementById('albumDateTo');
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
