(function () {
    var locale = navigator.language || 'en';
    var timestampFmt = new Intl.DateTimeFormat(locale, { dateStyle: 'short', timeStyle: 'short' });
    var dateOnlyFmt = new Intl.DateTimeFormat(locale, { dateStyle: 'short' });
    var dateShortFmt = new Intl.DateTimeFormat(locale, { dateStyle: 'short' });

    function formatTimestamp(isoUtc) {
        if (!isoUtc) return '';
        var parsed = new Date(isoUtc);
        if (Number.isNaN(parsed.getTime())) return isoUtc;
        return timestampFmt.format(parsed);
    }

    function formatDateOnly(val) {
        if (!val) return '';
        var parts = String(val).split('-');
        // Only full YYYY-MM-DD is localized; partial dates (YYYY, YYYY-MM) fall back to raw.
        if (parts.length === 3) {
            // Build from local components so a date-only value isn't shifted by timezone.
            var d = new Date(parseInt(parts[0], 10), parseInt(parts[1], 10) - 1, parseInt(parts[2], 10));
            if (Number.isNaN(d.getTime())) return val;
            return dateOnlyFmt.format(d);
        }
        return val;
    }

    function formatDateShort(val) {
        if (!val) return '';
        var parts = String(val).split('-');
        var d;
        if (parts.length === 3) {
            d = new Date(parseInt(parts[0], 10), parseInt(parts[1], 10) - 1, parseInt(parts[2], 10));
        } else {
            d = new Date(val);
        }
        if (Number.isNaN(d.getTime())) return val;
        return dateShortFmt.format(d);
    }

    function localizePending(root) {
        root = root || document;
        // Covers <time class="shuffle-time"> and bare td[data-utc] cells.
        root.querySelectorAll('[data-utc]').forEach(function (el) {
            el.textContent = formatTimestamp(el.dataset.utc);
            el.classList.remove('shuffle-time--pending');
        });
        // Scoped to .shuffle-time so datepicker day buttons ([data-date] without the class) are never rewritten.
        root.querySelectorAll('.shuffle-time[data-date]').forEach(function (el) {
            el.textContent = formatDateOnly(el.dataset.date);
            el.classList.remove('shuffle-time--pending');
        });
    }

    window.DateFmt = {
        locale: locale,
        formatTimestamp: formatTimestamp,
        formatDateOnly: formatDateOnly,
        formatDateShort: formatDateShort,
        localizePending: localizePending
    };
})();
