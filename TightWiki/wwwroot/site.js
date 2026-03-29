$(function () {
    localStorage.setItem('theme', '@sessionState.UserTheme.EditorTheme');

    $('nav')
        .removeClass('navbar-light bg-white')
        .addClass('@sessionState.UserTheme.ClassNavBar');

    $('a.nav-link')
        .removeClass('text-dark')
        .addClass('@sessionState.UserTheme.ClassNavLink');

    $('a.dropdown-item')
        .removeClass('text-dark')
        .addClass('@sessionState.UserTheme.ClassDropdown');

    $('.site-branding')
        .removeClass('text-dark')
        .addClass('@sessionState.UserTheme.ClassBranding');

    @if (GlobalConfiguration.FixedMenuPosition) {
        <text>
            const navHeight = $('nav').outerHeight() + 20;
            $('#mainContainer').css('margin-top', navHeight + 'px');
        </text>
    }

    setTimeout(function () {
        document.querySelectorAll('.auto-dismiss-alert').forEach(function (alertElement) {
            if (alertElement) {
                alertElement.classList.remove('show');
                setTimeout(() => alertElement.remove(), 300);
            }
        });
    }, 5000);
});