(function () {
    'use strict';

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

    function coverCell(imageUrl) {
        if (imageUrl) {
            return '<img class="bulk-save-cover" src="' + escapeHtml(imageUrl) +
                '" alt="" loading="lazy" onerror="this.style.display=\'none\';this.parentNode.classList.add(\'bulk-save-cover--placeholder\');this.parentNode.textContent=\'\\u266B\';">';
        }
        return '<span class="bulk-save-cover bulk-save-cover--placeholder" aria-hidden="true">♫</span>';
    }

    window.BulkGrid = {
        escapeHtml: escapeHtml,
        coverCell: coverCell
    };
})();
