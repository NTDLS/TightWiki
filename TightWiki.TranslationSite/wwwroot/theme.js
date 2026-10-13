// Follow the visitor's light/dark preference, and do it before the page paints to avoid a flash of the wrong theme.
(function () {
    var query = window.matchMedia('(prefers-color-scheme: dark)');
    var root = document.documentElement;

    var apply = function () {
        var wanted = query.matches ? 'dark' : 'light';
        if (root.getAttribute('data-bs-theme') !== wanted) {
            root.setAttribute('data-bs-theme', wanted);
        }
    };

    apply();

    if (query.addEventListener) {
        query.addEventListener('change', apply);
    }

    // Blazor's enhanced navigation (changing the language, following a link) updates the page from the HTML that the
    //  server sent, which has the default theme on the <html> element. Put the preference back whenever that happens.
    //  This runs before the browser paints, so there is no flash.
    new MutationObserver(apply).observe(root, { attributes: true, attributeFilter: ['data-bs-theme'] });
})();
