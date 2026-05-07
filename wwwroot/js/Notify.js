function mostrarMensaje(mensaje, tipo = 'success') {
    $.notify(mensaje, {
        className: tipo,
        position: "top right",
        autoHide: true,
        autoHideDelay: 5000,
        clickToHide: true,
        showAnimation: 'fadeIn',
        hideAnimation: 'fadeOut',
        style: 'bootstrap',
        showDuration: 400,
        hideDuration: 400,
        gap: 10, // Espacio entre notificaciones
        arrowShow: false,
        onShow: function () {
            $(this).css('opacity', 1);
        },
        onShown: function () {
            $(this).css('opacity', 1);
        }
    });
}