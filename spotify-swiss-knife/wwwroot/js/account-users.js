(function () {
    document.querySelectorAll('.role-select').forEach(function (select) {
        select.addEventListener('change', function () {
            var form = select.closest('form');
            if (form) form.submit();
        });
    });
})();
</content>
