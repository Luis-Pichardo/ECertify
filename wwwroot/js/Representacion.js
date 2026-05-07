document.getElementById('btnDescargarZip').addEventListener('click', function () {
    LoadingOverlay.show();
    window.location.href = '?handler=DescargarZip';
});
