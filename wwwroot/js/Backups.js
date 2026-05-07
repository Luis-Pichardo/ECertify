document.addEventListener('DOMContentLoaded', function () {
    const formCompletado = document.getElementById('formCompletado');
    const btnCompletado = document.getElementById('btnCompletado');
    const pasoData = document.getElementById('pasoData');

    if (!formCompletado || !btnCompletado) {
        console.error('Elementos no encontrados');
        return;
    }

    // Obtener estado inicial
    const pasoCompletado = typeof window.pasoCompletado !== 'undefined' ?
        window.pasoCompletado :
        (pasoData ? pasoData.dataset.completado === 'True' : false);

    console.log('Estado del paso (JavaScript):', pasoCompletado);

    // Actualizar estado inicial del botón
    actualizarEstadoBoton(pasoCompletado);

    // Manejar envío del formulario
    formCompletado.addEventListener('submit', async function (e) {
        e.preventDefault();

        // ✅ si ya está completado, no vuelvas a enviar
        if (pasoCompletado === true) {
            return;
        }

        const originalText = btnCompletado.innerHTML;
        const originalClass = btnCompletado.className;

        // Mostrar estado de carga
        btnCompletado.disabled = true;
        btnCompletado.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i>Procesando...';

        try {
            // Obtener token anti-CSRF
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

            if (!token) {
                throw new Error('Token de seguridad no encontrado');
            }

            // Crear FormData directamente del formulario
            const formData = new FormData(formCompletado);

            const response = await fetch(formCompletado.action, {
                method: 'POST',
                headers: { 'Accept': 'application/json' },
                body: formData
            });

            if (response.ok) {
                const result = await response.json();

                if (result.success) {
                    // ✅ marcar como completado en la variable y en la UI
                    pasoCompletado = true;
                    actualizarEstadoBoton(true);
                    mostrarMensajeExito('¡Paso completado correctamente!');
                } else {
                    throw new Error(result.message || 'Error al completar el paso');
                }
            } else {
                const errorText = await response.text();
                throw new Error(`Error ${response.status}: ${errorText}`);
            }

        } catch (err) {
            console.error('Error en la solicitud:', err);
            mostrarMensajeError('Error: ' + err.message);

            // Restaurar estado original del botón
            btnCompletado.innerHTML = originalText;
            btnCompletado.className = originalClass;
            btnCompletado.disabled = false;
        }
    });


    function actualizarEstadoBoton(completado) {
        if (completado) {
            btnCompletado.classList.remove('btn-outline-success');
            btnCompletado.classList.add('btn-success');
            btnCompletado.innerHTML = '<i class="fas fa-check-circle me-2"></i>¡Completado!';
            btnCompletado.disabled = true;

            // Deshabilitar el formulario completo
            formCompletado.style.pointerEvents = 'none';
            formCompletado.style.opacity = '0.7';
        }
    }

    function mostrarMensajeExito(mensaje) {
        // Crear toast de éxito
        const toastContainer = document.querySelector('.toast-container') || crearContenedorToasts();
        const toast = document.createElement('div');
        toast.className = 'toast show';
        toast.innerHTML = `
            <div class="toast-header bg-success text-white">
                <strong class="me-auto">Éxito</strong>
                <button type="button" class="btn-close btn-close-white" data-bs-dismiss="toast"></button>
            </div>
            <div class="toast-body">${mensaje}</div>
        `;
        toastContainer.appendChild(toast);

        // Auto-eliminar después de 5 segundos
        setTimeout(() => {
            toast.remove();
        }, 5000);
    }

    function mostrarMensajeError(mensaje) {
        // Crear toast de error
        const toastContainer = document.querySelector('.toast-container') || crearContenedorToasts();
        const toast = document.createElement('div');
        toast.className = 'toast show';
        toast.innerHTML = `
            <div class="toast-header bg-danger text-white">
                <strong class="me-auto">Error</strong>
                <button type="button" class="btn-close btn-close-white" data-bs-dismiss="toast"></button>
            </div>
            <div class="toast-body">${mensaje}</div>
        `;
        toastContainer.appendChild(toast);

        // Auto-eliminar después de 5 segundos
        setTimeout(() => {
            toast.remove();
        }, 5000);
    }

    function crearContenedorToasts() {
        const container = document.createElement('div');
        container.className = 'toast-container position-fixed top-0 end-0 p-3';
        document.body.appendChild(container);
        return container;
    }
});