(function () {
    Dropzone.autoDiscover = false;

    function antiforgeryToken() {
        var input = document.querySelector('input[name="__RequestVerificationToken"]');
        return input ? input.value : '';
    }

    function initFilesDropzone(el) {
        var dz = new Dropzone(el, {
            url: '/files/upload',
            method: 'post',
            paramName: 'file',
            uploadMultiple: false,
            parallelUploads: 3,
            maxFiles: null,
            maxFilesize: 512, // MB; server allows up to 512MB
            createImageThumbnails: false,
            addRemoveLinks: true,
            timeout: 0,
            headers: { 'RequestVerificationToken': antiforgeryToken() },
            dictDefaultMessage: 'Drop files here or click to select',
            dictRemoveFile: 'Clear',
            dictCancelUpload: 'Cancel',
            dictFileTooBig: 'File is too big ({{filesize}}MB). Max: {{maxFilesize}}MB.'
        });

        el.tabIndex = 0;

        el.addEventListener('keydown', function (e) {
            if (e.key === 'Enter' || e.key === ' ' || e.key === 'Spacebar') {
                e.preventDefault();
                dz.hiddenFileInput.click();
            }
        });

        dz.on('success', function (file) {
            if (typeof window.loadFiles === 'function') {
                window.loadFiles();
            }
            // Auto-clear the completed preview so the dropzone stays tidy.
            setTimeout(function () { dz.removeFile(file); }, 2500);
        });

        dz.on('error', function (file, message) {
            var text = message;
            if (message && typeof message === 'object') {
                text = message.error || 'Upload failed.';
            }
            var node = file.previewElement
                ? file.previewElement.querySelector('[data-dz-errormessage]')
                : null;
            if (node) node.textContent = text;
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        var el = document.getElementById('files-dropzone');
        if (el) initFilesDropzone(el);
    });
})();
