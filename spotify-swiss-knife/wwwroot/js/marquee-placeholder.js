document.addEventListener('DOMContentLoaded', function () {
    var ELIGIBLE_TYPES = {
        text: true,
        search: true,
        email: true,
        url: true,
        password: true,
        number: true,
        tel: true
    };

    function isEligible(input) {
        // Skip the bespoke global search (it owns its own overlay lifecycle).
        if (input.closest('[data-global-search]')) {
            return false;
        }

        if (input.hasAttribute('data-no-marquee')) {
            return false;
        }

        // Defensive against re-runs / nested wrapping.
        if (input.closest('.marquee-field')) {
            return false;
        }

        var type = (input.getAttribute('type') || 'text').toLowerCase();
        if (!ELIGIBLE_TYPES[type]) {
            return false;
        }

        var text = input.getAttribute('placeholder');
        return !!(text && text.trim().length > 0);
    }

    function buildInstance(input) {
        var text = input.getAttribute('placeholder');

        // Match the overlay's leading inset + edge fade to the input's own
        // left padding so the marquee text starts exactly where typed text does.
        var computed = window.getComputedStyle(input);
        var inset = computed.paddingLeft || '0.7rem';

        var field = document.createElement('div');
        field.className = 'marquee-field';
        input.parentNode.insertBefore(field, input);

        var placeholder = document.createElement('div');
        placeholder.className = 'marquee-placeholder';
        placeholder.setAttribute('aria-hidden', 'true');
        placeholder.style.setProperty('--mp-inset', inset);
        // Inherit the input's typography so the overlay aligns pixel-for-pixel.
        placeholder.style.fontSize = computed.fontSize;
        placeholder.style.fontFamily = computed.fontFamily;

        var track = document.createElement('div');
        track.className = 'marquee-placeholder-track';

        var copyA = document.createElement('span');
        copyA.className = 'marquee-placeholder-text';
        copyA.textContent = text;

        var copyB = document.createElement('span');
        copyB.className = 'marquee-placeholder-text';
        copyB.setAttribute('aria-hidden', 'true');
        copyB.textContent = text;

        track.appendChild(copyA);
        track.appendChild(copyB);
        placeholder.appendChild(track);

        field.appendChild(placeholder);
        field.appendChild(input);

        // Keep the placeholder semantic for assistive tech, but blank the native
        // one so the browser doesn't paint it behind our overlay.
        input.setAttribute('aria-placeholder', text);
        input.setAttribute('placeholder', '');

        return { input: input, placeholder: placeholder, firstCopy: copyA };
    }

    function wire(instance) {
        var input = instance.input;
        var placeholder = instance.placeholder;
        var firstCopy = instance.firstCopy;

        function updateVisibility() {
            placeholder.hidden = input.value.length > 0;
        }

        function updateOverflow() {
            if (placeholder.hidden) {
                placeholder.classList.remove('is-overflowing');
                return;
            }

            var copyWidth = firstCopy.getBoundingClientRect().width;
            var fieldWidth = placeholder.getBoundingClientRect().width;
            placeholder.classList.toggle('is-overflowing', copyWidth > fieldWidth - 8);
        }

        function refresh() {
            updateVisibility();
            updateOverflow();
        }

        input.addEventListener('input', refresh);
        // Hide while focused so the typing area is clean; restore on blur if empty.
        input.addEventListener('focus', function () {
            placeholder.hidden = true;
        });
        input.addEventListener('blur', refresh);

        if ('ResizeObserver' in window) {
            var ro = new ResizeObserver(updateOverflow);
            ro.observe(placeholder);
        } else {
            window.addEventListener('resize', updateOverflow);
        }

        if (document.fonts && document.fonts.ready) {
            document.fonts.ready.then(updateOverflow);
        }

        window.requestAnimationFrame(refresh);
        refresh();
    }

    var inputs = document.querySelectorAll('input');
    var i;
    for (i = 0; i < inputs.length; i++) {
        if (isEligible(inputs[i])) {
            wire(buildInstance(inputs[i]));
        }
    }
});
