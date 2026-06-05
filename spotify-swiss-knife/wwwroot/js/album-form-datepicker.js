(function () {
    var locale = navigator.language || 'en';
    var localeFirstDay = 0;
    try {
        var loc = new Intl.Locale(locale);
        var weekInfo = loc.weekInfo || (typeof loc.getWeekInfo === 'function' && loc.getWeekInfo());
        if (weekInfo && typeof weekInfo.firstDay === 'number') {
            localeFirstDay = weekInfo.firstDay % 7;
        }
    } catch (e) {}

    var triggerFmt = new Intl.DateTimeFormat(locale, { day: '2-digit', month: 'short', year: 'numeric' });
    var headerFmt  = new Intl.DateTimeFormat(locale, { month: 'long', year: 'numeric' });
    var ariaFmt    = new Intl.DateTimeFormat(locale, { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' });
    var weekdayFmt = new Intl.DateTimeFormat(locale, { weekday: 'short' });

    function renderWeekdayHeaders(dpcEl) {
        var weekdays = dpcEl.querySelector('.dpc__weekdays');
        if (!weekdays) return;
        weekdays.innerHTML = '';
        for (var i = 0; i < 7; i++) {
            var dayIndex = (localeFirstDay + i) % 7;
            var refDate = new Date(1970, 0, 4 + dayIndex);
            var span = document.createElement('span');
            span.textContent = weekdayFmt.format(refDate);
            weekdays.appendChild(span);
        }
    }

    function renderYearGrid(dpcEl, state) {
        var grid = dpcEl.querySelector('.dpc__year-grid');
        if (!grid) return;
        grid.innerHTML = '';
        var base = state.yearPageBase;
        for (var y = base; y < base + 16; y++) {
            var btn = document.createElement('button');
            btn.type = 'button';
            btn.className = 'dpc__year';
            btn.textContent = y;
            btn.setAttribute('data-year', y);
            if (y === state.year) btn.classList.add('dpc__year--selected');
            (function(yr) {
                btn.addEventListener('click', function(e) {
                    e.stopPropagation();
                    state.year = yr;
                    closeYearPicker(dpcEl, state);
                });
            })(y);
            grid.appendChild(btn);
        }
    }

    function openYearPicker(dpcEl, state) {
        state.yearPageBase = state.year - (state.year % 16);
        state.yearMode = true;
        var weekdays = dpcEl.querySelector('.dpc__weekdays');
        var dayGrid = dpcEl.querySelector('.dpc__grid');
        var yearGrid = dpcEl.querySelector('.dpc__year-grid');
        var headBtn = dpcEl.querySelector('.dpc__head-btn');
        if (weekdays) weekdays.hidden = true;
        if (dayGrid) dayGrid.hidden = true;
        if (yearGrid) yearGrid.hidden = false;
        if (headBtn) headBtn.textContent = state.yearPageBase + '–' + (state.yearPageBase + 15);
        renderYearGrid(dpcEl, state);
    }

    function closeYearPicker(dpcEl, state) {
        state.yearMode = false;
        var weekdays = dpcEl.querySelector('.dpc__weekdays');
        var dayGrid = dpcEl.querySelector('.dpc__grid');
        var yearGrid = dpcEl.querySelector('.dpc__year-grid');
        if (weekdays) weekdays.hidden = false;
        if (dayGrid) dayGrid.hidden = false;
        if (yearGrid) yearGrid.hidden = true;
        renderCalendar(dpcEl, state);
    }

    function renderCalendar(dpcEl, state, onSelect) {
        var targetId = dpcEl.dataset.target;
        var hidden = document.getElementById(targetId);
        var selectedVal = hidden ? hidden.value : '';

        var headBtn = dpcEl.querySelector('.dpc__head-btn');
        if (headBtn) {
            headBtn.textContent = headerFmt.format(new Date(state.year, state.month, 1));
        }

        var grid = dpcEl.querySelector('.dpc__grid');
        if (!grid) return;
        grid.innerHTML = '';

        var daysInMonth = new Date(state.year, state.month + 1, 0).getDate();
        var rawFirstDay = new Date(state.year, state.month, 1).getDay();
        var firstDayOffset = (rawFirstDay - localeFirstDay + 7) % 7;

        for (var i = 0; i < firstDayOffset; i++) {
            var empty = document.createElement('span');
            empty.className = 'dpc__day dpc__day--empty';
            empty.setAttribute('aria-hidden', 'true');
            grid.appendChild(empty);
        }

        for (var day = 1; day <= daysInMonth; day++) {
            var mm2 = String(state.month + 1).padStart(2, '0');
            var dd = String(day).padStart(2, '0');
            var val = state.year + '-' + mm2 + '-' + dd;

            var btn = document.createElement('button');
            btn.type = 'button';
            btn.className = 'dpc__day';
            btn.textContent = day;
            btn.setAttribute('data-date', val);
            btn.setAttribute('aria-label', ariaFmt.format(new Date(state.year, state.month, day)));

            if (val === selectedVal) btn.classList.add('dpc__day--selected');

            (function(v, el, st) {
                btn.addEventListener('click', function(e) {
                    e.stopPropagation();
                    if (hidden) hidden.value = v;
                    renderCalendar(el, st, onSelect);
                    var wrapper = el.closest('.dpc-wrapper');
                    if (wrapper) {
                        var panel = wrapper.querySelector('.dpc-panel');
                        var trig = wrapper.querySelector('.dpc-trigger');
                        if (panel) panel.hidden = true;
                        if (trig) trig.setAttribute('aria-expanded', 'false');
                        updateTriggerLabel(trig, v);
                    }
                    if (typeof onSelect === 'function') onSelect(v);
                });
            })(val, dpcEl, state);

            grid.appendChild(btn);
        }
    }

    function updateTriggerLabel(trigEl, val) {
        if (!trigEl) return;
        var hasPrefix = trigEl.id === 'triggerFrom' || trigEl.id === 'triggerTo';
        var prefix = hasPrefix ? (trigEl.id === 'triggerFrom' ? 'From' : 'To') : null;
        if (val) {
            var parts = val.split('-');
            var d = new Date(parseInt(parts[0], 10), parseInt(parts[1], 10) - 1, parseInt(parts[2], 10));
            trigEl.textContent = prefix ? prefix + ': ' + triggerFmt.format(d) : triggerFmt.format(d);
        } else {
            trigEl.textContent = prefix ? prefix + ': —' : '—';
        }
    }

    function buildCalendar(dpcEl, onSelect) {
        var targetId = dpcEl.dataset.target;
        var hidden = document.getElementById(targetId);

        var today = new Date();
        var year = today.getFullYear();
        var month = today.getMonth();

        if (hidden && hidden.value) {
            var parts = hidden.value.split('-');
            if (parts.length === 3) {
                year = parseInt(parts[0], 10);
                month = parseInt(parts[1], 10) - 1;
            }
        }

        var state = { year: year, month: month, yearMode: false, yearPageBase: year - (year % 16) };
        dpcEl._state = state;

        renderWeekdayHeaders(dpcEl);
        renderCalendar(dpcEl, state, onSelect);

        var headBtn = dpcEl.querySelector('.dpc__head-btn');
        if (headBtn) {
            headBtn.addEventListener('click', function(e) {
                e.stopPropagation();
                if (state.yearMode) {
                    closeYearPicker(dpcEl, state);
                } else {
                    openYearPicker(dpcEl, state);
                }
            });
        }

        dpcEl.querySelectorAll('.dpc__nav').forEach(function(navBtn) {
            navBtn.addEventListener('click', function(e) {
                e.stopPropagation();
                var dir = parseInt(navBtn.dataset.dir, 10);
                if (state.yearMode) {
                    state.yearPageBase += dir * 16;
                    var headBtnEl = dpcEl.querySelector('.dpc__head-btn');
                    if (headBtnEl) headBtnEl.textContent = state.yearPageBase + '–' + (state.yearPageBase + 15);
                    renderYearGrid(dpcEl, state);
                } else {
                    state.month += dir;
                    if (state.month > 11) { state.month = 0; state.year++; }
                    if (state.month < 0) { state.month = 11; state.year--; }
                    renderCalendar(dpcEl, state, onSelect);
                }
            });
        });

        var wrapper = dpcEl.closest('.dpc-wrapper');
        if (wrapper && hidden && hidden.value) {
            var trig = wrapper.querySelector('.dpc-trigger');
            updateTriggerLabel(trig, hidden.value);
        }
    }

    function initDpcWrappers(onSelect) {
        document.querySelectorAll('.dpc-wrapper').forEach(function(wrapper) {
            var trig = wrapper.querySelector('.dpc-trigger');
            var panel = wrapper.querySelector('.dpc-panel');
            if (!trig || !panel) return;

            trig.addEventListener('click', function(e) {
                e.stopPropagation();
                var isOpen = !panel.hidden;
                document.querySelectorAll('.dpc-panel').forEach(function(p) { p.hidden = true; });
                document.querySelectorAll('.dpc-trigger').forEach(function(t) { t.setAttribute('aria-expanded', 'false'); });
                if (!isOpen) {
                    panel.hidden = false;
                    trig.setAttribute('aria-expanded', 'true');
                    var dpcEl = panel.querySelector('.dpc');
                    if (dpcEl && dpcEl._state) renderCalendar(dpcEl, dpcEl._state, onSelect);
                }
            });
        });

        document.addEventListener('click', function(e) {
            var openPanel = document.querySelector('.dpc-panel:not([hidden])');
            if (!openPanel) return;
            var wrapper = openPanel.closest('.dpc-wrapper');
            if (wrapper && !wrapper.contains(e.target)) {
                openPanel.hidden = true;
                var trig = wrapper.querySelector('.dpc-trigger');
                if (trig) trig.setAttribute('aria-expanded', 'false');
            }
        });

        document.addEventListener('keydown', function(e) {
            if (e.key === 'Escape') {
                var openPanel = document.querySelector('.dpc-panel:not([hidden])');
                if (openPanel) {
                    var wrapper = openPanel.closest('.dpc-wrapper');
                    openPanel.hidden = true;
                    if (wrapper) {
                        var trig = wrapper.querySelector('.dpc-trigger');
                        if (trig) { trig.setAttribute('aria-expanded', 'false'); trig.focus(); }
                    }
                }
            }
        });
    }

    window.Dpc = {
        buildCalendar: buildCalendar,
        renderCalendar: renderCalendar,
        updateTriggerLabel: updateTriggerLabel,
        initDpcWrappers: initDpcWrappers,
        triggerFmt: triggerFmt,
        selfInit: false
    };

    document.addEventListener('DOMContentLoaded', function() {
        if (window.Dpc.selfInit) return;
        initDpcWrappers(null);
        document.querySelectorAll('.dpc').forEach(function(el) { buildCalendar(el, null); });
    });
})();
