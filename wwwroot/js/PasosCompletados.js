document.addEventListener('DOMContentLoaded', function () {
    const forms = document.querySelectorAll('.form-completado');

    forms.forEach(form => {
        const btn = form.querySelector('.btn-completado');
        let completado = form.dataset.completado === "true";

        if (completado) setBotonCompletado(btn);

        form.addEventListener('submit', async function (e) {
            e.preventDefault();
            if (completado) return;

            btn.disabled = true;
            const isHero = btn.classList.contains('sh-btn');
            btn.innerHTML = isHero
                ? '<i class="fas fa-spinner fa-spin me-1"></i>Procesando...'
                : '<i class="fas fa-spinner fa-spin me-2"></i>Procesando...';

            try {
                const formData = new FormData(form);
                const response = await fetch(form.action, {
                    method: 'POST',
                    headers: { 'Accept': 'application/json' },
                    body: formData
                });

                const result = await response.json();

                if (result.success) {
                    completado = true;
                    setBotonCompletado(btn);
                    mostrarToast('Éxito', result.message, 'success');
                } else {
                    resetBoton(btn);
                    mostrarToast('Error', result.message, 'danger');
                }
            } catch (err) {
                console.error(err);
                resetBoton(btn);
                mostrarToast('Error', 'Ocurrió un error inesperado', 'danger');
            }
        });
    });

    function setBotonCompletado(btn) {
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

    function resetBoton(btn) {
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

    function mostrarToast(titulo, mensaje, tipo) {
        const toastContainer = document.createElement('div');
        toastContainer.className = 'toast-container position-fixed top-0 end-0 p-3';
        toastContainer.innerHTML = `
            <div class="toast show" role="alert" aria-live="assertive" aria-atomic="true">
                <div class="toast-header bg-${tipo} text-white">
                    <strong class="me-auto">${titulo}</strong>
                    <button type="button" class="btn-close btn-close-white" data-bs-dismiss="toast"></button>
                </div>
                <div class="toast-body">${mensaje}</div>
            </div>`;
        document.body.appendChild(toastContainer);
        setTimeout(() => toastContainer.remove(), 4000);
    }
});
