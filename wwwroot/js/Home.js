(() => {
    const container = document.getElementById('stepsDataContainer');
    if (!container) return;

    let steps = [];
    try {
        steps = JSON.parse(container.dataset.steps || '[]');
    } catch {
        return;
    }

    const completed = steps.filter(s => s.Completed).length;
    const pending   = steps.length - completed;
    const percent   = steps.length > 0 ? Math.round((completed / steps.length) * 100) : 0;

    // ── Today's date ──
    const dateEl = document.getElementById('heroDate');
    if (dateEl) {
        dateEl.textContent = new Date().toLocaleDateString('es-DO', {
            weekday: 'short', day: '2-digit', month: 'short', year: 'numeric'
        });
    }

    // ── Hero chips ──
    const setText = (id, text) => { const el = document.getElementById(id); if (el) el.textContent = text; };
    setText('heroChipCompleted', `${completed} completado${completed !== 1 ? 's' : ''}`);
    setText('heroChipPending',   `${pending} pendiente${pending !== 1 ? 's' : ''}`);
    setText('heroChipPercent',   `${percent}% avance`);

    // ── Animated number counter ──
    function animateCount(id, target, suffix = '') {
        const el = document.getElementById(id);
        if (!el) return;
        let current  = 0;
        const increment = Math.max(1, Math.ceil(target / 28));
        const timer  = setInterval(() => {
            current = Math.min(current + increment, target);
            el.textContent = current + suffix;
            if (current >= target) clearInterval(timer);
        }, 42);
    }

    animateCount('kpiCompleted',  completed);
    animateCount('kpiPending',    pending);
    animateCount('kpiTotal',      steps.length);
    animateCount('progressValue', percent, '%');

    // ── Mark nav cards completed ──
    document.querySelectorAll('.nav-card[data-step-name]').forEach(card => {
        const isDone = steps.some(s => s.Name === card.dataset.stepName && s.Completed);
        if (isDone) card.classList.add('nc--done');
    });

    // ── Progress donut chart ──
    const chartCanvas = document.getElementById('progressChart');
    if (chartCanvas && typeof Chart !== 'undefined') {
        const hasData = completed > 0 || pending > 0;
        new Chart(chartCanvas, {
            type: 'doughnut',
            data: {
                labels: ['Completados', 'Pendientes'],
                datasets: [{
                    data: hasData ? [completed, pending] : [0, steps.length || 1],
                    backgroundColor: ['#16a34a', '#f1f5f9'],
                    borderWidth: 0,
                    hoverBackgroundColor: ['#15803d', '#e5e7eb'],
                    hoverOffset: 5
                }]
            },
            options: {
                cutout: '80%',
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        callbacks: {
                            label: ctx => ` ${ctx.label}: ${ctx.raw} paso${ctx.raw !== 1 ? 's' : ''}`
                        }
                    }
                },
                animation: { duration: 1200, easing: 'easeInOutQuart' }
            }
        });
    }

    // ── Certification stepper ──
    const stepperEl = document.getElementById('dashStepper');
    if (!stepperEl) return;

    steps.sort((a, b) => a.Id - b.Id);

    stepperEl.innerHTML = steps.map((s, i) => {
        const done   = s.Completed;
        const isLast = i === steps.length - 1;
        const date   = done && s.CompletedOn
            ? new Date(s.CompletedOn).toLocaleDateString('es-DO', {
                  day: '2-digit', month: 'short', year: 'numeric'
              })
            : null;

        return `
        <div class="step-row">
            <div class="step-track">
                <div class="step-dot ${done ? 'sd--done' : 'sd--pend'}">
                    ${done ? '<i class="bi bi-check-lg"></i>' : `<span>${s.Id}</span>`}
                </div>
                ${!isLast ? `<div class="step-line ${done ? 'sl--done' : ''}"></div>` : ''}
            </div>
            <div class="step-info">
                <span class="step-name ${done ? 'sn--done' : 'sn--pend'}">${s.Name || '—'}</span>
                ${date ? `<span class="step-date"><i class="bi bi-calendar3 me-1"></i>${date}</span>` : ''}
                <span class="step-pill ${done ? 'sp--done' : 'sp--pend'}">
                    ${done
                        ? '<i class="bi bi-check-circle-fill"></i> Completado'
                        : '<i class="bi bi-clock"></i> Pendiente'}
                </span>
            </div>
        </div>`;
    }).join('');
})();
