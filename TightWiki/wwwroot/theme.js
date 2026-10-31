/*
    Color theme for TightWiki.

    The preference is "auto" (follow the browser's prefers-color-scheme), "light" or "dark". It is stored only in this
    browser (localStorage), and is applied to the page as Bootstrap's data-bs-theme attribute on <html>.

    This file is loaded in the <head> so that the theme is applied before the page is painted (no flash of the wrong theme).

    Pages that need to react to a change (such as the CodeMirror editor) can listen for the "themechange" event on document:
        document.addEventListener("themechange", e => { console.log(e.detail.theme); }); //"light" or "dark"
*/
(function () {
    const storageKey = 'tightwiki-theme';
    const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');

    function getPreference() {
        try {
            const stored = localStorage.getItem(storageKey);
            if (stored === 'light' || stored === 'dark') return stored;
        } catch (e) { /* Storage can be blocked; fall back to following the browser. */ }
        return 'auto';
    }

    function resolve(preference) {
        return preference === 'auto' ? (mediaQuery.matches ? 'dark' : 'light') : preference;
    }

    function apply() {
        const preference = getPreference();
        const theme = resolve(preference);
        document.documentElement.setAttribute('data-bs-theme', theme);

        //Reflect the choice in the selector (it may not exist yet when this first runs from the head).
        document.querySelectorAll('[data-theme-choice]').forEach(function (el) {
            const active = el.getAttribute('data-theme-choice') === preference;
            el.classList.toggle('active', active);
            el.setAttribute('aria-pressed', active ? 'true' : 'false');
        });
        const icon = document.getElementById('themeToggleIcon');
        if (icon) {
            icon.className = 'bi ' + (preference === 'auto' ? 'bi-circle-half' : theme === 'dark' ? 'bi-moon-stars-fill' : 'bi-sun-fill');
        }

        //Stylesheets that only apply to one theme are switched on and off with their media attribute.
        document.querySelectorAll('link[data-theme-only]').forEach(function (el) {
            el.media = el.getAttribute('data-theme-only') === theme ? 'all' : 'not all';
        });

        document.dispatchEvent(new CustomEvent('themechange', { detail: { theme: theme, preference: preference } }));
    }

    window.twTheme = {
        /** The theme currently in effect: "light" or "dark". */
        get current() { return resolve(getPreference()); },
        /** The user's preference: "auto", "light" or "dark". */
        get preference() { return getPreference(); },
        /** Sets the preference to "auto", "light" or "dark". */
        set: function (preference) {
            try {
                if (preference === 'light' || preference === 'dark') localStorage.setItem(storageKey, preference);
                else localStorage.removeItem(storageKey);
            } catch (e) { /* Not persisted, but still applied for this page. */ }
            apply();
        }
    };

    //Apply right away so the page never flashes the wrong theme.
    document.documentElement.setAttribute('data-bs-theme', resolve(getPreference()));

    //Follow the browser's setting while the preference is "auto".
    mediaQuery.addEventListener('change', function () { if (getPreference() === 'auto') apply(); });

    //Keep other tabs in step.
    window.addEventListener('storage', function (e) { if (e.key === storageKey) apply(); });

    //The selector and the theme-only stylesheets are in the page after this script runs.
    document.addEventListener('DOMContentLoaded', apply);
    apply();
})();
