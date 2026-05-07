document.addEventListener("DOMContentLoaded", () => {

    // Variable global para almacenar la instancia del gráfico
    let donutChartInstance = null;

    // 1. Buscar elementos CRÍTICOS
    const timeline = document.getElementById("timelinePasos");
    const container = document.getElementById("pasosContainer");
    

    // 2. Si timeline NO existe, MOSTRAR ERROR y SALIR
    if (!timeline) {
        return;
    }

    if (!container) {
        console.error("❌ ERROR: No se encontró pasosContainer");
        timeline.innerHTML = '<li class="text-danger">Error al cargar datos</li>';
        return;
    }

    // 3. Parsear datos
    let pasos = [];
    try {
        pasos = JSON.parse(container.dataset.pasos || "[]");
    } catch (error) {
        console.error("Error parseando datos:", error);
        timeline.innerHTML = '<li class="text-danger">Error en formato de datos</li>';
        return;
    }

    // 4. Actualizar KPIs
    try {
        const kpiCompletados = document.getElementById("kpiCompletados");
        const kpiPendientes = document.getElementById("kpiPendientes");
        const kpiPorcentaje = document.getElementById("kpiPorcentaje");

        if (kpiCompletados && kpiPendientes && kpiPorcentaje) {
            const completados = pasos.filter(p => p.Completado).length;
            const pendientes = pasos.length - completados;
            const porcentaje = pasos.length > 0 ? Math.round((completados / pasos.length) * 100) : 0;

            kpiCompletados.innerText = completados;
            kpiPendientes.innerText = pendientes;
            kpiPorcentaje.innerText = porcentaje + "%";
        }
    } catch (error) {
        console.error("Error actualizando KPIs:", error);
    }

    // 5. Crear o actualizar gráfico
    try {
        const ctx = document.getElementById("donutChart");
        if (ctx) {
            const completados = pasos.filter(p => p.Completado).length;
            const pendientes = pasos.length - completados;

            if (!donutChartInstance) {
                // Crear nuevo gráfico
                donutChartInstance = new Chart(ctx, {
                    type: "doughnut",
                    data: {
                        labels: ["Completados", "Pendientes"],
                        datasets: [{
                            data: [completados, pendientes],
                            backgroundColor: ["#22c55e", "#dc2626"],
                            borderWidth: 0
                        }]
                    },
                    options: {
                        responsive: true,
                        maintainAspectRatio: false,
                        plugins: {
                            legend: {
                                position: "bottom",
                                labels: {
                                    font: { size: 12 }
                                }
                            },
                            tooltip: {
                                callbacks: {
                                    label: function (context) {
                                        const label = context.label || '';
                                        const value = context.raw || 0;
                                        const total = context.dataset.data.reduce((a, b) => a + b, 0);
                                        const percentage = Math.round((value / total) * 100);
                                        return `${label}: ${value} (${percentage}%)`;
                                    }
                                }
                            }
                        },
                        cutout: '60%',
                        animation: {
                            animateScale: true,
                            animateRotate: true
                        }
                    }
                });
            } else {
                // Actualizar datos si ya existe
                donutChartInstance.data.datasets[0].data = [completados, pendientes];
                donutChartInstance.update();
            }
        }
    } catch (error) {
    }

    timeline.innerHTML = '';

    if (pasos.length === 0) {
        timeline.innerHTML = '<li class="text-muted text-start">No hay pasos para mostrar</li>';
        return;
    }

    pasos.sort((a, b) => a.Id - b.Id);

    pasos.forEach(p => {
        const li = document.createElement("li");
        li.className = "mb-3 text-start"; 

        let fechaInfo = '';
        if (p.Completado && p.FechaCompletado) {
            const fecha = new Date(p.FechaCompletado);
            fechaInfo = `<small class="text-muted ms-2">Completado: ${fecha.toLocaleDateString()}</small>`;
        }

        li.innerHTML = `
        <span class="badge ${p.Completado ? "bg-success" : "bg-secondary"} me-2">
            ${p.Completado ? "✓" : "⋯"}
        </span>
        <span class="${p.Completado ? "text-success fw-bold" : "text-dark"}">
            ${p.Nombre || 'Paso sin nombre'} ${fechaInfo}
        </span>
    `;
        timeline.appendChild(li);
    });

    
});

// Función para limpiar el gráfico cuando se cambie de página
window.addEventListener('beforeunload', () => {
    if (donutChartInstance) {
        donutChartInstance.destroy();
        donutChartInstance = null;
    }
});
