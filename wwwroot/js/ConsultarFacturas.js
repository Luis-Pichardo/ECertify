$(document).ready(function () {

    var table = $('#datos').DataTable({
        pageLength: 10,
        lengthMenu: [5, 10, 25, 50],
        language: {
            url: '/Json/es-ES.json'
        }
    });

    // Modal TrackId
    $(document).on('click', '.ver-trackid-btn', function () {
        var trackId = $(this).data('trackid');
        $('#trackIdModalContent').text(trackId);
    });

    function renderDetalleFactura(factura) {

        return `
        <div class="p-2">
            <table class="table table-sm table-bordered mb-0 tabla-detalle">
                <tbody>
                    <tr>
                        <th scope="row">Fecha Recepción</th>
                        <td>${formatearFechaHora(factura.FechaRecepcion)}</td>
                    </tr>
                    <tr>
                        <th scope="row">No. Comprobante</th>
                        <td>${factura.ENCF || 'N/A'}</td>
                    </tr>
                    <tr>
                        <th scope="row">RNC Emisor</th>
                        <td>${factura.RncEmisor || 'N/A'}</td>
                    </tr>
                    <tr>
                        <th scope="row">RNC Comprador</th>
                        <td>${factura.RncComprador || 'N/A'}</td>
                    </tr>
                    <tr>
                        <th scope="row">Razón Social</th>
                        <td>${factura.RazonSocialComprador || 'N/A'}</td>
                    </tr>
                    <tr>
                        <th scope="row">Código Seguridad</th>
                        <td>${factura.CodigoSeguridad || 'N/A'}</td>
                    </tr>
                    <tr>
                        <th scope="row">TrackID</th>
                        <td>${factura.TrackId || 'N/A'}</td>
                    </tr>
                    <tr>
                        <th scope="row">Estado</th>
                        <td>${factura.Estado || 'N/A'}</td>
                    </tr>
                    <tr>
                    <tr>
                    <th scope="row">Mensaje</th>
                    <td>${factura.MensajeValor}</td>
                    </tr>
                    <tr>
                        <th scope="row">Código</th>
                        <td>${factura.MensajeCodigo}</td>
                    </tr>
                </tbody>
            </table>
        </div>
    `;
    }

    $('#datos tbody').on('click', '.btn-expand', function () {
        const tr = $(this).closest('tr');
        const row = table.row(tr);

        const button = $(this);
        const expanded = button.attr('aria-expanded') === 'true';

        if (expanded) {
            row.child.hide();
            tr.removeClass('shown');
            button.attr('aria-expanded', 'false');
            button.html('&gt;'); // >
        } else {
            const encf = tr.data('encf');
            const factura = todasLasFacturas.find(f => f.ENCF.trim() === String(encf).trim());
            if (factura) {
                row.child(renderDetalleFactura(factura)).show();
                tr.addClass('shown');
                button.attr('aria-expanded', 'true');
                button.html('&or;'); // v
            }
        }
    });


    function formatearFechaHora(fechaIso) {
        if (!fechaIso) return 'N/A';

        const fecha = new Date(fechaIso);
        return new Intl.DateTimeFormat('es-DO', {
            day: '2-digit',
            month: '2-digit',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit',
            second: '2-digit',
            hour12: true
        }).format(fecha);
    }

    //Trabajar con los filtros en el local Storage
    // Evento para aplicar filtros y guardar en sessionStorage
    document.getElementById("btnBuscar").addEventListener("click", function (e) {
        e.preventDefault();

        const filtros = {
            fechaInicio: document.getElementById("fechaInicio").value,
            fechaFin: document.getElementById("fechaFin").value,
            estado: document.getElementById("estado").value,
            tipo: document.getElementById("tipo").value,
            search: document.getElementById("search").value
        };

        // Guardar en sessionStorage
        sessionStorage.setItem("filtrosFacturas", JSON.stringify(filtros));

        // Aplicar filtros directamente
        const filtradas = aplicarFiltros(todasLasFacturas, filtros);
        renderTabla(filtradas);
    });

    //Filtros iniciales
    function obtenerFiltrosActuales() {
        return {
            fechaInicio: document.getElementById("fechaInicio").value,
            fechaFin: document.getElementById("fechaFin").value,
            estado: document.getElementById("estado").value,
            tipo: document.getElementById("tipo").value,
            search: document.getElementById("search").value
        };
    }


    // Recuperar filtros guardados y aplicar automáticamente al cargar la página
    document.addEventListener("DOMContentLoaded", function () {
        const filtrosGuardados = sessionStorage.getItem("filtrosFacturas");
        if (filtrosGuardados) {
            const filtros = JSON.parse(filtrosGuardados);

            if (filtros.fechaInicio) document.getElementById("fechaInicio").value = filtros.fechaInicio;
            if (filtros.fechaFin) document.getElementById("fechaFin").value = filtros.fechaFin;
            if (filtros.estado) document.getElementById("estado").value = filtros.estado;
            if (filtros.tipo) document.getElementById("tipo").value = filtros.tipo;
            if (filtros.search) document.getElementById("search").value = filtros.search;

            const filtradas = aplicarFiltros(todasLasFacturas, filtros);
            renderTabla(filtradas);
        } else {
            renderTabla(todasLasFacturas); // si no hay filtros, mostrar todo
        }
    });

    // Función que aplica los filtros a la lista completa
    function aplicarFiltros(lista, filtros) {
        return lista.filter(f => {
            const fecha = f.FechaRecepcion?.split('T')[0]; // formato ISO

            const cumpleFecha =
                (!filtros.fechaInicio || fecha >= filtros.fechaInicio) &&
                (!filtros.fechaFin || fecha <= filtros.fechaFin);

            const cumpleEstado =
                !filtros.estado || f.Estado?.toLowerCase() === filtros.estado.toLowerCase();

            const cumpleTipo =
                !filtros.tipo || (f.ENCF && f.ENCF.startsWith("E" + filtros.tipo));

            const search = filtros.search?.toLowerCase() || "";
            const cumpleSearch =
                !search ||
                f.ENCF?.toLowerCase().includes(search) ||
                f.RncComprador?.toLowerCase().includes(search) ||
                f.RazonSocialComprador?.toLowerCase().includes(search);

            return cumpleFecha && cumpleEstado && cumpleTipo && cumpleSearch;
        });
    }

    function renderTabla(filas) {
        table.clear(); // Limpia la tabla

        filas.forEach(f => {
            const fechaFormateada = f.FechaRecepcion
                ? new Date(f.FechaRecepcion).toLocaleDateString('es-DO')
                : 'N/A';

            const row = table.row.add([
                `<button type="button" class="btn btn-sm btn-expand" aria-expanded="false">&gt;</button>`,
                f.ENCF || 'N/A',
                fechaFormateada,
                f.RazonSocialComprador || 'N/A',
                f.RncComprador || 'N/A',
                `<span class="px-2 py-1 rounded ${f.Estado === "Aceptado" ? "text-success" :
                    f.Estado === "Rechazado" ? "text-danger" : "text-secondary"
                }">${f.Estado || 'N/A'}</span>`,
                `<button type="button" class="btn btn-sm btn-outline-black ver-trackid-btn bg-warning"
                data-bs-toggle="modal" data-bs-target="#modalTrackId" data-trackid="${f.TrackId}">
                Ver
            </button>`
            ]).node();

            $(row).attr('data-encf', f.ENCF || '');
        });

        table.draw(); // Redibuja la tabla
    }

    // Al cargar la página, aplicar filtros iniciales según valores por defecto en los inputs
    const filtrosIniciales = obtenerFiltrosActuales();
    const filtradasIniciales = aplicarFiltros(todasLasFacturas, filtrosIniciales);
    renderTabla(filtradasIniciales);


});
