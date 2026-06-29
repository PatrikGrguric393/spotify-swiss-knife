(function () {
    // Prevent album-form-datepicker.js auto-init from running before this module
    // can call initDateRange with the correct onSelect callback. Without this,
    // initDpcWrappers is called twice (once with null, once with onSelect), adding
    // two click listeners per trigger — causing the panel to open and immediately close.
    if (window.Dpc) window.Dpc.selfInit = true;

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
                thumb.src = '/lib/albums/cover/' + encodeURIComponent(r.id) + (r.coverFileName ? '?v=' + encodeURIComponent(r.coverFileName) : '');
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
            var dateEl = document.createElement('time');
            dateEl.className = 'shuffle-time';
            dateEl.setAttribute('data-date', r.releaseDate || '');
            dateEl.textContent = window.DateFmt.formatDateOnly(r.releaseDate);
            tdDate.appendChild(dateEl);

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
            if (r.hasCover) detailData.Cover = '/lib/albums/cover/' + encodeURIComponent(r.id) + (r.coverFileName ? '?v=' + encodeURIComponent(r.coverFileName) : '');
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
            tbody.appendChild(window.LibraryList.makeStatusRow(4, 'No results'));
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
                tbody.appendChild(window.LibraryList.makeStatusRow(4, 'Loading...'));
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
                    return { id: item.id, name: item.name, artists: item.artists, releaseDate: item.releaseDate, hasCover: item.hasCover, coverFileName: item.coverFileName };
                }));
                if (scroll) scroll.scrollTop = top;
                if (status) status.textContent = data.length === 0 ? 'No results' : '';
            })
            .catch(function(err) { console.error('Album search failed', err); });
    }

    document.addEventListener('DOMContentLoaded', function() {
        var table = document.querySelector('.entity-table');
        if (table) window.DateFmt.localizePending(table);

        window.LibraryList.setup({
            searchInputId: 'albumSearch',
            rowSelector: '.entity-table tbody tr[data-details-id]',
            doSearch: doSearch,
            onReady: function (ctx) {
                var searchInput = ctx.input;
                var onDateSelect = function() { ctx.fetchSearch(searchInput.value); };
                window.Dpc.initDateRange(onDateSelect, {
                    fromHiddenId: 'albumDateFrom',
                    toHiddenId: 'albumDateTo',
                    clearBtnId: 'albumDateClear',
                    triggerFromId: 'triggerFrom',
                    triggerToId: 'triggerTo',
                    timerCtx: ctx
                });
            }
        });
    });
})();
