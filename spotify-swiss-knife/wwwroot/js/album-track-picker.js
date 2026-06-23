document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('[data-album-track-picker]').forEach((picker) => {
        const albumTypeSelect = picker.querySelector('[data-album-type-select]') || document.querySelector('[data-album-type-select]');
        const albumTypeHelp = picker.querySelector('[data-album-type-help]') || document.querySelector('[data-album-type-help]') || null;
        const searchInput = picker.querySelector('[data-album-track-search]');
        const rows = Array.from(picker.querySelectorAll('[data-album-track-row]'));
        const checkboxes = () => Array.from(picker.querySelectorAll('[data-album-track-checkbox]'));
        const status = picker.querySelector('#albumTrackPickerStatus') || null;
        const hadPickerHelp = !!picker.querySelector('[data-album-track-help]');
        let pickerHelp = picker.querySelector('[data-album-track-help]') || null;
        let pickerRule = picker.querySelector('[data-album-track-constraint]') || null;
        const list = picker.querySelector('.album-track-picker-list');
        const allowedTypes = new Set(['album', 'single', 'compilation']);

        if (!albumTypeSelect || !searchInput || !list) {
            return;
        }

        if (!pickerRule) {
            pickerRule = document.createElement('p');
            pickerRule.setAttribute('data-album-track-constraint', '');
            pickerRule.setAttribute('role', 'status');
            pickerRule.setAttribute('aria-live', 'polite');
            pickerRule.className = 'album-track-picker-rule';
            const fieldset = picker.querySelector('fieldset') || picker;
            fieldset.insertBefore(pickerRule, fieldset.querySelector('.album-track-picker-search-label') || null);
        }

        if (!pickerHelp) {
            pickerHelp = document.createElement('p');
            pickerHelp.setAttribute('data-album-track-help', '');
            pickerHelp.className = 'album-track-picker-help';
            // keep it non-intrusive when created programmatically; do not reveal it
            pickerHelp.hidden = true;
            const fieldset = picker.querySelector('fieldset') || picker;
            fieldset.insertBefore(pickerHelp, pickerRule.nextSibling || null);
        }

        const emptyState = document.createElement('div');
        emptyState.className = 'album-track-picker-empty';
        emptyState.textContent = 'No songs match this search.';
        emptyState.hidden = true;
        list.appendChild(emptyState);

        const getType = () => albumTypeSelect.value.trim().toLowerCase();

        const isTypeSelected = () => allowedTypes.has(getType());

        const getChecked = () => checkboxes().filter((checkbox) => checkbox.checked);

        const setControlsEnabled = (enabled) => {
            searchInput.disabled = !enabled;
            checkboxes().forEach((checkbox) => {
                checkbox.disabled = !enabled;
            });
            picker.classList.toggle('album-track-picker-locked', !enabled);
        };

        const updateTypeHelp = (type) => {
            if (albumTypeHelp) {
                if (type === 'single') {
                    albumTypeHelp.textContent = 'Single mode requires exactly one song.';
                } else if (type === 'album' || type === 'compilation') {
                    albumTypeHelp.textContent = 'You can select multiple songs for this type.';
                } else {
                    albumTypeHelp.textContent = 'Choose album type first. Singles allow one song; album and compilation allow multiple songs.';
                }
            }

            if (pickerHelp) {
                // Only reveal an existing picker help that was present in the DOM originally.
                if (hadPickerHelp) {
                    pickerHelp.hidden = false;
                }
                if (type === 'single') {
                    pickerHelp.textContent = 'Select exactly one song for this single.';
                } else if (type === 'album' || type === 'compilation') {
                    pickerHelp.textContent = 'Search songs, then check every track you want on this release.';
                } else {
                    pickerHelp.textContent = 'Choose album type to enable song selection.';
                }
            }
        };

        const updateRuleText = (type, overrideText = '') => {
            if (!pickerRule) return;
            if (overrideText) {
                pickerRule.textContent = overrideText;
                return;
            }

            if (type === 'single') {
                pickerRule.textContent = 'Only one song can be selected for a single.';
                return;
            }

            if (type === 'album' || type === 'compilation') {
                pickerRule.textContent = 'Multiple songs can be selected.';
                return;
            }

            pickerRule.textContent = 'Choose album type to enable song selection.';
        };

        const enforceTrackRules = (type, changedCheckbox = null) => {
            if (type !== 'single') {
                return;
            }

            const checked = getChecked();
            if (checked.length <= 1) {
                return;
            }

            if (changedCheckbox && changedCheckbox.checked) {
                checked.forEach((checkbox) => {
                    if (checkbox !== changedCheckbox) {
                        checkbox.checked = false;
                    }
                });
                updateRuleText(type, 'Only one song is allowed for a single. Keeping your latest selection.');
                return;
            }

            checked.slice(1).forEach((checkbox) => {
                checkbox.checked = false;
            });
            updateRuleText(type, 'Only one song is allowed for a single. Keeping your first selection.');
        };

        const updateStatus = () => {
            const type = getType();
            if (!isTypeSelected()) {
                status.textContent = 'Choose album type to enable song selection.';
                emptyState.hidden = true;
                return;
            }

            const visibleRows = rows.filter((row) => !row.hidden);
            const selectedCount = getChecked().length;
            const modeText = type === 'single' ? 'Single mode' : 'Multi-track mode';
            status.textContent = `${modeText}: ${visibleRows.length} song${visibleRows.length === 1 ? '' : 's'} shown, ${selectedCount} selected.`;
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

        const applyAlbumTypeMode = () => {
            const type = getType();
            const selected = isTypeSelected();

            setControlsEnabled(selected);
            updateTypeHelp(type);

            if (!selected) {
                updateRuleText(type);
                updateStatus();
                return;
            }

            enforceTrackRules(type);
            updateRuleText(type);
            updateStatus();
        };

        picker.addEventListener('change', (event) => {
            if (event.target.matches('[data-album-track-checkbox]')) {
                const type = getType();
                enforceTrackRules(type, event.target);
                updateStatus();
            }
        });

        albumTypeSelect.addEventListener('change', applyAlbumTypeMode);
        // also listen for input to catch any programmatic or IME changes immediately
        albumTypeSelect.addEventListener('input', applyAlbumTypeMode);

        // Also react to album-type changes from anywhere in the document to ensure
        // the picker is enabled when the global album type select changes.
        document.addEventListener('change', (evt) => {
            if (evt.target && evt.target.matches && evt.target.matches('[data-album-type-select]')) {
                applyAlbumTypeMode();
            }
        });
        searchInput.addEventListener('input', applyFilter);

        window.albumTrackPickerApplyAlbumType = applyAlbumTypeMode;

        applyFilter();
        applyAlbumTypeMode();
    });
});