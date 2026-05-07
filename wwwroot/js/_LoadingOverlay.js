(function () {
    function getEl() { return document.getElementById('globalLoader'); }

    window.showLoader = function (text) {
        var el = getEl();
        if (!el) return;
        if (text) {
            var t = el.querySelector('.gl-text');
            if (t) t.textContent = text;
        }
        el.classList.add('active');
    };

    window.hideLoader = function () {
        var el = getEl();
        if (el) el.classList.remove('active');
    };

    window.addEventListener('load', function () { hideLoader(); });
})();
