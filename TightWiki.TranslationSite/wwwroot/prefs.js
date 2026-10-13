// Remembers the languages a visitor has chosen. It is a cookie, rather than something kept only in the browser,
//  so that the server can use it when it draws the page and the right suggestions are there from the first moment.
window.twPrefs = {
    // Brings a menu that has just opened (which can be low on the page) fully into view.
    reveal: function (element) {
        if (element && element.scrollIntoView) {
            element.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
        }
    },

    setLanguages: function (codes) {
        var secure = location.protocol === 'https:' ? '; Secure' : '';
        document.cookie = 'tw_languages=' + encodeURIComponent(codes) + '; max-age=31536000; path=/; SameSite=Lax' + secure;
    }
};
