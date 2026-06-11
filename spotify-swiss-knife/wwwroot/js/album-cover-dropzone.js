(function () {
    Dropzone.autoDiscover = false;

    function setMessage(dz, text) {
        var msgEl = dz.element.querySelector('.dz-message');
        if (msgEl) msgEl.textContent = text;
        dz.element.setAttribute('aria-label', 'Album cover upload. ' + text);
    }

    function initCoverDropzone(el) {
        var inputSel = el.getAttribute('data-cover-input-target');
        var input = inputSel ? document.querySelector(inputSel) : null;
        if (!input) return;

        var defaultMessage = el.getAttribute('data-cover-default-message')
            || 'Drop cover image here or click to select';

        var dz = new Dropzone(el, {
            url: '#',
            autoProcessQueue: false,
            uploadMultiple: false,
            maxFiles: 1,
            createImageThumbnails: false,
            addRemoveLinks: false,
            acceptedFiles: 'image/jpeg,image/png,image/gif,image/webp',
            dictDefaultMessage: defaultMessage
        });

        el.tabIndex = 0;
        el.setAttribute('role', 'button');
        el.setAttribute('aria-describedby', 'error-CoverImage');
        setMessage(dz, defaultMessage);

        el.addEventListener('keydown', function (e) {
            if (e.key === 'Enter' || e.key === ' ' || e.key === 'Spacebar') {
                e.preventDefault();
                dz.hiddenFileInput.click();
            }
        });

        function syncToInput(file) {
            var dt = new DataTransfer();
            if (file) dt.items.add(file);
            input.files = dt.files;
            input.dispatchEvent(new Event('change', { bubbles: true }));
        }

        dz.on('addedfile', function (file) {
            if (dz.files.length > 1) {
                dz.removeFile(dz.files[0]);
            }
            syncToInput(file);
            setMessage(dz, 'Selected: ' + file.name + ' — click to choose a different file');
        });

        dz.on('removedfile', function () {
            if (dz.files.length === 0) {
                syncToInput(null);
                setMessage(dz, defaultMessage);
            }
        });

        // Edit screen: checking "Remove current cover" clears any staged file.
        var removeSel = input.getAttribute('data-album-cover-remove-target');
        if (removeSel) {
            var removeBox = document.querySelector(removeSel);
            if (removeBox) {
                removeBox.addEventListener('change', function () {
                    if (removeBox.checked) dz.removeAllFiles();
                });
            }
        }
    }

    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('[data-album-cover-dz]').forEach(initCoverDropzone);
    });
})();
