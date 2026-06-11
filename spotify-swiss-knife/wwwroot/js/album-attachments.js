(function () {
    Dropzone.autoDiscover = false;

    const dzEl = document.getElementById('album-attachments-dz');
    const listEl = document.getElementById('attachmentList');

    if (!dzEl || !listEl) return;

    const antiforgeryToken = () =>
        document.querySelector('input[name="__RequestVerificationToken"]').value;

    new Dropzone('#album-attachments-dz', {
        url: dzEl.getAttribute('action'),
        dictDefaultMessage: 'Drop files here or click to upload',
        init: function () {
            this.on('sending', function (file, xhr, formData) {
                formData.append('__RequestVerificationToken', antiforgeryToken());
            });
            this.on('success', function () {
                loadAttachments();
            });
            this.on('error', function (file, errorMessage) {
                const msg = typeof errorMessage === 'string'
                    ? errorMessage
                    : (errorMessage.error || 'Upload failed.');
                const errEl = file.previewElement.querySelector('.dz-error-message span');
                if (errEl) errEl.textContent = msg;
            });
        }
    });

    async function loadAttachments() {
        const url = listEl.dataset.listUrl;
        try {
            const res = await fetch(url);
            if (!res.ok) throw new Error('Failed to load attachments.');
            listEl.innerHTML = await res.text();
        } catch (e) {
            listEl.innerHTML = '<p class="files-empty">' + e.message + '</p>';
        }
    }

    listEl.addEventListener('click', async function (e) {
        const btn = e.target.closest('.attachment-delete-btn');
        if (!btn) return;
        if (!confirm(btn.dataset.confirm || 'Delete this attachment?')) return;
        try {
            const res = await fetch(btn.dataset.deleteUrl, {
                method: 'POST',
                headers: { 'RequestVerificationToken': antiforgeryToken() }
            });
            if (!res.ok) throw new Error('Delete failed.');
            loadAttachments();
        } catch (err) {
            alert(err.message);
        }
    });

    loadAttachments();
})();
