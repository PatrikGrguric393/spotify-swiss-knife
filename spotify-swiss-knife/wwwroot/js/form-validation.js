(function () {
    window.FormValidation = window.FormValidation || {};

    function debounce(fn, delay) {
        var timer = null;
        return function () {
            var args = arguments;
            clearTimeout(timer);
            timer = setTimeout(function () { fn.apply(null, args); }, delay);
        };
    }

    function attachNameValidatorTo(input, options) {
        options = options || {};
        var url = options.validateUrl || input.getAttribute('data-validate-url');
        var errorEl = document.getElementById(input.getAttribute('aria-describedby')) || document.querySelector('#error-Name');
        var excludeSelector = input.getAttribute('data-exclude-id-selector') || options.excludeIdSelector;

        var doCheck = function () {
            var val = input.value.trim();
            if (!val) return;
            var q = encodeURIComponent(val);
            var finalUrl = url + '?q=' + q;
            if (excludeSelector) {
                var excl = document.querySelector(excludeSelector);
                if (excl && excl.value) finalUrl += '&excludeId=' + encodeURIComponent(excl.value);
            }

            fetch(finalUrl).then(function (r) { return r.json(); })
                .then(function (data) {
                    if (!data.isUnique) {
                        input.dataset.asyncError = '1';
                        if (errorEl) {
                            errorEl.classList.add('show');
                            errorEl.innerHTML = '<div class="validation-error-message">An artist with this name already exists.</div>';
                        }
                        input.classList.add('field-error');
                        input.classList.remove('field-success');
                    } else {
                        delete input.dataset.asyncError;
                        if (errorEl) { errorEl.classList.remove('show'); errorEl.innerHTML = ''; }
                        input.classList.remove('field-error');
                    }
                }).catch(function (e) { console.error('Name validation check failed:', e); });
        };

        var handler = debounce(doCheck, options.debounce || 150);
        input.addEventListener('blur', handler);
    }

    window.FormValidation.attachNameValidators = function (opts) {
        var inputs = document.querySelectorAll('[data-validate-name="true"]');
        Array.prototype.forEach.call(inputs, function (input) {
            attachNameValidatorTo(input, opts || {});
        });
    };

    function attachSpotifyValidatorTo(input, options) {
        options = options || {};
        var errorEl = document.getElementById(input.getAttribute('aria-describedby')) || document.querySelector('#error-SpotifyUrl');

        var doCheck = function () {
            var val = input.value.trim();
            if (!val) {
                delete input.dataset.asyncError;
                if (errorEl) { errorEl.classList.remove('show'); errorEl.innerHTML = ''; }
                input.classList.remove('field-error');
                return;
            }
            try {
                var u = new URL(val);
                if (!u.hostname.includes('spotify.com')) {
                    input.dataset.asyncError = '1';
                    if (errorEl) {
                        errorEl.classList.add('show');
                        errorEl.innerHTML = '<div class="validation-error-message">Spotify URL must be a spotify.com link.</div>';
                    }
                    input.classList.add('field-error');
                    input.classList.remove('field-success');
                    return;
                }
            } catch (e) {
                input.dataset.asyncError = '1';
                if (errorEl) {
                    errorEl.classList.add('show');
                    errorEl.innerHTML = '<div class="validation-error-message">Invalid URL format.</div>';
                }
                input.classList.add('field-error');
                input.classList.remove('field-success');
                return;
            }

            delete input.dataset.asyncError;
            if (errorEl) { errorEl.classList.remove('show'); errorEl.innerHTML = ''; }
            input.classList.remove('field-error');
        };

        var handler = debounce(doCheck, options.debounce || 150);
        input.addEventListener('blur', handler);
    }

    window.FormValidation.attachSpotifyValidators = function (opts) {
        var inputs = document.querySelectorAll('[data-validate-spotify="true"]');
        Array.prototype.forEach.call(inputs, function (input) {
            attachSpotifyValidatorTo(input, opts || {});
        });
    };

    document.addEventListener('DOMContentLoaded', function () {
        try { window.FormValidation.attachNameValidators(); } catch (e) { }
        try { window.FormValidation.attachSpotifyValidators(); } catch (e) { }
    });
})();
