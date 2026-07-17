(function () {
    const toggle = document.querySelector("[data-password-toggle]");

    if (!toggle) {
        return;
    }

    const field = toggle.closest(".auth-password-wrap")?.querySelector("input");

    if (!field) {
        return;
    }

    toggle.addEventListener("click", function () {
        const shouldShow = field.type === "password";

        field.type = shouldShow ? "text" : "password";
        toggle.setAttribute("aria-pressed", shouldShow.toString());
        toggle.setAttribute("aria-label", shouldShow ? "Ocultar senha" : "Mostrar senha");
        field.focus();
    });
})();
