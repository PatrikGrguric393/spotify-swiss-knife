// Generic "filter checkbox list + status text + empty state" widget.
// Unifies the previously near-identical album-artist-picker.js and
// playlist-track-picker.js. Each entry in PICKER_TYPES describes how a given
// picker variant locates its elements and what wording it uses.
document.addEventListener('DOMContentLoaded', () => {
    const PICKER_TYPES = [
        {
            pickerAttr: 'data-album-artist-picker',
            searchAttr: 'data-album-artist-search',
            rowAttr: 'data-album-artist-row',
            checkboxAttr: 'data-album-artist-checkbox',
            statusSelector: '.album-track-picker-status',
            emptyText: 'No artists match this search.',
            noun: 'artist'
        },
        {
            pickerAttr: 'data-playlist-track-picker',
            searchAttr: 'data-playlist-track-search',
            rowAttr: 'data-playlist-track-row',
            checkboxAttr: 'data-playlist-track-checkbox',
            statusSelector: '#playlistTrackPickerStatus',
            emptyText: 'No songs match this search.',
            noun: 'song'
        }
    ];

    PICKER_TYPES.forEach((type) => {
        document.querySelectorAll(`[${type.pickerAttr}]`).forEach((picker) => {
            const searchInput = picker.querySelector(`[${type.searchAttr}]`);
            const rows = Array.from(picker.querySelectorAll(`[${type.rowAttr}]`));
            const checkboxes = () => Array.from(picker.querySelectorAll(`[${type.checkboxAttr}]`));
            const status = picker.querySelector(type.statusSelector);
            const list = picker.querySelector('.album-track-picker-list');

            if (!searchInput || !list) {
                return;
            }

            const emptyState = document.createElement('div');
            emptyState.className = 'album-track-picker-empty';
            emptyState.textContent = type.emptyText;
            emptyState.hidden = true;
            list.appendChild(emptyState);

            const getChecked = () => checkboxes().filter((checkbox) => checkbox.checked);

            const updateStatus = () => {
                const visibleRows = rows.filter((row) => !row.hidden);
                const selectedCount = getChecked().length;
                if (status) {
                    status.textContent = `${visibleRows.length} ${type.noun}${visibleRows.length === 1 ? '' : 's'} shown, ${selectedCount} selected.`;
                }
                emptyState.hidden = visibleRows.length !== 0;
            };

            const applyFilter = () => {
                const query = (searchInput.value || '').trim().toLowerCase();

                rows.forEach((row) => {
                    const text = (row.getAttribute('data-search-text') || '').toLowerCase();
                    row.hidden = query.length > 0 && !text.includes(query);
                });

                updateStatus();
            };

            searchInput.addEventListener('input', applyFilter);
            applyFilter();
        });
    });
});
