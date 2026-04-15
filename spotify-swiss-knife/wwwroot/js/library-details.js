document.addEventListener('DOMContentLoaded', function () {
    var detailBackdrop = document.getElementById('entityDetailBackdrop');
    var detailTitle = document.getElementById('entityDetailTitle');
    var detailContent = document.getElementById('entityDetailContent');
    var detailClose = document.getElementById('entityDetailClose');
    var detailButtons = document.querySelectorAll('.entity-detail-trigger');

    if (!detailBackdrop || !detailTitle || !detailContent || !detailClose || detailButtons.length === 0) {
        return;
    }

    function closeDetails() {
        detailBackdrop.hidden = true;
        document.body.classList.remove('entity-detail-open');
    }

    function formatLabel(key) {
        return String(key)
            .replace(/([a-z0-9])([A-Z])/g, '$1 $2')
            .replace(/_/g, ' ')
            .replace(/\s+/g, ' ')
            .trim();
    }

    function formatValue(value) {
        if (value === null || value === undefined || value === '') {
            return 'N/A';
        }

        if (Array.isArray(value)) {
            return value.length > 0 ? value.join(', ') : 'N/A';
        }

        return String(value);
    }

    function renderDetailList(data) {
        detailContent.textContent = '';

        var list = document.createElement('dl');
        list.className = 'entity-detail-list';

        Object.keys(data).forEach(function (key) {
            var label = document.createElement('dt');
            label.textContent = formatLabel(key);

            var value = document.createElement('dd');
            value.textContent = formatValue(data[key]);

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
        document.body.classList.add('entity-detail-open');
        detailClose.focus();
    }

    detailButtons.forEach(function (button) {
        button.addEventListener('click', function () {
            var detailsId = button.getAttribute('data-details-id');
            var entityType = button.getAttribute('data-entity-type') || 'Entity';

            if (!detailsId) {
                return;
            }

            openDetails(entityType, detailsId);
        });
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
});
