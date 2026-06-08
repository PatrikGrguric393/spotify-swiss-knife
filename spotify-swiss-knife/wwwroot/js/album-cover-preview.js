(function () {
    function showPreview(input) {
        var targetSel = input.getAttribute('data-album-cover-preview-target');
        var preview = targetSel ? document.querySelector(targetSel) : null;
        var img = preview ? preview.querySelector('[data-album-cover-preview-img]') : null;
        if (!preview || !img) return;

        var file = input.files && input.files[0];
        if (!file || !/^image\//.test(file.type)) {
            img.removeAttribute('src');
            preview.hidden = true;
            return;
        }

        var reader = new FileReader();
        reader.onload = function (e) {
            img.src = e.target.result;
            preview.hidden = false;
        };
        reader.readAsDataURL(file);

        var removeSel = input.getAttribute('data-album-cover-remove-target');
        if (removeSel) {
            var removeBox = document.querySelector(removeSel);
            if (removeBox) removeBox.checked = false;
        }
    }

    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('[data-album-cover-input]').forEach(function (input) {
            input.addEventListener('change', function () { showPreview(input); });
        });
    });
})();
