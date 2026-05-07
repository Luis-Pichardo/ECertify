// Muestra el nombre del archivo al seleccionarlo
function mostrarNombreArchivo(inputId, labelId) {
    const input = document.getElementById(inputId);
    const label = document.getElementById(labelId);
    if (input && label) {
        input.addEventListener('change', function (e) {
            const fileName = e.target.files[0]?.name || 'Ningún archivo seleccionado';
            label.textContent = fileName;
        });
    }
}

mostrarNombreArchivo('certificado', 'fileNameCertificado');
mostrarNombreArchivo('logoFile', 'fileNameLogo');

// Mostrar/Ocultar contraseña
document.querySelectorAll('.password-toggle').forEach(function (toggle) {
    toggle.addEventListener('click', function () {
        const targetId = this.getAttribute('data-target');
        const input = document.getElementById(targetId);
        if (input) {
            const isPassword = input.type === 'password';
            input.type = isPassword ? 'text' : 'password';
            this.classList.toggle('fa-eye-slash', isPassword);
        }
    });
});

// Cargar los datos de provincias y municipios
const provinciasConMunicipios = window.provinciasConMunicipios;

function cargarMunicipios(provinciaId) {
    const municipioSelect = document.getElementById('municipioSelect');
    municipioSelect.innerHTML = '';

    if (!provinciaId) {
        municipioSelect.innerHTML = '<option value="">Primero seleccione una provincia</option>';
        municipioSelect.disabled = true;
        return;
    }

    const provincia = provinciasConMunicipios.find(p => p.id == provinciaId);

    if (provincia && provincia.municipios && provincia.municipios.length > 0) {
        municipioSelect.disabled = false;
        municipioSelect.innerHTML = '<option value="">Seleccione un municipio</option>';

        // Ordenar municipios alfabéticamente
        provincia.municipios.sort((a, b) => a.descripcion.localeCompare(b.descripcion));

        provincia.municipios.forEach(m => {
            const option = document.createElement('option');
            option.value = m.id;
            option.textContent = m.descripcion;
            municipioSelect.appendChild(option);
        });
    } else {
        municipioSelect.disabled = true;
        municipioSelect.innerHTML = '<option value="">No hay municipios disponibles</option>';
    }
}

$(document).ready(function () {
    // Configurar el evento change para provincia
    $('#provinciaSelect').change(function () {
        const provinciaId = $(this).val();
        cargarMunicipios(provinciaId);
    });

    // Precargar municipios si ya hay una provincia seleccionada
    const provinciaInicial = $('#provinciaSelect').val();
    if (provinciaInicial) {
        cargarMunicipios(provinciaInicial);

        // Establecer el municipio si ya hay uno seleccionado
        const municipioId = window.municipioIdInicial;
        if (municipioId) {
            setTimeout(() => {
                $('#municipioSelect').val(municipioId);
            }, 100);
        }
    }

    // Configurar drag and drop para archivos
    setupFileUpload('logoUpload', 'logoFile', 'fileNameLogo');
    setupFileUpload('certificadoUpload', 'certificado', 'fileNameCertificado');
});

// Función para drag and drop de archivos
function setupFileUpload(uploadDivId, inputFileId, fileNameDivId) {
    const uploadDiv = document.getElementById(uploadDivId);
    const inputFile = document.getElementById(inputFileId);
    const fileNameDiv = document.getElementById(fileNameDivId);

    uploadDiv.addEventListener('click', function (e) {
        if (e.target.tagName.toLowerCase() !== 'input') {
            inputFile.click();
        }
    });

    inputFile.addEventListener('change', function (e) {
        const fileName = e.target.files[0]?.name || 'Ningún archivo seleccionado';
        fileNameDiv.textContent = fileName;
    });

    uploadDiv.addEventListener('dragover', function (e) {
        e.preventDefault();
        uploadDiv.classList.add('drag-over');
    });

    uploadDiv.addEventListener('dragleave', function (e) {
        e.preventDefault();
        uploadDiv.classList.remove('drag-over');
    });

    uploadDiv.addEventListener('drop', function (e) {
        e.preventDefault();
        uploadDiv.classList.remove('drag-over');

        if (e.dataTransfer.files.length > 0) {
            inputFile.files = e.dataTransfer.files;
            const fileName = inputFile.files[0]?.name || 'Ningún archivo seleccionado';
            fileNameDiv.textContent = fileName;
        }
    });
}

document.querySelector("form").addEventListener("submit", function () {
    const btn = document.getElementById("btnCrearEmpresa");
    btn.disabled = true;
    btn.innerHTML = `
                    <span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>
                    Creando...
                `;
});