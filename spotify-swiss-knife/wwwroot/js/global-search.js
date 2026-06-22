document.addEventListener('DOMContentLoaded', function () {
    var shell = document.querySelector('[data-global-search]');

    if (!shell) {
        return;
    }

    var input = shell.querySelector('[data-search-input]');
    var clearButton = shell.querySelector('[data-search-clear]');
    var panel = shell.querySelector('[data-search-panel]');
    var status = shell.querySelector('[data-search-status]');
    var results = shell.querySelector('[data-search-results]');
    var placeholder = shell.querySelector('[data-search-placeholder]');
    var placeholderText = shell.querySelector('[data-search-placeholder-text]');
    var endpoint = shell.getAttribute('data-search-endpoint') || '/search';
    var minLength = 2;
    var debounceTimer = 0;
    var controller = null;
    var links = [];
    var activeIndex = -1;
    var resizeObserver = null;

    if (!input || !clearButton || !panel || !status || !results) {
        return;
    }

    function updatePlaceholderVisibility() {
        if (!placeholder) {
            return;
        }

        placeholder.hidden = input.value.length > 0;
    }

    function updatePlaceholderOverflow() {
        if (!placeholder || !placeholderText) {
            return;
        }

        if (placeholder.hidden) {
            placeholder.classList.remove('is-overflowing');
            return;
        }

        // The track is max-content, so compare the first copy's natural width
        // to the visible field. Distance is now handled purely in CSS (-50%).
        var copyWidth = placeholderText.getBoundingClientRect().width;
        var fieldWidth = placeholder.getBoundingClientRect().width;
        var isOverflowing = copyWidth > fieldWidth - 8;

        placeholder.classList.toggle('is-overflowing', isOverflowing);
    }

    function setPanelVisible(visible) {
        panel.hidden = !visible;
        input.setAttribute('aria-expanded', visible ? 'true' : 'false');
        shell.classList.toggle('is-open', visible);
    }

    function setStatus(text) {
        status.textContent = text;
    }

    function clearResults() {
        results.textContent = '';
        links = [];
        activeIndex = -1;
    }

    function updateClearButton() {
        clearButton.hidden = input.value.length === 0;
    }

    function updateSearchBarHint() {
        updateClearButton();
        updatePlaceholderVisibility();
        updatePlaceholderOverflow();
    }

    function renderLoading() {
        clearResults();
        var loading = document.createElement('div');
        loading.className = 'global-search-empty';
        loading.textContent = 'Loading results...';
        results.appendChild(loading);
    }

    function renderMessage(message) {
        clearResults();
        var empty = document.createElement('div');
        empty.className = 'global-search-empty';
        empty.textContent = message;
        results.appendChild(empty);
    }

    function entityLabel(entityType) {
        switch (entityType) {
            case 'Artist':
                return 'Artists';
            case 'Album':
                return 'Albums';
            case 'Track':
                return 'Tracks';
            case 'Playlist':
                return 'Playlists';
            case 'Page':
                return 'Pages';
            default:
                return entityType;
        }
    }

    function renderResults(items) {
        clearResults();

        if (!items || items.length === 0) {
            renderMessage('No matches found across artists, albums, tracks, playlists, or pages.');
            return;
        }

        var grouped = new Map();
        items.forEach(function (item) {
            if (!grouped.has(item.entityType)) {
                grouped.set(item.entityType, []);
            }

            grouped.get(item.entityType).push(item);
        });

        grouped.forEach(function (groupItems, entityType) {
            var isPage = entityType === 'Page';
            var section = document.createElement('section');
            section.className = isPage
                ? 'global-search-group global-search-group--pages'
                : 'global-search-group';

            var heading = document.createElement('h3');
            heading.className = 'global-search-group-title';
            heading.textContent = (isPage ? '» ' : '') + entityLabel(entityType);
            section.appendChild(heading);

            var list = document.createElement('div');
            list.className = 'global-search-list';

            groupItems.forEach(function (item) {
                var link = document.createElement('a');
                link.className = 'global-search-result';
                link.href = item.url;
                link.setAttribute('role', 'option');
                link.setAttribute('aria-selected', 'false');

                var title = document.createElement('span');
                title.className = 'global-search-result-title';
                title.textContent = item.title;

                var subtitle = document.createElement('span');
                subtitle.className = 'global-search-result-subtitle';
                subtitle.textContent = item.subtitle || entityType;

                var badge = document.createElement('span');
                badge.className = 'global-search-result-badge';
                badge.textContent = entityType;

                var copy = document.createElement('span');
                copy.className = 'global-search-result-copy';
                copy.appendChild(title);
                copy.appendChild(subtitle);

                link.appendChild(copy);
                link.appendChild(badge);

                list.appendChild(link);
                links.push(link);
            });

            section.appendChild(list);
            results.appendChild(section);
        });

        activeIndex = links.length > 0 ? 0 : -1;
        updateActiveLink();
    }

    function updateActiveLink() {
        links.forEach(function (link, index) {
            var active = index === activeIndex;
            link.classList.toggle('is-active', active);
            link.setAttribute('aria-selected', active ? 'true' : 'false');

            if (active) {
                link.scrollIntoView({ block: 'nearest' });
            }
        });
    }

    function moveActive(delta) {
        if (links.length === 0) {
            return;
        }

        activeIndex = (activeIndex + delta + links.length) % links.length;
        updateActiveLink();
    }

    function abortPendingRequest() {
        if (controller) {
            controller.abort();
            controller = null;
        }
    }

    function fetchResults(query) {
        abortPendingRequest();

        if (query.length < minLength) {
            setStatus('Type 2+ characters to search.');
            renderMessage('Search library from the header.');
            setPanelVisible(true);
            return;
        }

        controller = new AbortController();
        setStatus('Searching...');
        renderLoading();
        setPanelVisible(true);

        fetch(endpoint + '?q=' + encodeURIComponent(query), {
            headers: { Accept: 'application/json' },
            signal: controller.signal
        })
            .then(function (response) {
                if (!response.ok) {
                    throw new Error('Search request failed');
                }

                return response.json();
            })
            .then(function (data) {
                setStatus(data.length === 0 ? 'No matches found.' : '');
                renderResults(data);
                setPanelVisible(true);
            })
            .catch(function (error) {
                if (error && error.name === 'AbortError') {
                    return;
                }

                setStatus('Search unavailable right now.');
                renderMessage('Search unavailable right now.');
                setPanelVisible(true);
            });
    }

    function scheduleSearch() {
        clearTimeout(debounceTimer);
        updateSearchBarHint();

        debounceTimer = window.setTimeout(function () {
            fetchResults(input.value.trim());
        }, 250);
    }

    input.addEventListener('focus', function () {
        if (input.value.trim().length < minLength) {
            setStatus('Type 2+ characters to search.');
            renderMessage('Search library from the header.');
            setPanelVisible(true);
        }
    });

    input.addEventListener('input', scheduleSearch);

    input.addEventListener('keydown', function (event) {
        if (event.key === 'ArrowDown') {
            event.preventDefault();
            moveActive(1);
            return;
        }

        if (event.key === 'ArrowUp') {
            event.preventDefault();
            moveActive(-1);
            return;
        }

        if (event.key === 'Enter' && activeIndex >= 0 && links[activeIndex]) {
            event.preventDefault();
            links[activeIndex].click();
            return;
        }

        if (event.key === 'Escape') {
            abortPendingRequest();
            setPanelVisible(false);
            input.blur();
        }
    });

    clearButton.addEventListener('click', function () {
        abortPendingRequest();
        input.value = '';
        updateSearchBarHint();
        setStatus('Type 2+ characters to search.');
        renderMessage('Search library from the header.');
        setPanelVisible(true);
        input.focus();
    });

    document.addEventListener('click', function (event) {
        if (!shell.contains(event.target)) {
            setPanelVisible(false);
        }
    });

    if ('ResizeObserver' in window && placeholder) {
        resizeObserver = new ResizeObserver(updatePlaceholderOverflow);
        resizeObserver.observe(placeholder);
    } else {
        window.addEventListener('resize', updatePlaceholderOverflow);
    }

    if (document.fonts && document.fonts.ready) {
        document.fonts.ready.then(updatePlaceholderOverflow);
    }

    window.requestAnimationFrame(function () {
        updateSearchBarHint();
    });

    updateSearchBarHint();
});