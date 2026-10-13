// Carry on automatically as soon as the captcha is solved.
function twGateSolved() {
    var form = document.getElementById('gateForm');
    if (form) { form.submit(); }
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
