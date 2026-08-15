// All page behaviour lives here rather than in inline <script> blocks, so the
// Content-Security-Policy can forbid inline script.
(() => {
    'use strict';

    // Bootstrap client-side validation.
    document.querySelectorAll('.needs-validation').forEach(form => {
        form.addEventListener('submit', event => {
            if (!form.checkValidity()) {
                event.preventDefault();
                event.stopPropagation();
            }
            form.classList.add('was-validated');
        }, false);
    });

    // Show / hide a password field. data-toggle-password holds the input's id.
    document.querySelectorAll('[data-toggle-password]').forEach(button => {
        button.addEventListener('click', () => {
            const input = document.getElementById(button.getAttribute('data-toggle-password'));
            if (!input) {
                return;
            }
            const hidden = input.type === 'password';
            input.type = hidden ? 'text' : 'password';
            button.textContent = hidden ? 'Hide' : 'Show';
        });
    });

    // "Confirm password" must match the field named in data-match.
    document.querySelectorAll('[data-match]').forEach(confirmField => {
        const source = document.getElementById(confirmField.getAttribute('data-match'));
        if (!source) {
            return;
        }
        const check = () => confirmField.setCustomValidity(
            confirmField.value === source.value ? '' : 'Passwords do not match.');
        source.addEventListener('input', check);
        confirmField.addEventListener('input', check);
    });

    // Ask before running a destructive form. data-confirm holds the question.
    document.querySelectorAll('form[data-confirm]').forEach(form => {
        form.addEventListener('submit', event => {
            if (!window.confirm(form.getAttribute('data-confirm'))) {
                event.preventDefault();
            }
        });
    });

    // A select inside .js-autosubmit posts its form as soon as it changes.
    document.querySelectorAll('.js-autosubmit select').forEach(select => {
        select.addEventListener('change', () => select.form.submit());
    });

    // System screen: the row "Edit" buttons feed the hidden edit form.
    const editForm = document.getElementById('editForm');
    if (editForm) {
        document.querySelectorAll('.js-edit').forEach(button => {
            button.addEventListener('click', () => {
                const current = button.getAttribute('data-email');
                const next = window.prompt('New email address:', current);
                if (next && next.trim() && next.trim() !== current) {
                    document.getElementById('editId').value = button.getAttribute('data-id');
                    document.getElementById('editEmail').value = next.trim();
                    editForm.submit();
                }
            });
        });
    }
})();
