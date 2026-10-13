// Remembers the languages a visitor has chosen. It is a cookie, rather than something kept only in the browser,
//  so that the server can use it when it draws the page and the right suggestions are there from the first moment.
window.twPrefs = {
    setLanguages: function (codes) {
        var secure = location.protocol === 'https:' ? '; Secure' : '';
        document.cookie = 'tw_languages=' + encodeURIComponent(codes) + '; max-age=31536000; path=/; SameSite=Lax' + secure;
    }
};
