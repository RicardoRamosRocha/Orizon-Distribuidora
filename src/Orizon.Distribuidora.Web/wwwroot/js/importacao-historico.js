(() => {
    const form = document.querySelector("#historyForm");
    const metadata = document.querySelector("#historyMetadata");
    if (!form || !metadata) return;
    let timer;
    document.querySelector("#instantSearch")?.addEventListener("input", () => {
        clearTimeout(timer);
        timer = setTimeout(() => form.requestSubmit(), 450);
    });
    document.querySelectorAll(".page-link").forEach(link => link.addEventListener("click", () => {
        form.querySelector("[name=Pagina]").value = link.dataset.page;
        form.requestSubmit();
    }));
    document.querySelector("#rollbackId")?.nextElementSibling?.addEventListener("click", () => {
        const id = document.querySelector("#rollbackId").value.trim();
        if (id) window.location.assign(`${metadata.dataset.rollbackUrl}/${encodeURIComponent(id)}`);
    });
    const dialog = document.querySelector("#deleteHistoryDialog");
    let pendingForm;
    document.querySelectorAll('form[action*="/Excluir/"]').forEach(deleteForm => {
        deleteForm.onsubmit = null;
        deleteForm.addEventListener("submit", event => {
            if (deleteForm.dataset.confirmed === "true") return;
            event.preventDefault();
            pendingForm = deleteForm;
            dialog.showModal();
        });
    });
    dialog?.addEventListener("close", () => {
        if (dialog.returnValue === "confirm" && pendingForm) {
            pendingForm.dataset.confirmed = "true";
            pendingForm.requestSubmit();
        }
        pendingForm = null;
    });
    if (!window.Chart || !metadata.dataset.dashboardUrl) return;
    const styles = getComputedStyle(document.documentElement);
    const token = (name, fallback) => styles.getPropertyValue(name).trim() || fallback;
    fetch(metadata.dataset.dashboardUrl, { headers: { Accept: "application/json" } })
        .then(response => response.ok ? response.json() : Promise.reject())
        .then(data => {
            const draw = (selector, points, color, type = "bar") => {
                const canvas = document.querySelector(selector);
                if (!canvas) return;
                new Chart(canvas, { type, data: { labels: points.map(x => x.rotulo), datasets: [{ data: points.map(x => x.valor), backgroundColor: color, borderColor: color }] }, options: { responsive: true, maintainAspectRatio: false, plugins: { legend: { display: false } } } });
            };
            draw("#chartDay", data.importacoesPorDia, token("--orizon-color-primary", "currentColor"), "line");
            draw("#chartProducts", data.produtosPorMes, token("--orizon-color-success", "currentColor"));
            draw("#chartFailures", data.falhasPorCategoria, token("--orizon-color-danger", "currentColor"));
            draw("#chartTime", data.tempoMedioPorMes, token("--orizon-color-accent", token("--orizon-color-primary", "currentColor")), "line");
        });
})();
