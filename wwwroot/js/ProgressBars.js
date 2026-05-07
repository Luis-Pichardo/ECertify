document.addEventListener('DOMContentLoaded', function () {
    const steps = document.querySelectorAll('#certificationSteps .nav-link');
    const path = window.location.pathname.toLowerCase();

    steps.forEach(step => {
        const stepUrl = step.getAttribute('data-step-url').toLowerCase();

        // Activar el paso si coincide con la URL
        if (path === stepUrl) {
            step.classList.add('active');
        } else {
            step.classList.remove('active');
        }

        // Permitir navegación haciendo clic
        step.addEventListener('click', function () {
            window.location.href = stepUrl;
        });
    });

    // Opcional: Marcar como "completado" los pasos anteriores
    let activeFound = false;
    steps.forEach(step => {
        if (step.classList.contains('active')) {
            activeFound = true;
        } else if (!activeFound) {
            step.classList.add('completed');
        } else {
            step.classList.remove('completed');
        }
    });
});
