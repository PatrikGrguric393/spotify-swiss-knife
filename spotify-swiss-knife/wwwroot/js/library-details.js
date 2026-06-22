document.addEventListener('DOMContentLoaded', function () {
    var detailBackdrop = document.getElementById('entityDetailBackdrop');
    var detailTitle = document.getElementById('entityDetailTitle');
    var detailContent = document.getElementById('entityDetailContent');
    var detailClose = document.getElementById('entityDetailClose');
    var lastTrigger = null;

    if (!detailBackdrop || !detailTitle || !detailContent || !detailClose) {
        return;
    }

    function openSelectedEntityFromQuery() {
        var selectedId = new URLSearchParams(window.location.search).get('selected');
        if (!selectedId) {
            return;
        }

        var row = Array.prototype.find.call(document.querySelectorAll('tr[data-details-id]'), function (tr) {
            var detailsId = tr.getAttribute('data-details-id') || '';
            return detailsId.endsWith(selectedId);
        });

        if (row) {
            lastTrigger = row;
            openDetails(row.getAttribute('data-entity-type') || 'Entity', row.getAttribute('data-details-id'));
        }
    }

    function closeDetails() {
        detailBackdrop.hidden = true;
        detailBackdrop.setAttribute('aria-hidden', 'true');
        document.body.classList.remove('entity-detail-open');

        if (lastTrigger && typeof lastTrigger.focus === 'function') {
            lastTrigger.focus();
        }
    }

    function formatLabel(key) {
        return String(key)
            .replace(/([a-z0-9])([A-Z])/g, '$1 $2')
            .replace(/_/g, ' ')
            .replace(/\s+/g, ' ')
            .trim();
    }

    function isLikelyUrl(value) {
        return /^(https?:\/\/|www\.)[^\s]+$/i.test(value);
    }

    function isLikelyIsoDate(value) {
        if (typeof value !== 'string') {
            return false;
        }

        var trimmed = value.trim();
        if (!/^\d{4}-\d{2}-\d{2}(?:[tT ]\d{2}:\d{2}(?::\d{2}(?:\.\d{1,3})?)?(?:Z|[+-]\d{2}:?\d{2})?)?$/.test(trimmed)) {
            return false;
        }

        var parsed = Date.parse(trimmed);
        return !Number.isNaN(parsed);
    }

    function formatDateValue(value) {
        var str = String(value);
        var hasTime = /[tT ]\d{2}:\d{2}/.test(str);
        return hasTime ? window.DateFmt.formatTimestamp(str) : window.DateFmt.formatDateMedium(str);
    }

    function formatValue(value) {
        var node = document.createElement('span');

        if (value === null || value === undefined || value === '') {
            node.textContent = 'N/A';
            node.className = 'entity-detail-value--empty';
            return node;
        }

        if (Array.isArray(value)) {
            if (value.length === 0) {
                node.textContent = 'N/A';
                node.className = 'entity-detail-value--empty';
                return node;
            }

            var list = document.createElement('ul');
            list.className = 'entity-detail-value-list';

            value.forEach(function (item) {
                var itemNode = document.createElement('li');
                itemNode.textContent = String(item);
                list.appendChild(itemNode);
            });

            return list;
        }

        if (typeof value === 'boolean') {
            node.textContent = value ? 'Yes' : 'No';
            node.className = value ? 'entity-detail-value--boolean-true' : 'entity-detail-value--boolean-false';
            return node;
        }

        if (typeof value === 'number' && Number.isFinite(value)) {
            node.textContent = new Intl.NumberFormat().format(value);
            node.className = 'entity-detail-value--number';
            return node;
        }

        if (typeof value === 'object') {
            node.textContent = JSON.stringify(value, null, 2);
            node.className = 'entity-detail-value--object';
            return node;
        }

        var text = String(value).trim();

        if (text === '') {
            node.textContent = 'N/A';
            node.className = 'entity-detail-value--empty';
            return node;
        }

        if (isLikelyUrl(text)) {
            var link = document.createElement('a');
            var href = /^https?:\/\//i.test(text) ? text : 'https://' + text;
            link.href = href;
            link.target = '_blank';
            link.rel = 'noopener noreferrer';
            link.textContent = text;
            link.setAttribute('aria-label', 'Open link: ' + text);
            return link;
        }

        if (isLikelyIsoDate(text)) {
            node.textContent = formatDateValue(text);
            node.className = 'entity-detail-value--date';
            return node;
        }

        node.textContent = text;
        return node;
    }

    function renderDetailList(data) {
        detailContent.textContent = '';

        var list = document.createElement('dl');
        list.className = 'entity-detail-list';

        var keys = Object.keys(data).sort(function (a, b) {
            if (a === 'Name') {
                return -1;
            }

            if (b === 'Name') {
                return 1;
            }

            return a.localeCompare(b);
        });

        keys.forEach(function (key) {
            var label = document.createElement('dt');
            label.textContent = formatLabel(key);

            var value = document.createElement('dd');
            value.appendChild(formatValue(data[key]));

            list.appendChild(label);
            list.appendChild(value);
        });

        detailContent.appendChild(list);
    }

    function openDetails(entityType, detailsId) {
        var detailsNode = document.getElementById(detailsId);
        if (!detailsNode) {
            return;
        }

        var parsed;
        try {
            parsed = JSON.parse(detailsNode.textContent || '{}');
        } catch (_error) {
            parsed = { Error: 'Unable to parse entity details.' };
        }

        var displayName = parsed.Name || parsed.Id || 'Selected Entity';
        detailTitle.textContent = entityType + ' Details: ' + displayName;
        renderDetailList(parsed);

        detailBackdrop.hidden = false;
        detailBackdrop.setAttribute('aria-hidden', 'false');
        document.body.classList.add('entity-detail-open');
        detailClose.focus();
    }

    document.addEventListener('click', function (e) {
        if (e.target.closest('a, button')) return;
        var row = e.target.closest && e.target.closest('tr[data-details-id]');
        if (!row) return;

        var detailsId = row.getAttribute('data-details-id');
        var entityType = row.getAttribute('data-entity-type') || 'Entity';
        if (!detailsId) return;

        lastTrigger = row;
        openDetails(entityType, detailsId);
    });

    document.addEventListener('keydown', function (e) {
        if (e.key !== 'Enter' && e.key !== ' ') return;
        var row = e.target.closest && e.target.closest('tr[data-details-id]');
        if (!row) return;

        e.preventDefault();
        var detailsId = row.getAttribute('data-details-id');
        var entityType = row.getAttribute('data-entity-type') || 'Entity';
        if (!detailsId) return;

        lastTrigger = row;
        openDetails(entityType, detailsId);
    });

    detailClose.addEventListener('click', closeDetails);

    detailBackdrop.addEventListener('click', function (event) {
        if (event.target === detailBackdrop) {
            closeDetails();
        }
    });

    document.addEventListener('keydown', function (event) {
        if (event.key === 'Escape' && !detailBackdrop.hidden) {
            closeDetails();
        }
    });

    openSelectedEntityFromQuery();
});
