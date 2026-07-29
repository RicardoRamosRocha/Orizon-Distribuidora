(() => {
const root = document.querySelector("[data-pdf-summer]");

if (root && root.dataset.pdfSummerInitialized !== "true") {
    root.dataset.pdfSummerInitialized = "true";
    const panel = root.querySelector(".pdf-summer-panel");
    const dragHandle = root.querySelector("[data-pdf-summer-drag-handle]");
    const restoreButton = root.querySelector("[data-pdf-summer-restore]");
    const backdrop = root.querySelector(".pdf-summer-backdrop");
    const launcher = root.querySelector("[data-pdf-summer-open]");
    const dropzone = root.querySelector("[data-pdf-dropzone]");
    const pdfInput = root.querySelector("[data-pdf-input]");
    const folderInput = root.querySelector("[data-folder-input]");
    const documentsHost = root.querySelector("[data-pdf-documents]");
    const empty = root.querySelector("[data-pdf-empty]");
    const notice = root.querySelector("[data-pdf-notice]");
    const processButton = root.querySelector("[data-process-pdfs]");
    const processLabel = root.querySelector("[data-process-label]");
    const clearButton = root.querySelector("[data-clear-session]");
    const countLabel = root.querySelector("[data-document-count]");
    const sizeLabel = root.querySelector("[data-session-size]");
    const grandTotal = root.querySelector("[data-grand-total]");
    const validSummary = root.querySelector("[data-valid-summary]");
    const currency = new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" });
    const documents = new Map();
    let sequence = 0;
    let lastFocused = null;
    let pdfJsPromise = null;
    let savedPosition = null;
    let dragState = null;
    let dragFrame = null;
    let launcherPosition = null;
    let launcherDragState = null;
    let launcherDragFrame = null;
    let suppressLauncherClick = false;
    const positionStorageKey = "orizon.pdfSummer.position";
    const launcherPositionStorageKey = "orizon.pdf-calculator.floating-position.v1";
    const viewportMargin = 12;
    const launcherDragThreshold = 5;
    const compactViewport = window.matchMedia("(max-width: 560px)");

    const clamp = (value, minimum, maximum) => Math.min(Math.max(value, minimum), maximum);

    const getSafeArea = () => {
        const styles = getComputedStyle(root);
        return {
            top: parseFloat(styles.paddingTop) || 0,
            right: parseFloat(styles.paddingRight) || 0,
            bottom: parseFloat(styles.paddingBottom) || 0,
            left: parseFloat(styles.paddingLeft) || 0
        };
    };

    const getLauncherBounds = () => {
        const safeArea = getSafeArea();
        const minimumX = Math.max(viewportMargin, safeArea.left + viewportMargin);
        const minimumY = Math.max(viewportMargin, safeArea.top + viewportMargin);
        return {
            minimumX,
            minimumY,
            maximumX: Math.max(minimumX,
                window.innerWidth - launcher.offsetWidth - Math.max(viewportMargin, safeArea.right + viewportMargin)),
            maximumY: Math.max(minimumY,
                window.innerHeight - launcher.offsetHeight - Math.max(viewportMargin, safeArea.bottom + viewportMargin))
        };
    };

    const constrainLauncherPosition = (x, y) => {
        const bounds = getLauncherBounds();
        return {
            x: clamp(x, bounds.minimumX, bounds.maximumX),
            y: clamp(y, bounds.minimumY, bounds.maximumY)
        };
    };

    const setLauncherPosition = position => {
        const constrained = constrainLauncherPosition(position.x, position.y);
        launcher.style.left = `${constrained.x}px`;
        launcher.style.top = `${constrained.y}px`;
        launcher.classList.add("is-positioned");
        return constrained;
    };

    const normalizeLauncherPosition = position => {
        const bounds = getLauncherBounds();
        const width = bounds.maximumX - bounds.minimumX;
        const height = bounds.maximumY - bounds.minimumY;
        return {
            x: width > 0 ? (position.x - bounds.minimumX) / width : 0,
            y: height > 0 ? (position.y - bounds.minimumY) / height : 0
        };
    };

    const denormalizeLauncherPosition = position => {
        const bounds = getLauncherBounds();
        return {
            x: bounds.minimumX + position.x * (bounds.maximumX - bounds.minimumX),
            y: bounds.minimumY + position.y * (bounds.maximumY - bounds.minimumY)
        };
    };

    const readLauncherPosition = () => {
        try {
            const value = JSON.parse(localStorage.getItem(launcherPositionStorageKey));
            if (!value || !Number.isFinite(value.x) || !Number.isFinite(value.y)
                || value.x < 0 || value.x > 1 || value.y < 0 || value.y > 1) return null;
            return { x: value.x, y: value.y };
        } catch {
            return null;
        }
    };

    const saveLauncherPosition = position => {
        launcherPosition = normalizeLauncherPosition(position);
        try {
            localStorage.setItem(launcherPositionStorageKey, JSON.stringify(launcherPosition));
        } catch {
            // A posição continua válida durante a navegação atual.
        }
    };

    const applyLauncherPosition = () => {
        if (launcherPosition) {
            setLauncherPosition(denormalizeLauncherPosition(launcherPosition));
            return;
        }
        const bounds = getLauncherBounds();
        setLauncherPosition({ x: bounds.maximumX, y: bounds.maximumY });
    };

    const moveLauncher = event => {
        if (!launcherDragState || event.pointerId !== launcherDragState.pointerId) return;
        const deltaX = event.clientX - launcherDragState.startX;
        const deltaY = event.clientY - launcherDragState.startY;
        if (!launcherDragState.dragged
            && Math.hypot(deltaX, deltaY) < launcherDragThreshold) return;
        launcherDragState.dragged = true;
        suppressLauncherClick = true;
        launcher.classList.add("is-dragging");
        launcherDragState.nextPosition = constrainLauncherPosition(
            launcherDragState.startLeft + deltaX,
            launcherDragState.startTop + deltaY);
        event.preventDefault();
        if (launcherDragFrame !== null) return;
        launcherDragFrame = requestAnimationFrame(() => {
            if (launcherDragState) setLauncherPosition(launcherDragState.nextPosition);
            launcherDragFrame = null;
        });
    };

    const finishLauncherDrag = event => {
        if (!launcherDragState || event.pointerId !== launcherDragState.pointerId) return;
        if (launcherDragFrame !== null) {
            cancelAnimationFrame(launcherDragFrame);
            launcherDragFrame = null;
        }
        if (launcherDragState.dragged) {
            const finalPosition = setLauncherPosition(launcherDragState.nextPosition);
            saveLauncherPosition(finalPosition);
        }
        launcher.classList.remove("is-dragging");
        launcher.removeEventListener("pointermove", moveLauncher);
        launcher.removeEventListener("pointerup", finishLauncherDrag);
        launcher.removeEventListener("pointercancel", finishLauncherDrag);
        if (launcher.hasPointerCapture(event.pointerId)) launcher.releasePointerCapture(event.pointerId);
        launcherDragState = null;
    };

    const startLauncherDrag = event => {
        if (event.button !== 0 && event.pointerType !== "touch") return;
        const bounds = launcher.getBoundingClientRect();
        suppressLauncherClick = false;
        launcherDragState = {
            pointerId: event.pointerId,
            startX: event.clientX,
            startY: event.clientY,
            startLeft: bounds.left,
            startTop: bounds.top,
            nextPosition: { x: bounds.left, y: bounds.top },
            dragged: false
        };
        launcher.setPointerCapture(event.pointerId);
        launcher.addEventListener("pointermove", moveLauncher);
        launcher.addEventListener("pointerup", finishLauncherDrag);
        launcher.addEventListener("pointercancel", finishLauncherDrag);
    };

    const isFreeMovementEnabled = () =>
        !compactViewport.matches && panel.offsetWidth < window.innerWidth - (viewportMargin * 2);

    const constrainPosition = (x, y) => {
        const width = panel.offsetWidth;
        const height = panel.offsetHeight;
        const maxX = Math.max(viewportMargin, window.innerWidth - width - viewportMargin);
        const maxY = Math.max(viewportMargin, window.innerHeight - height - viewportMargin);
        return {
            x: Math.min(Math.max(x, viewportMargin), maxX),
            y: Math.min(Math.max(y, viewportMargin), maxY)
        };
    };

    const readStoredPosition = () => {
        try {
            const value = JSON.parse(localStorage.getItem(positionStorageKey));
            return value && Number.isFinite(value.x) && Number.isFinite(value.y)
                ? { x: value.x, y: value.y }
                : null;
        } catch {
            return null;
        }
    };

    const setPanelPosition = position => {
        const constrained = constrainPosition(position.x, position.y);
        panel.style.left = `${constrained.x}px`;
        panel.style.top = `${constrained.y}px`;
        panel.classList.add("is-positioned");
        return constrained;
    };

    const useResponsivePosition = () => {
        panel.classList.remove("is-positioned");
        panel.style.removeProperty("left");
        panel.style.removeProperty("top");
    };

    const applySavedPosition = () => {
        if (!isFreeMovementEnabled()) {
            useResponsivePosition();
            return;
        }
        if (savedPosition) savedPosition = setPanelPosition(savedPosition);
        else useResponsivePosition();
    };

    const savePosition = position => {
        savedPosition = position;
        try {
            localStorage.setItem(positionStorageKey, JSON.stringify(position));
        } catch {
            // A posição continua válida durante a navegação atual.
        }
    };

    const restorePosition = () => {
        savedPosition = null;
        try {
            localStorage.removeItem(positionStorageKey);
        } catch {
            // O layout padrão ainda pode ser restaurado nesta página.
        }
        useResponsivePosition();
    };

    const finishDrag = event => {
        if (!dragState || event.pointerId !== dragState.pointerId) return;
        if (dragFrame !== null) {
            cancelAnimationFrame(dragFrame);
            dragFrame = null;
        }
        const finalPosition = setPanelPosition(dragState.nextPosition);
        savePosition(finalPosition);
        panel.classList.remove("is-dragging");
        dragHandle.removeEventListener("pointermove", movePanel);
        dragHandle.removeEventListener("pointerup", finishDrag);
        dragHandle.removeEventListener("pointercancel", finishDrag);
        if (dragHandle.hasPointerCapture(event.pointerId)) {
            dragHandle.releasePointerCapture(event.pointerId);
        }
        dragState = null;
    };

    const movePanel = event => {
        if (!dragState || event.pointerId !== dragState.pointerId) return;
        dragState.nextPosition = constrainPosition(
            dragState.startLeft + event.clientX - dragState.startX,
            dragState.startTop + event.clientY - dragState.startY);
        if (dragFrame !== null) return;
        dragFrame = requestAnimationFrame(() => {
            if (dragState) setPanelPosition(dragState.nextPosition);
            dragFrame = null;
        });
    };

    const startDrag = event => {
        if (event.button !== 0 || !isFreeMovementEnabled()
            || event.target.closest("button, a, input, select, textarea, [contenteditable], [role='button']")) return;
        const bounds = panel.getBoundingClientRect();
        dragState = {
            pointerId: event.pointerId,
            startX: event.clientX,
            startY: event.clientY,
            startLeft: bounds.left,
            startTop: bounds.top,
            nextPosition: { x: bounds.left, y: bounds.top }
        };
        event.preventDefault();
        panel.classList.add("is-dragging");
        dragHandle.setPointerCapture(event.pointerId);
        dragHandle.addEventListener("pointermove", movePanel);
        dragHandle.addEventListener("pointerup", finishDrag);
        dragHandle.addEventListener("pointercancel", finishDrag);
    };

    const escapeHtml = value => String(value)
        .replaceAll("&", "&amp;").replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;").replaceAll('"', "&quot;");

    const formatBytes = bytes => {
        if (bytes < 1024) return `${bytes} B`;
        if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
        return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
    };

    const parseMoney = value => {
        const cleaned = String(value ?? "").replace(/[^\d,.-]/g, "").trim();
        if (!cleaned) return null;
        let normalized = cleaned;
        if (cleaned.includes(",") && cleaned.includes(".")) {
            normalized = cleaned.lastIndexOf(",") > cleaned.lastIndexOf(".")
                ? cleaned.replaceAll(".", "").replace(",", ".")
                : cleaned.replaceAll(",", "");
        } else if (cleaned.includes(",")) {
            normalized = cleaned.replaceAll(".", "").replace(",", ".");
        } else if ((cleaned.match(/\./g) ?? []).length > 1) {
            normalized = cleaned.replaceAll(".", "");
        }
        const number = Number(normalized);
        return Number.isFinite(number) && number >= 0 ? number : null;
    };

    const inputMoney = value => value == null
        ? ""
        : value.toLocaleString("pt-BR", { minimumFractionDigits: 2, maximumFractionDigits: 2 });

    const showNotice = (message, isError = false) => {
        notice.textContent = message;
        notice.classList.toggle("is-error", isError);
        notice.hidden = !message;
    };

    const statusText = document => {
        if (document.status === "processing") return ["Processando…", "", "Extraindo o texto do documento."];
        if (document.status === "ready") return ["Valor identificado", "is-ready", document.hint || "Confira o valor antes de finalizar."];
        if (document.status === "manual") return ["Revisão necessária", "is-warning", document.hint || "Informe o valor total manualmente."];
        if (document.status === "error") return ["Não foi possível ler", "is-error", document.hint || "Informe o valor manualmente."];
        return ["Aguardando processamento", "", "Clique em Processar documentos."];
    };

    const updateTotal = () => {
        const valid = [...documents.values()].filter(item => item.value != null && item.value >= 0);
        const total = valid.reduce((sum, item) => sum + item.value, 0);
        grandTotal.textContent = currency.format(total);
        validSummary.textContent = `${valid.length} ${valid.length === 1 ? "documento incluído" : "documentos incluídos"}`;
    };

    const render = () => {
        const items = [...documents.values()];
        documentsHost.innerHTML = items.map(item => {
            const [label, tone, hint] = statusText(item);
            const path = item.file.webkitRelativePath || item.file.name;
            return `<article class="pdf-document" data-document-id="${item.id}">
                <div class="pdf-document-top">
                    <span class="pdf-document-file-icon" aria-hidden="true">PDF</span>
                    <div class="pdf-document-name">
                        <strong title="${escapeHtml(path)}">${escapeHtml(item.file.name)}</strong>
                        <span>${formatBytes(item.file.size)}${path !== item.file.name ? ` · ${escapeHtml(path)}` : ""}</span>
                    </div>
                    <button class="pdf-summer-icon-button pdf-document-remove" type="button"
                            data-remove-document="${item.id}" aria-label="Remover ${escapeHtml(item.file.name)}" title="Remover">×</button>
                </div>
                <div class="pdf-document-bottom">
                    <div>
                        <span class="pdf-document-status ${tone}">${label}</span>
                        <span class="pdf-document-hint">${escapeHtml(hint)}</span>
                    </div>
                    <label>
                        <span class="pdf-document-value-label">Valor do documento</span>
                        <span class="pdf-document-value-wrap">
                            <span>R$</span>
                            <input class="pdf-document-value" data-document-value="${item.id}"
                                   inputmode="decimal" autocomplete="off" placeholder="0,00"
                                   value="${escapeHtml(inputMoney(item.value))}"
                                   aria-label="Valor de ${escapeHtml(item.file.name)}" />
                        </span>
                    </label>
                </div>
            </article>`;
        }).join("");

        const totalBytes = items.reduce((sum, item) => sum + item.file.size, 0);
        countLabel.textContent = items.length
            ? `${items.length} ${items.length === 1 ? "documento" : "documentos"}`
            : "Nenhum documento";
        sizeLabel.textContent = items.length ? `${formatBytes(totalBytes)} nesta sessão` : "Adicione PDFs para começar.";
        empty.hidden = items.length > 0;
        documentsHost.hidden = items.length === 0;
        processButton.disabled = items.length === 0 || items.some(item => item.status === "processing");
        clearButton.disabled = items.length === 0;
        updateTotal();
    };

    const addFiles = fileList => {
        const files = [...fileList];
        const pdfs = files.filter(file =>
            file.type === "application/pdf" || file.name.toLowerCase().endsWith(".pdf"));
        const rejected = files.length - pdfs.length;
        let added = 0;
        for (const file of pdfs) {
            const path = file.webkitRelativePath || file.name;
            const duplicate = [...documents.values()].some(item =>
                (item.file.webkitRelativePath || item.file.name) === path
                && item.file.size === file.size && item.file.lastModified === file.lastModified);
            if (duplicate) continue;
            const id = `pdf-${++sequence}`;
            documents.set(id, { id, file, status: "pending", value: null, hint: "" });
            added++;
        }
        if (rejected) showNotice(`${rejected} arquivo(s) ignorado(s). Apenas documentos PDF são aceitos.`);
        else if (!added && pdfs.length) showNotice("Esses PDFs já estão na sessão.");
        else showNotice("");
        pdfInput.value = "";
        folderInput.value = "";
        render();
    };

    const loadPdfJs = () => {
        if (!pdfJsPromise) {
            pdfJsPromise = import("https://cdn.jsdelivr.net/npm/pdfjs-dist@4.10.38/build/pdf.min.mjs")
                .then(pdfjs => {
                    pdfjs.GlobalWorkerOptions.workerSrc =
                        "https://cdn.jsdelivr.net/npm/pdfjs-dist@4.10.38/build/pdf.worker.min.mjs";
                    return pdfjs;
                });
        }
        return pdfJsPromise;
    };

    const extractText = async (file, pdfjs) => {
        const data = new Uint8Array(await file.arrayBuffer());
        const pdf = await pdfjs.getDocument({ data }).promise;
        const pages = [];
        for (let pageNumber = 1; pageNumber <= pdf.numPages; pageNumber++) {
            const page = await pdf.getPage(pageNumber);
            const content = await page.getTextContent();
            pages.push(content.items.map(item => item.str).join(" "));
        }
        return pages.join("\n");
    };

    const findTotal = text => {
        const normalized = text.replace(/\s+/g, " ");
        const labeledPatterns = [
            /(?:valor\s+total\s+da\s+nota|valor\s+total\s+da\s+nf(?:-?e)?|total\s+da\s+nota|valor\s+total|total\s+geral|total\s+do\s+documento)\s*:?\s*(?:r\$\s*)?([\d.]+,\d{2})/gi,
            /(?:total)\s*:?\s*(?:r\$\s*)?([\d.]+,\d{2})/gi
        ];
        for (const pattern of labeledPatterns) {
            const matches = [...normalized.matchAll(pattern)];
            if (matches.length) {
                const parsed = parseMoney(matches.at(-1)[1]);
                if (parsed != null) return { value: parsed, confidence: "label" };
            }
        }
        const currencyValues = [...normalized.matchAll(/r\$\s*([\d.]+,\d{2})/gi)]
            .map(match => parseMoney(match[1])).filter(value => value != null);
        if (currencyValues.length) return { value: Math.max(...currencyValues), confidence: "largest" };
        return null;
    };

    const processDocuments = async () => {
        const pending = [...documents.values()].filter(item => item.status !== "processing");
        if (!pending.length) return;
        pending.forEach(item => { item.status = "processing"; item.hint = ""; });
        processLabel.textContent = "Processando…";
        showNotice("");
        render();

        let pdfjs;
        try {
            pdfjs = await loadPdfJs();
        } catch {
            pending.forEach(item => {
                item.status = "error";
                item.hint = "O leitor de PDFs não pôde ser carregado. Confira sua conexão e informe o valor.";
            });
            showNotice("Não foi possível carregar o leitor de PDFs. Os valores ainda podem ser preenchidos manualmente.", true);
            processLabel.textContent = "Processar novamente";
            render();
            return;
        }

        for (const item of pending) {
            try {
                const text = await extractText(item.file, pdfjs);
                const result = findTotal(text);
                if (result) {
                    item.value = result.value;
                    item.status = result.confidence === "label" ? "ready" : "manual";
                    item.hint = result.confidence === "label"
                        ? "Total localizado no texto. Confirme antes de usar."
                        : "Maior valor monetário encontrado; confirme o total.";
                } else {
                    item.status = "manual";
                    item.hint = text.trim()
                        ? "Nenhum total conclusivo foi encontrado. Informe o valor."
                        : "PDF sem camada de texto (possivelmente digitalizado). Informe o valor.";
                }
            } catch {
                item.status = "error";
                item.hint = "Documento protegido, inválido ou sem texto legível. Informe o valor.";
            }
            render();
        }
        processLabel.textContent = "Processar novamente";
    };

    const openPanel = () => {
        lastFocused = document.activeElement;
        panel.hidden = false;
        backdrop.hidden = false;
        launcher.setAttribute("aria-expanded", "true");
        document.body.style.overflow = "hidden";
        applySavedPosition();
        panel.querySelector(".pdf-summer-icon-button")?.focus();
    };

    const closePanel = () => {
        panel.hidden = true;
        backdrop.hidden = true;
        launcher.setAttribute("aria-expanded", "false");
        document.body.style.overflow = "";
        lastFocused?.focus();
    };

    const handleViewportChange = () => {
        applyLauncherPosition();
        if (!panel.hidden) applySavedPosition();
    };

    launcher.addEventListener("pointerdown", startLauncherDrag);
    launcher.addEventListener("click", event => {
        if (suppressLauncherClick) {
            event.preventDefault();
            event.stopPropagation();
            suppressLauncherClick = false;
            return;
        }
        openPanel();
    });
    document.querySelectorAll("[data-pdf-summer-open]").forEach(trigger => {
        if (trigger !== launcher) trigger.addEventListener("click", openPanel);
    });
    dragHandle.addEventListener("pointerdown", startDrag);
    restoreButton.addEventListener("click", restorePosition);
    window.addEventListener("resize", handleViewportChange);
    window.addEventListener("orientationchange", handleViewportChange);
    root.querySelectorAll("[data-pdf-summer-close]").forEach(button => button.addEventListener("click", closePanel));
    root.querySelector("[data-select-pdfs]").addEventListener("click", event => { event.stopPropagation(); pdfInput.click(); });
    root.querySelector("[data-select-folder]").addEventListener("click", event => { event.stopPropagation(); folderInput.click(); });
    pdfInput.addEventListener("change", () => addFiles(pdfInput.files));
    folderInput.addEventListener("change", () => addFiles(folderInput.files));
    dropzone.addEventListener("click", event => {
        if (!event.target.closest("button")) pdfInput.click();
    });
    dropzone.addEventListener("keydown", event => {
        if (event.key === "Enter" || event.key === " ") { event.preventDefault(); pdfInput.click(); }
    });
    ["dragenter", "dragover"].forEach(type => dropzone.addEventListener(type, event => {
        event.preventDefault();
        dropzone.classList.add("is-dragging");
    }));
    ["dragleave", "drop"].forEach(type => dropzone.addEventListener(type, event => {
        event.preventDefault();
        dropzone.classList.remove("is-dragging");
    }));
    dropzone.addEventListener("drop", event => addFiles(event.dataTransfer.files));
    processButton.addEventListener("click", processDocuments);
    clearButton.addEventListener("click", () => {
        documents.clear();
        showNotice("");
        processLabel.textContent = "Processar documentos";
        render();
    });
    documentsHost.addEventListener("click", event => {
        const button = event.target.closest("[data-remove-document]");
        if (!button) return;
        documents.delete(button.dataset.removeDocument);
        render();
    });
    documentsHost.addEventListener("input", event => {
        const input = event.target.closest("[data-document-value]");
        if (!input) return;
        const item = documents.get(input.dataset.documentValue);
        if (!item) return;
        item.value = parseMoney(input.value);
        if (item.value != null && item.status !== "ready") {
            item.status = "manual";
            item.hint = "Valor informado manualmente.";
        }
        updateTotal();
    });
    documentsHost.addEventListener("blur", event => {
        const input = event.target.closest("[data-document-value]");
        if (!input) return;
        const item = documents.get(input.dataset.documentValue);
        input.value = inputMoney(item?.value);
    }, true);
    document.addEventListener("keydown", event => {
        if (event.key === "Escape" && !panel.hidden) closePanel();
        if (event.key === "Tab" && !panel.hidden) {
            const focusable = [...panel.querySelectorAll("button:not(:disabled), input:not(:disabled), [tabindex='0']")];
            if (!focusable.length) return;
            const first = focusable[0];
            const last = focusable.at(-1);
            if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); }
            else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
        }
    });

    savedPosition = readStoredPosition();
    launcherPosition = readLauncherPosition();
    applyLauncherPosition();
    render();
}
})();
