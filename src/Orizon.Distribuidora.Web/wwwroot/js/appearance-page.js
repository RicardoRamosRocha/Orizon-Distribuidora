(() => {
    "use strict";
    const stateLabel = document.querySelector("[data-appearance-save-state]");
    const companyForm = document.querySelector("[data-company-form]");
    const token = document.querySelector('meta[name="request-verification-token"]')?.content || "";
    const headers = { "Content-Type": "application/json", "RequestVerificationToken": token };
    OrizonAppearance.configure({ appearance: "/api/appearance", preferences: "/api/preferences", theme: "/api/theme", companyTheme: "/api/company/theme" });
    window.addEventListener("orizon:appearancechange", () => { if (stateLabel) stateLabel.textContent = "Salvando preferencias..."; });
    window.addEventListener("orizon:appearancesaved", () => { if (stateLabel) stateLabel.textContent = "Preferencias sincronizadas"; });
    window.addEventListener("orizon:appearanceerror", () => { if (stateLabel) stateLabel.textContent = "Preferencia salva neste dispositivo; sincronizacao pendente"; });
    companyForm?.addEventListener("submit", async event => {
        event.preventDefault(); const button = companyForm.querySelector("button[type=submit]"); button.setAttribute("aria-busy", "true"); button.disabled = true;
        try {
            const payload = { ...OrizonAppearance.get(), loginTitle: companyForm.elements.loginTitle.value };
            for (const input of companyForm.querySelectorAll("[data-company-asset]")) {
                if (!input.files?.length) continue;
                const data = new FormData(); data.append("file", input.files[0]);
                const upload = await fetch("/api/company/assets", { method: "POST", headers: { "RequestVerificationToken": token }, body: data });
                if (!upload.ok) throw new Error("Falha ao enviar imagem.");
                payload[input.dataset.companyAsset] = (await upload.json()).path;
            }
            const response = await fetch("/api/company/theme", { method: "POST", headers, body: JSON.stringify(payload) });
            if (!response.ok) throw new Error("Falha ao salvar configuracao da empresa.");
            stateLabel.textContent = "Padrao da empresa atualizado";
        } catch (error) { stateLabel.textContent = error.message; }
        finally { button.removeAttribute("aria-busy"); button.disabled = false; }
    });
})();
