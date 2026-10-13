// Carry on automatically as soon as the captcha is solved.
function twGateSolved() {
    var form = document.getElementById('gateForm');
    if (form) { form.submit(); }
}

// Turnstile's own error shows only "Unable to connect to website", which says nothing about why. These are the
//  error codes that site owners run into, with what to check. The address and site key are shown so that they can be
//  compared with the widget in the Cloudflare dashboard (neither is secret, both are in the page's source).
function twGateError(code) {
    var box = document.getElementById('gateWidgetError');
    if (!box) { return false; }

    var widget = document.querySelector('.cf-turnstile');
    var host = location.hostname;
    var siteKey = widget ? widget.getAttribute('data-sitekey') : '';

    var advice = {
        '110100': 'Cloudflare does not recognise the site key that this page is using.',
        '110110': 'Cloudflare does not recognise the site key that this page is using.',
        '110200': 'Cloudflare does not allow this widget on this address. In the Cloudflare dashboard, the hostnames of the widget that owns this site key must include ' + host + '.',
        '110420': 'The widget was asked for an action that is not allowed.',
        '110500': 'This browser is not supported by the check.',
        '110510': 'The browser reports inconsistent information about itself, which can happen with some privacy extensions.'
    };

    var message = advice[String(code)] || 'The check could not run.';

    box.querySelector('[data-role="message"]').textContent = message;
    box.querySelector('[data-role="code"]').textContent = String(code);
    box.querySelector('[data-role="host"]').textContent = host;
    box.querySelector('[data-role="key"]').textContent = siteKey;
    box.classList.remove('d-none');

    // Returning true tells Turnstile that the error has been dealt with, so it doesn't also report it as unhandled.
    return true;
}

// The Turnstile script is only needed (and only loaded) on the page that shows the captcha.
document.addEventListener('DOMContentLoaded', function () {
    if (document.querySelector('.cf-turnstile')) {
        var script = document.createElement('script');
        script.src = 'https://challenges.cloudflare.com/turnstile/v0/api.js';
        script.async = true;
        script.defer = true;
        document.head.appendChild(script);
    }
});
