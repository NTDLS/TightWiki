/*
    Shared client-side helpers for TightWiki pages.
    Localized labels are provided by the layout in window.twText.
*/
(function () {
    const text = Object.assign({ confirmTitle: 'Are you sure?', yes: 'Yes', no: 'No', close: 'Close' }, window.twText || {});

    /**
     * Shows a Bootstrap confirmation dialog, resolving to true if the user chose "Yes".
     * The message is always displayed as text.
     */
    window.twConfirm = function (message) {
        return new Promise(function (resolve) {
            const modalElement = document.createElement('div');
            modalElement.className = 'modal fade';
            modalElement.tabIndex = -1;
            modalElement.setAttribute('aria-hidden', 'true');
            modalElement.innerHTML =
                '<div class="modal-dialog modal-dialog-centered"><div class="modal-content">' +
                '<div class="modal-header"><h5 class="modal-title"></h5><button type="button" class="btn-close" data-bs-dismiss="modal"></button></div>' +
                '<div class="modal-body tw-message"></div>' +
                '<div class="modal-footer"><button type="button" class="btn btn-secondary" data-bs-dismiss="modal"></button>' +
                '<button type="button" class="btn btn-danger"></button></div>' +
                '</div></div>';

            modalElement.querySelector('.modal-title').textContent = text.confirmTitle;
            modalElement.querySelector('.modal-body').textContent = message;
            modalElement.querySelector('.btn-close').setAttribute('aria-label', text.close);
            const [noButton, yesButton] = modalElement.querySelectorAll('.modal-footer .btn');
            noButton.textContent = text.no;
            yesButton.textContent = text.yes;

            document.body.appendChild(modalElement);
            const modal = new bootstrap.Modal(modalElement);
            let confirmed = false;

            yesButton.addEventListener('click', function () {
                confirmed = true;
                modal.hide();
            });
            //These confirm destructive actions, so the safe choice has the focus.
            modalElement.addEventListener('shown.bs.modal', function () {
                noButton.focus();
            });
            modalElement.addEventListener('hidden.bs.modal', function () {
                modal.dispose();
                modalElement.remove();
                resolve(confirmed);
            });

            modal.show();
        });
    };

    //Links with a data-tw-confirm="message" attribute only navigate after the user confirms.
    document.addEventListener('click', function (event) {
        const link = event.target.closest('a[data-tw-confirm]');
        if (!link) {
            return;
        }
        event.preventDefault();
        window.twConfirm(link.getAttribute('data-tw-confirm')).then(function (confirmed) {
            if (confirmed) {
                window.location.href = link.href;
            }
        });
    });

    //Success messages dismiss themselves after a few seconds.
    document.addEventListener('DOMContentLoaded', function () {
        setTimeout(function () {
            document.querySelectorAll('.auto-dismiss-alert').forEach(function (alertElement) {
                alertElement.classList.remove('show');
                setTimeout(function () { alertElement.remove(); }, 300);
            });
        }, 5000);
    });
})();
