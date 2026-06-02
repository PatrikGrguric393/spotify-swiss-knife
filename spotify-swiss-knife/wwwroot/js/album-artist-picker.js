document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('[data-album-artist-picker]').forEach((picker) => {
        const searchInput = picker.querySelector('[data-album-artist-search]');
        const rows = Array.from(picker.querySelectorAll('[data-album-artist-row]'));
        const checkboxes = () => Array.from(picker.querySelectorAll('[data-album-artist-checkbox]'));
        const status = picker.querySelector('#albumArtistPickerStatus') || null;
        const list = picker.querySelector('.album-track-picker-list');

        if (!searchInput || !list) {
            return;
        }

        const emptyState = document.createElement('div');
        emptyState.className = 'album-track-picker-empty';
        emptyState.textContent = 'No artists match this search.';
        emptyState.hidden = true;
        list.appendChild(emptyState);

        const getChecked = () => checkboxes().filter((checkbox) => checkbox.checked);

        const updateStatus = () => {
            const visibleRows = rows.filter((row) => !row.hidden);
            const selectedCount = getChecked().length;
            status.textContent = `${visibleRows.length} artist${visibleRows.length === 1 ? '' : 's'} shown, ${selectedCount} selected.`;
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