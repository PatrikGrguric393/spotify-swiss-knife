// Behaviour shared by the schedule Create and Edit views (Views/Schedules/_ScheduleFormFields.cshtml).
//
// Depends on bulk-grid-utils.js (window.BulkGrid) for the playlist picker, so it must be
// loaded after it. Contains four self-contained pieces:
//   1. Multi-select playlist picker
//   2. Time wheel (local time -> UTC hidden input + offset)
//   3. Frequency mode switcher (shows/hides the day fields)
//   4. Day-of-month wheel
//
// UTC SEED (Edit only): the create flow treats the rendered time/day values as LOCAL.
// On the Edit GET the controller seeds those fields with the schedule's stored UTC values
// and marks the form with data-utc-seed="true". When that flag is present we convert
// UTC -> local exactly once, BEFORE the four pieces below read the DOM, by rewriting the
// time wheel's data-hour/data-minute, the day-of-week checkbox checked states, and the day
// wheel's data-day / hidden input. Create renders data-utc-seed="false" (or omits it) and an
// Edit POST re-render also renders "false" (the fields already hold the user's local
// selection), so in both of those cases this prelude is a no-op and behaviour is unchanged.
(function () {
    // --- UTC -> local seed prelude (runs first, only when flagged) -----------------
    (function () {
        var form = document.querySelector('form.shuffle-form[data-utc-seed="true"]');
        if (!form) { return; }

        // Minutes to add to local time to reach UTC (same convention the server uses).
        var offset = new Date().getTimezoneOffset();

        // --- Time: shift UTC clock minutes back into local clock minutes. ---
        var timeWheel = document.getElementById('timeWheel');
        var dayShift = 0;
        if (timeWheel) {
            var utcHour = parseInt(timeWheel.getAttribute('data-hour'), 10);
            var utcMinute = parseInt(timeWheel.getAttribute('data-minute'), 10);
            if (!isFinite(utcHour)) { utcHour = 0; }
            if (!isFinite(utcMinute)) { utcMinute = 0; }

            var utcMin = utcHour * 60 + utcMinute;
            var raw = utcMin - offset;                 // local minutes, may be <0 or >=1440
            var localMin = ((raw % 1440) + 1440) % 1440; // local clock minutes
            var localHour = Math.floor(localMin / 60);
            var localMinute = localMin % 60;
            dayShift = Math.floor(raw / 1440);         // -1, 0, or +1

            timeWheel.setAttribute('data-hour', String(localHour));
            timeWheel.setAttribute('data-minute', String(localMinute));
        }

        // --- Day of week: shift each checked UTC day by the same dayShift. ---
        // Preserve the SET of days; only translate each across any midnight crossing.
        var days = document.getElementById('scheduleDays');
        if (days) {
            var boxes = Array.prototype.slice.call(
                days.querySelectorAll('input[type="checkbox"][name="DaysOfWeek"]'));
            var checkedUtc = boxes
                .filter(function (b) { return b.checked; })
                .map(function (b) { return parseInt(b.value, 10); });
            var localDays = {};
            checkedUtc.forEach(function (d) {
                localDays[(((d + dayShift) % 7) + 7) % 7] = true;
            });
            boxes.forEach(function (b) {
                b.checked = !!localDays[parseInt(b.value, 10)];
            });
        }

        // --- Day of month: shift by the same dayShift, wrapping within 1..31. ---
        var dayWheel = document.getElementById('dayWheel');
        var domHidden = document.getElementById('DayOfMonth');
        if (dayWheel) {
            var utcDom = parseInt(dayWheel.getAttribute('data-day'), 10);
            if (isFinite(utcDom)) {
                var localDom = (((utcDom - 1 + dayShift) % 31) + 31) % 31 + 1;
                dayWheel.setAttribute('data-day', String(localDom));
                if (domHidden) { domHidden.value = String(localDom); }
            }
        }
        // Frequency is not timezone-dependent and is left untouched.
    }());

    // --- 1. Multi-select playlist picker ------------------------------------------
    // A schedule can target any number of playlists. Clicking a row toggles its
    // selection (highlighted via .is-selected); clicking a selected row deselects it.
    // Selection lives in a Set keyed by playlist id, so it survives search/filtering
    // even when a selected row isn't currently rendered. On every change we rebuild
    // the hidden inputs in #selectedPlaylistInputs as index-aligned repeated
    // PlaylistIds/PlaylistNames inputs (the POST source of truth). When empty, no
    // inputs are posted so server-side validation fails.
    (function () {
        var inputsHost = document.getElementById('selectedPlaylistInputs');
        var search = document.getElementById('schedulePlaylistSearch');
        var searchClear = document.getElementById('schedulePlaylistSearchClear');
        var searchStatus = document.getElementById('schedulePlaylistSearchStatus');
        var tbody = document.getElementById('schedulePlaylistTableBody');
        var empty = document.getElementById('schedulePlaylistEmpty');
        var count = document.getElementById('schedulePlaylistCount');
        if (!tbody || !inputsHost) { return; }

        var escapeHtml = window.BulkGrid.escapeHtml;
        var coverCell = window.BulkGrid.coverCell;

        var allPlaylists = [];
        var dataEl = document.getElementById('schedulePlaylistData');
        if (dataEl) {
            try { allPlaylists = JSON.parse(dataEl.textContent) || []; } catch (e) { allPlaylists = []; }
        }

        var nameById = {};
        for (var n = 0; n < allPlaylists.length; n++) {
            nameById[allPlaylists[n].id] = allPlaylists[n].name || '';
        }

        // Seed selection from the model (re-render after a validation error, or Edit GET).
        var selected = new Set();
        var seedEl = document.getElementById('scheduleSelectedPlaylistIds');
        if (seedEl) {
            try {
                (JSON.parse(seedEl.textContent) || []).forEach(function (id) {
                    if (id) { selected.add(id); }
                });
            } catch (e) { /* leave empty */ }
        }

        function rowHtml(p, isSelected) {
            var id = escapeHtml(p.id);
            return '<tr class="' + (isSelected ? 'is-selected' : '') + '" data-id="' + id + '"' +
                ' aria-selected="' + (isSelected ? 'true' : 'false') + '">' +
                '<td data-label="Cover">' + coverCell(p.imageUrl) + '</td>' +
                '<td data-label="Name"><span class="bulk-save-name">' + escapeHtml(p.name) + '</span></td>' +
                '<td data-label="Tracks"><span class="bulk-save-muted">' + escapeHtml(p.tracks) + '</span></td>' +
                '</tr>';
        }

        function render(list) {
            var html = '';
            for (var i = 0; i < list.length; i++) {
                html += rowHtml(list[i], selected.has(list[i].id));
            }
            tbody.innerHTML = html;
        }

        function updateEmpty(rowCount, query) {
            if (rowCount > 0) { empty.hidden = true; return; }
            empty.hidden = false;
            empty.textContent = query ? 'No playlists match your search.' : 'No playlists found.';
        }

        function filter(query) {
            var q = query.trim().toLowerCase();
            return allPlaylists.filter(function (p) {
                return [p.name || '', String(p.tracks)].join('  ').toLowerCase().indexOf(q) !== -1;
            });
        }

        function renderView() {
            var query = search.value.trim();
            searchClear.hidden = search.value === '';
            if (query === '') {
                render(allPlaylists);
                updateEmpty(allPlaylists.length, '');
                searchStatus.textContent = '';
            } else {
                var matches = filter(query);
                render(matches);
                updateEmpty(matches.length, query);
                searchStatus.textContent = matches.length + (matches.length === 1 ? ' match' : ' matches');
            }
        }

        function updateCount() {
            var c = selected.size;
            count.textContent = c === 0
                ? 'No playlists selected'
                : (c + (c === 1 ? ' playlist selected' : ' playlists selected'));
        }

        // Rebuild repeated hidden inputs. PlaylistIds[i] aligns with PlaylistNames[i]
        // because we append both for each id in the same loop / document order.
        function syncInputs() {
            var html = '';
            selected.forEach(function (id) {
                var name = nameById[id] || '';
                html += '<input type="hidden" name="PlaylistIds" value="' + escapeHtml(id) + '">' +
                        '<input type="hidden" name="PlaylistNames" value="' + escapeHtml(name) + '">';
            });
            inputsHost.innerHTML = html;
        }

        tbody.addEventListener('click', function (e) {
            if (e.target.tagName === 'A') { return; }
            var row = e.target.closest('tr[data-id]');
            if (!row) { return; }
            var id = row.dataset.id;
            if (selected.has(id)) {
                selected.delete(id);
                row.classList.remove('is-selected');
                row.setAttribute('aria-selected', 'false');
            } else {
                selected.add(id);
                row.classList.add('is-selected');
                row.setAttribute('aria-selected', 'true');
            }
            syncInputs();
            updateCount();
        });

        searchClear.addEventListener('click', function () {
            search.value = '';
            searchClear.hidden = true;
            search.focus();
            renderView();
        });
        search.addEventListener('input', renderView);

        renderView();
        syncInputs();
        updateCount();
    }());

    // --- 2. Time wheel -------------------------------------------------------------
    (function () {
        var root = document.getElementById('timeWheel');
        var hidden = document.getElementById('TimeUtc');
        if (!root || !hidden) { return; }

        var trigger   = document.getElementById('timeWheelTrigger');
        var valueEl   = document.getElementById('timeWheelValue');
        var panel     = document.getElementById('timeWheelPanel');
        var doneBtn   = document.getElementById('timeWheelDone');
        var offsetEl  = document.getElementById('TimezoneOffsetMinutes');
        var zoneEl    = document.getElementById('scheduleTimeZone');
        var utcHintEl = document.getElementById('scheduleTimeUtcHint');
        var cols      = Array.prototype.slice.call(root.querySelectorAll('.timewheel-col'));

        var backdrop = document.createElement('div');
        backdrop.className = 'timewheel-backdrop';
        backdrop.hidden = true;
        document.body.appendChild(backdrop);
        backdrop.addEventListener('pointerdown', function () { close(false); });

        // For the mobile bottom-sheet the panel must live in <body>, not inside
        // .app-scroll-area (z-index:1 stacking context), or the body-level backdrop
        // (z-index:1049) paints on top of it regardless of the panel's own z-index.
        var panelOriginalParent = panel.parentNode;
        var panelNextSibling    = panel.nextSibling;

        // Single source of truth for the bottom-sheet breakpoint: resolve the
        // exact same query the CSS @media (max-width: 480px) uses. A window.innerWidth
        // check can disagree with the CSS layout viewport at the edge (scrollbar
        // width, zoom, device-pixel-ratio rounding); when CSS rendered the sheet
        // but JS skipped the reparent, the panel stayed trapped in .app-scroll-area
        // and the backdrop painted over it (shadowed, unclickable).
        var sheetQuery = window.matchMedia('(max-width: 480px)');
        function isSheet() { return sheetQuery.matches; }

        var state = {
            hour:   clampInt(root.getAttribute('data-hour'), 23),
            minute: clampInt(root.getAttribute('data-minute'), 59)
        };

        function clampInt(v, max) {
            var n = parseInt(v, 10);
            if (isNaN(n) || n < 0) { n = 0; }
            if (n > max) { n = max; }
            return n;
        }
        function pad2(n) {
            var i = Math.trunc(Number(n));
            if (!isFinite(i) || i < 0) { i = 0; }
            return ('0' + i).slice(-2);
        }

        // Same value the server reads: minutes to add to local time to reach UTC.
        function offsetMinutes() { return new Date().getTimezoneOffset(); }
        function setOffset() { if (offsetEl) { offsetEl.value = String(offsetMinutes()); } }

        function setZone() {
            if (!zoneEl) { return; }
            var zone = '';
            try { zone = Intl.DateTimeFormat().resolvedOptions().timeZone || ''; } catch (e) {}
            if (zone) { zoneEl.textContent = 'Detected timezone: ' + zone; zoneEl.hidden = false; }
        }
        function updateUtcHint() {
            if (!utcHintEl) { return; }
            var totalLocal = state.hour * 60 + state.minute;
            var totalUtc = ((totalLocal + offsetMinutes()) % 1440 + 1440) % 1440;
            utcHintEl.textContent = '= ' + pad2(Math.floor(totalUtc / 60)) + ':' + pad2(totalUtc % 60) + ' UTC';
            utcHintEl.hidden = false;
        }
        function sync() {
            hidden.value = pad2(state.hour) + ':' + pad2(state.minute);
            valueEl.textContent = pad2(state.hour) + ':' + pad2(state.minute);
            setOffset();
            updateUtcHint();
        }

        function rowHeight(col) {
            var opt = col.querySelector('.timewheel-opt');
            var h = opt ? opt.getBoundingClientRect().height : 0;
            return (h > 0) ? h : 44;
        }

        function buildColumn(col) {
            var unit = col.getAttribute('data-unit');
            var max  = parseInt(col.getAttribute('data-max'), 10);
            var track = col.querySelector('[data-track]');
            for (var i = 0; i <= max; i++) {
                var opt = document.createElement('div');
                opt.className = 'timewheel-opt';
                opt.setAttribute('role', 'option');
                opt.id = 'tw-' + unit + '-' + pad2(i);
                opt.setAttribute('aria-selected', 'false');
                opt.setAttribute('data-index', String(i));
                opt.textContent = pad2(i);
                track.appendChild(opt);
            }
            col._unit = unit;
            col._max = max;
            col._raf = null;
            col._snapTimer = null;

            track.addEventListener('click', function (e) {
                var opt = e.target.closest('.timewheel-opt');
                if (opt) { scrollToIndex(col, parseInt(opt.getAttribute('data-index'), 10), true); }
            });

            col.addEventListener('scroll', function () {
                if (col._raf) { cancelAnimationFrame(col._raf); }
                col._raf = requestAnimationFrame(function () { markCentered(col); });
                if (col._snapTimer) { clearTimeout(col._snapTimer); }
                col._snapTimer = setTimeout(function () { commitCentered(col); }, 90);
            }, { passive: true });

            col.addEventListener('keydown', function (e) { onColKey(col, e); });
        }

        function indexFromScroll(col) {
            var rh = rowHeight(col);
            var idx = Math.round(col.scrollTop / rh);
            if (!isFinite(idx)) { idx = 0; }
            return Math.max(0, Math.min(col._max, idx));
        }

        function markCentered(col) {
            var idx = indexFromScroll(col);
            var opts = col.querySelectorAll('.timewheel-opt');
            for (var i = 0; i < opts.length; i++) {
                opts[i].setAttribute('aria-selected', i === idx ? 'true' : 'false');
            }
            col.setAttribute('aria-activedescendant', 'tw-' + col._unit + '-' + pad2(idx));
        }

        function commitCentered(col) {
            var idx = indexFromScroll(col);
            if (col._unit === 'hour') { state.hour = idx; } else { state.minute = idx; }
            markCentered(col);
            sync();
        }

        function scrollToIndex(col, idx, smooth) {
            var rh = rowHeight(col);
            col.scrollTo({ top: idx * rh, behavior: smooth ? 'smooth' : 'auto' });
            if (col._unit === 'hour') { state.hour = idx; } else { state.minute = idx; }
            markCentered(col);
            sync();
        }

        function onColKey(col, e) {
            var cur = (col._unit === 'hour') ? state.hour : state.minute;
            var handled = true;
            switch (e.key) {
                case 'ArrowDown': cur = Math.min(col._max, cur + 1); break;
                case 'ArrowUp':   cur = Math.max(0, cur - 1); break;
                case 'PageDown':  cur = Math.min(col._max, cur + 5); break;
                case 'PageUp':    cur = Math.max(0, cur - 5); break;
                case 'Home':      cur = 0; break;
                case 'End':       cur = col._max; break;
                case 'Enter':
                case 'Escape':    close(true); return;
                default:
                    if (/^[0-9]$/.test(e.key)) { cur = typeBuffer(col, e.key); break; }
                    handled = false;
            }
            if (handled) { e.preventDefault(); scrollToIndex(col, cur, true); }
        }

        function typeBuffer(col, digit) {
            var now = Date.now();
            if (!col._typeAt || now - col._typeAt > 800) { col._typeStr = ''; }
            col._typeAt = now;
            col._typeStr = (col._typeStr + digit).slice(-2);
            var n = parseInt(col._typeStr, 10);
            if (n > col._max) { n = parseInt(digit, 10); col._typeStr = digit; }
            return n;
        }

        function reposition() {
            if (isSheet()) { return; }   // mobile: CSS bottom-sheet handles it
            var rect = panel.getBoundingClientRect();
            var vph = (window.visualViewport ? window.visualViewport.height : window.innerHeight);
            if (rect.bottom > vph) {
                panel.style.top = 'auto';
                panel.style.bottom = 'calc(100% + 4px)';
            } else {
                panel.style.top = '';
                panel.style.bottom = '';
            }
        }

        function open() {
            // Escape the panel out of .app-scroll-area to <body>, appending the
            // backdrop first and the panel last so the panel wins on document
            // order as well as z-index. Driven by the same query as the CSS sheet.
            if (isSheet()) {
                document.body.appendChild(backdrop);
                document.body.appendChild(panel);
            }
            panel.hidden = false;
            backdrop.hidden = false;
            trigger.setAttribute('aria-expanded', 'true');
            reposition();
            requestAnimationFrame(function () {
                cols.forEach(function (col) {
                    var idx = (col._unit === 'hour') ? state.hour : state.minute;
                    scrollToIndex(col, idx, false);
                });
            });
            window.addEventListener('resize', reposition);
            if (window.visualViewport) { window.visualViewport.addEventListener('resize', reposition); }
            document.addEventListener('pointerdown', onOutside, true);
            document.addEventListener('keydown', onDocKey, true);
            if (cols[0]) { cols[0].focus(); }
        }

        function close(focusTrigger) {
            if (panel.hidden) { return; }
            panel.hidden = true;
            if (panel.parentNode === document.body) {
                panelOriginalParent.insertBefore(panel, panelNextSibling);
            }
            backdrop.hidden = true;
            trigger.setAttribute('aria-expanded', 'false');
            panel.style.top = '';
            panel.style.bottom = '';
            window.removeEventListener('resize', reposition);
            if (window.visualViewport) { window.visualViewport.removeEventListener('resize', reposition); }
            document.removeEventListener('pointerdown', onOutside, true);
            document.removeEventListener('keydown', onDocKey, true);
            if (focusTrigger) { trigger.focus(); }
        }

        function onOutside(e) { if (!root.contains(e.target) && !panel.contains(e.target)) { close(false); } }
        function onDocKey(e) { if (e.key === 'Escape') { e.preventDefault(); close(true); } }

        trigger.addEventListener('click', function () {
            if (panel.hidden) { open(); } else { close(true); }
        });
        doneBtn.addEventListener('click', function () { close(true); });

        var form = trigger.closest('form');
        if (form) { form.addEventListener('submit', sync); }

        cols.forEach(buildColumn);
        setZone();
        sync();
    }());

    // --- 3. Frequency mode switcher ------------------------------------------------
    (function () {
        var group    = document.getElementById('freqGroup');
        var dowField = document.getElementById('dayOfWeekField');
        var domField = document.getElementById('dayOfMonthField');
        var days     = document.getElementById('scheduleDays');
        var dowHint  = document.getElementById('dowHint');
        if (!group || !dowField || !domField || !days) { return; }

        var radios = Array.prototype.slice.call(
            group.querySelectorAll('input[type="radio"][name="Frequency"]'));
        var boxes = Array.prototype.slice.call(
            days.querySelectorAll('input[type="checkbox"][name="DaysOfWeek"]'));

        function currentFreq() {
            for (var i = 0; i < radios.length; i++) {
                if (radios[i].checked) { return radios[i].value; }
            }
            return 'Weekly';
        }

        function mirrorAria() {
            boxes.forEach(function (b) {
                b.setAttribute('aria-checked', b.checked ? 'true' : 'false');
            });
        }

        function enforceSingle(changed) {
            if (changed && changed.checked) {
                boxes.forEach(function (b) { if (b !== changed) { b.checked = false; } });
            }
            mirrorAria();
        }

        function setMode(freq) {
            var showDom = (freq === 'Monthly');
            var single  = (freq === 'Weekly');

            dowField.hidden = showDom;
            if (freq === 'Daily') {
                dowField.disabled = true;
                dowField.classList.add('is-disabled');
            } else {
                dowField.disabled = false;
                dowField.classList.remove('is-disabled');
            }

            days.classList.toggle('is-single', single);
            days.setAttribute('aria-label', single
                ? 'Day of week (select one)'
                : 'Days of week (select any)');
            boxes.forEach(function (b) {
                b.setAttribute('role', single ? 'radio' : 'checkbox');
            });

            if (dowHint) {
                dowHint.textContent =
                    freq === 'Daily'        ? 'Shuffles every day. No day selection needed.' :
                    freq === 'Weekly'       ? 'Pick one day each week.' :
                    freq === 'CustomWeekly' ? 'Pick any number of days.' : '';
            }

            if (single) {
                var firstChecked = boxes.filter(function (b) { return b.checked; })[0];
                boxes.forEach(function (b) { if (b !== firstChecked) { b.checked = false; } });
            }
            mirrorAria();

            domField.hidden = !showDom;
        }

        radios.forEach(function (r) {
            r.addEventListener('change', function () { setMode(currentFreq()); });
        });

        days.addEventListener('change', function (e) {
            var box = e.target.closest('input[type="checkbox"][name="DaysOfWeek"]');
            if (!box) { return; }
            if (currentFreq() === 'Weekly') { enforceSingle(box); }
            else { mirrorAria(); }
        });

        setMode(currentFreq());
    }());

    // --- 4. Day-of-month wheel -----------------------------------------------------
    (function () {
        var root = document.getElementById('dayWheel');
        var hidden = document.getElementById('DayOfMonth');
        if (!root || !hidden) { return; }

        var col = root.querySelector('.timewheel-col');
        var track = col.querySelector('[data-track]');
        var min = parseInt(col.getAttribute('data-min'), 10) || 1;
        var max = parseInt(col.getAttribute('data-max'), 10) || 31;

        function clamp(n) {
            if (isNaN(n) || n < min) { return min; }
            if (n > max) { return max; }
            return n;
        }
        var state = { day: clamp(parseInt(root.getAttribute('data-day'), 10)) };

        function rowHeight() {
            var opt = col.querySelector('.timewheel-opt');
            var h = opt ? opt.getBoundingClientRect().height : 0;
            return (h > 0) ? h : 44;
        }
        function idToVal(i) { return min + i; }
        function valToIndex(v) { return v - min; }

        for (var v = min; v <= max; v++) {
            var opt = document.createElement('div');
            opt.className = 'timewheel-opt';
            opt.setAttribute('role', 'option');
            opt.id = 'dw-day-' + v;
            opt.setAttribute('aria-selected', 'false');
            opt.setAttribute('data-index', String(valToIndex(v)));
            opt.textContent = String(v);
            track.appendChild(opt);
        }

        function indexFromScroll() {
            var rh = rowHeight();
            var maxIdx = max - min;
            var idx = Math.round(col.scrollTop / rh);
            if (!isFinite(idx)) { idx = 0; }
            return Math.max(0, Math.min(maxIdx, idx));
        }
        function mark(idx) {
            var opts = col.querySelectorAll('.timewheel-opt');
            for (var i = 0; i < opts.length; i++) {
                opts[i].setAttribute('aria-selected', i === idx ? 'true' : 'false');
            }
            col.setAttribute('aria-activedescendant', 'dw-day-' + idToVal(idx));
        }
        function sync() { hidden.value = String(state.day); }

        function scrollToIndex(idx, smooth) {
            var rh = rowHeight();
            col.scrollTo({ top: idx * rh, behavior: smooth ? 'smooth' : 'auto' });
            state.day = idToVal(idx);
            mark(idx);
            sync();
        }

        var raf = null, snapTimer = null;
        col.addEventListener('scroll', function () {
            if (raf) { cancelAnimationFrame(raf); }
            raf = requestAnimationFrame(function () { mark(indexFromScroll()); });
            if (snapTimer) { clearTimeout(snapTimer); }
            snapTimer = setTimeout(function () {
                var idx = indexFromScroll();
                state.day = idToVal(idx);
                mark(idx);
                sync();
            }, 90);
        }, { passive: true });

        track.addEventListener('click', function (e) {
            var opt = e.target.closest('.timewheel-opt');
            if (opt) { scrollToIndex(parseInt(opt.getAttribute('data-index'), 10), true); }
        });

        col.addEventListener('keydown', function (e) {
            var cur = valToIndex(state.day);
            var maxIdx = max - min;
            var handled = true;
            switch (e.key) {
                case 'ArrowDown': cur = Math.min(maxIdx, cur + 1); break;
                case 'ArrowUp':   cur = Math.max(0, cur - 1); break;
                case 'PageDown':  cur = Math.min(maxIdx, cur + 5); break;
                case 'PageUp':    cur = Math.max(0, cur - 5); break;
                case 'Home':      cur = 0; break;
                case 'End':       cur = maxIdx; break;
                default: handled = false;
            }
            if (handled) { e.preventDefault(); scrollToIndex(cur, true); }
        });

        var domField = document.getElementById('dayOfMonthField');
        if (domField && 'MutationObserver' in window) {
            new MutationObserver(function () {
                if (!domField.hidden) { scrollToIndex(valToIndex(state.day), false); }
            }).observe(domField, { attributes: true, attributeFilter: ['hidden'] });
        }

        var form = root.closest('form');
        if (form) { form.addEventListener('submit', sync); }

        scrollToIndex(valToIndex(state.day), false);
        sync();
    }());
}());
