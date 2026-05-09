document.addEventListener('DOMContentLoaded', function () {
    const forms = document.querySelectorAll('.sd-form-mark-step');

    forms.forEach(function (form) {
        const btn       = form.querySelector('.sd-btn-mark-step');
        let   completed = form.dataset.completed === 'true';

        if (completed) setCompletedState(btn);

        form.addEventListener('submit', async function (e) {
            e.preventDefault();
            if (completed) return;

            btn.disabled = true;
            const isHero = btn.classList.contains('sh-btn');
            btn.innerHTML = isHero
                ? '<i class="fas fa-spinner fa-spin me-1"></i>Procesando…'
                : '<i class="fas fa-spinner fa-spin me-2"></i>Procesando…';

            try {
                const payload  = new FormData(form);
                const response = await fetch(form.action, {
                    method:  'POST',
                    headers: { 'Accept': 'application/json' },
                    body:    payload
                });

                const result = await response.json();

                if (result.success) {
                    completed = true;
                    setCompletedState(btn);
                    showToast('Éxito', result.message, 'success');
                } else {
                    resetState(btn);
                    showToast('Error', result.message, 'danger');
                }
            } catch (err) {
                console.error('[StepTracking] Submit error:', err);
                resetState(btn);
                showToast('Error', 'Ocurrió un error inesperado.', 'danger');
            }
        });
    });

    function setCompletedState(btn) {
        btn.disabled = true;
        if (btn.classList.contains('sh-btn')) {
            btn.classList.add('is-done');
            btn.innerHTML = '<i class="fas fa-check-circle me-1"></i>¡Completado!';
        } else {
            btn.classList.remove('btn-outline-success');
            btn.classList.add('btn-success');
            btn.innerHTML = '<i class="fas fa-check-circle me-2"></i>¡Completado!';
        }
    }

    function resetState(btn) {
        btn.disabled = false;
        if (btn.classList.contains('sh-btn')) {
            btn.classList.remove('is-done');
            btn.innerHTML = '<i class="far fa-check-circle me-1"></i>Marcar Completado';
        } else {
            btn.classList.remove('btn-success');
            btn.classList.add('btn-outline-success');
            btn.innerHTML = '<i class="far fa-check-circle me-2"></i>Marcar como completado';
        }
    }

    function showToast(title, message, type) {
        const container = document.createElement('div');
        container.className = 'toast-container position-fixed top-0 end-0 p-3';
        container.style.zIndex = '9999';
        container.innerHTML = `
            <div class="toast show" role="alert" aria-live="assertive" aria-atomic="true">
                <div class="toast-header bg-${type} text-white">
                    <strong class="me-auto">${title}</strong>
                    <button type="button" class="btn-close btn-close-white" data-bs-dismiss="toast"></button>
                </div>
                <div class="toast-body">${message}</div>
            </div>`;
        document.body.appendChild(container);
        setTimeout(() => container.remove(), 4000);
    }
});
