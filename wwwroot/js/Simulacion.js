document.addEventListener("DOMContentLoaded", function () {
    const jsonEditor = document.getElementById('jsonEditor');
    const jsonHiddenInput = document.getElementById('JsonGeneradoHidden');
    if (jsonEditor) {
        jsonEditor.addEventListener('blur', guardarEdicion);
    }

    // ✅ Mostrar/ocultar campos según tipo e-CF
    const tipoSelect = document.getElementById('TipoECF');
    const modificacionWrapper = document.getElementById('modificacionWrapper');
    const clienteRNC = document.getElementById('ClienteRNC');
    const clienteNombre = document.getElementById('ClienteNombre');
    const productoItbis = document.getElementById('ProductoItbis');
    const productoTipo = document.getElementById("ProductoTipo");

    if (!tipoSelect) return; // Si no existe el select, no hacer nada

    tipoSelect.addEventListener('change', function () {
        const tipo = tipoSelect.value;

        // Mostrar u ocultar campos de modificación según tipo
        if (tipo === "33" || tipo === "34") {
            modificacionWrapper.classList.remove('d-none');
        } else {
            modificacionWrapper.classList.add('d-none');
        }

        // Mostrar alertas específicas para cada tipo
        if (tipo === "33") {
            document.getElementById('RazonModificacion').value = "Corrección de datos.";
            alert(
                "Tipo E33 seleccionado:\n\n" +
                "Esta nota de débito simula una corrección de datos. Solo debes modificar la cantidad del producto y llenar los campos 'NCF Modificado' y 'Razón de modificación'.\n\n" +
                "Todos los demás datos deben mantenerse exactamente igual a la factura original."
            );
        } else if (tipo === "34") {
            document.getElementById('RazonModificacion').value = "Error en los datos.";
            alert(
                "Tipo E34 seleccionado:\n\n" +
                "Esta nota de crédito simula un error en los datos. Debes establecer todos los montos a cero y completar los campos 'NCF Modificado' y 'Razón de modificación'. Todos los campos restantes llenarlo iguales"
            );
        }

        if (tipo === "41") {
            productoItbis.disabled = true;
            productoItbis.value = "18";
            alert("Los comprobantes tipo E41 requieren el ITBIS por obligación para poder calcular las retenciones durante la simulación de este tipo de facturas.");
        }
        else {
            productoItbis.disabled = false;
        }

        // Deshabilitar RNC y Razón Social si el tipo es 43
        if (tipo === "43") {
            clienteRNC.disabled = true;
            clienteNombre.disabled = true;
            clienteRNC.removeAttribute('required');
            clienteNombre.removeAttribute('required');
            clienteRNC.value = '';
            clienteNombre.value = '';
            alert("Los comprobantes tipo E43 son de Gastos Menores, por lo que no es necesario ingresar RNC o Nombre del cliente. Estos podrian ser (Comidas para el personal, Combustible, entre otros...)");
            

        } else {
            clienteRNC.disabled = false;
            clienteNombre.disabled = false;
            clienteRNC.setAttribute('required', 'required');
            clienteNombre.setAttribute('required', 'required');
            
        }

        // Control de ITBIS
        const tiposSinItbis = ["43", "44", "46", "47"];
        const deshabilitarItbis = tiposSinItbis.includes(tipo);

        if (productoItbis) {
            productoItbis.disabled = deshabilitarItbis;
            productoItbis.value = deshabilitarItbis
                ? (tipo === "46" ? "0" : "")
                : "18"; // Puedes cambiar "18" por el valor por defecto si es necesario
        }

        if (tipo === "47") {
            
            if (productoTipo) {
                productoTipo.value = "S";
                productoTipo.disabled = true;   
            }

            document.getElementById("ProductoTipoHidden").value = "S";

        } else {

            if (productoTipo) {
                productoTipo.disabled = false;
            }

            document.getElementById("ProductoTipoHidden").value = productoTipo.value;
        }
    });

});

function mostrarLoading(mostrar) {
    document.getElementById('loadingOverlay').style.display = mostrar ? 'flex' : 'none';
    document.getElementById('btnGenerar').disabled = mostrar;
}

function descargarJson() {
    try {
        const jsonContent = document.getElementById('jsonContent');
        const jsonEditor = document.getElementById('jsonEditor');

        const jsonStr = !jsonEditor.classList.contains('d-none')
            ? jsonEditor.value
            : jsonContent.textContent;

        const blob = new Blob([jsonStr], { type: 'application/json' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `factura-${new Date().toISOString().slice(0, 10)}.json`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);

        agregarLog('✅ JSON descargado correctamente');
    } catch (error) {
        console.error('Error al descargar JSON:', error);
        agregarLog(`❌ Error al descargar JSON: ${error.message}`);
    }
}


function editarJson() {
    const jsonContent = document.getElementById('jsonContent');
    const jsonEditor = document.getElementById('jsonEditor');

    jsonContent.classList.add('d-none');
    jsonEditor.classList.remove('d-none');
    limpiarLog();
    agregarLog('Modo edición activado - Puede modificar el JSON directamente');
}

function guardarEdicion() {
    const jsonContent = document.getElementById('jsonContent');
    const jsonEditor = document.getElementById('jsonEditor');
    const jsonHiddenInput = document.getElementById('JsonGeneradoHidden');

    try {
        // Validar que sea JSON válido
        JSON.parse(jsonEditor.value);

        // Actualizar contenido
        jsonContent.textContent = jsonEditor.value;
        jsonHiddenInput.value = jsonEditor.value;

        // Cambiar vista
        jsonEditor.classList.add('d-none');
        jsonContent.classList.remove('d-none');
        limpiarLog();
        agregarLog('✅ JSON guardado y actualizado correctamente');

    } catch (e) {
        alert('Error: JSON inválido. Corrija antes de guardar.');
    }
}


function agregarLog(mensaje) {
    const consola = document.getElementById('logConsole_ECF');
    const time = new Date().toLocaleTimeString();
    consola.innerHTML += `<div>[${time}] ${mensaje}</div>`;
    consola.scrollTop = consola.scrollHeight;
}

function limpiarLog() {
    document.getElementById('logConsole_ECF').innerHTML = '';
}
