function verificarSesion() {
    if (document.cookie.indexOf("SesionActiva=true") === -1) {
        window.location.replace("/Account/Index");
    }
}

verificarSesion();

window.addEventListener("pageshow", function (event) {
    verificarSesion();
});