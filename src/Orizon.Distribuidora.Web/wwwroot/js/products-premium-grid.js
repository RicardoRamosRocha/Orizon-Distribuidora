(function () {
  const root = document.querySelector("[data-products-grid]");
  if (!root) return;

  const table = root.querySelector(".products-grid");
  const tbody = table.querySelector("tbody");
  const scroll = root.querySelector(".products-grid-scroll");
  const form = root.querySelector("[data-server-filter-form]");
  const search = form.querySelector('[name="Search"]');
  const token = root.querySelector('input[name="__RequestVerificationToken"]')?.value || "";
  const storageKey = "orizon.products.grid.v4";
  const defaultState = { hiddenColumns: [], widths: {}, order: [], pinned: ["code", "name"] };
  let state = loadState();
  let allFiltered = false;
  let lastSelected = -1;
  let loading = false;
  let hasMore = Number(root.dataset.page) * Number(form.elements.PageSize.value) < Number(root.dataset.totalRecords);
  let nextPage = Number(root.dataset.page) + 1;

  hydrateServerState();
  applyColumns();
  updateSelection();

  let debounce;
  search.addEventListener("input", function () {
    clearTimeout(debounce);
    debounce = setTimeout(function () { form.elements.Page.value = 1; form.submit(); }, 350);
  });

  root.addEventListener("change", function (event) {
    if (event.target.matches("[data-column-toggle]")) {
      setColumnVisibility(event.target.dataset.columnToggle, event.target.checked);
      persistState();
    }
    if (event.target.matches("[data-select-all]")) selectPage(event.target.checked);
  });

  root.addEventListener("click", function (event) {
    const toggle = event.target.closest("[data-column-menu-toggle]");
    if (toggle) {
      const menu = root.querySelector("[data-column-menu]");
      menu.hidden = !menu.hidden;
      toggle.setAttribute("aria-expanded", (!menu.hidden).toString());
      return;
    }
    const select = event.target.closest("[data-row-select]");
    if (select) {
      const rows = currentRows();
      const index = rows.indexOf(select.closest("[data-row]"));
      if (event.shiftKey && lastSelected >= 0) {
        for (let i = Math.min(index, lastSelected); i <= Math.max(index, lastSelected); i++) rows[i].querySelector("[data-row-select]").checked = select.checked;
      }
      lastSelected = index;
      allFiltered = false;
      updateSelection();
      return;
    }
    if (event.target.closest("[data-select-filtered]")) {
      allFiltered = true;
      selectPage(true);
      updateSelection();
      toast(`${root.dataset.totalRecords} produtos filtrados selecionados.`, "saved");
      return;
    }
    const bulk = event.target.closest("[data-bulk-operation]");
    if (bulk) runBulk(bulk.dataset.bulkOperation);
  });

  root.addEventListener("dblclick", function (event) {
    const cell = event.target.closest("[data-edit]");
    if (cell) beginEdit(cell);
  });

  table.addEventListener("keydown", async function (event) {
    const cell = event.target.closest("[data-edit]");
    if (!cell || event.target.matches("input")) return;
    const cells = editableCells();
    const index = cells.indexOf(cell);
    if (event.key === "Enter") { event.preventDefault(); beginEdit(cell); }
    else if (event.key === "Tab") { event.preventDefault(); cells[index + (event.shiftKey ? -1 : 1)]?.focus(); }
    else if (event.key === "ArrowRight") cells[index + 1]?.focus();
    else if (event.key === "ArrowLeft") cells[index - 1]?.focus();
    else if (event.key === "ArrowDown") cell.parentElement.nextElementSibling?.querySelector(`[data-edit="${cell.dataset.edit}"]`)?.focus();
    else if (event.key === "ArrowUp") cell.parentElement.previousElementSibling?.querySelector(`[data-edit="${cell.dataset.edit}"]`)?.focus();
    else if (event.key === "Home") editableCells()[0]?.focus();
    else if (event.key === "End") editableCells().at(-1)?.focus();
    else if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "c") await copySelection(cell);
    else if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "v") await pasteAt(cell);
    else if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "d") await fillDown(cell);
    else if (event.key === "Delete") { cell.dataset.value = ""; await saveCell(cell, ""); }
    cell.scrollIntoView({ block: "nearest", inline: "nearest" });
  });

  scroll.addEventListener("scroll", function () {
    if (hasMore && !loading && scroll.scrollTop + scroll.clientHeight >= scroll.scrollHeight - 240) loadMore();
  });

  root.querySelectorAll("[data-resizer]").forEach(handle => handle.addEventListener("pointerdown", startResize));

  function currentRows() { return Array.from(tbody.querySelectorAll("[data-row]")); }
  function editableCells() { return Array.from(tbody.querySelectorAll("[data-edit]")); }
  function loadState() { try { return { ...defaultState, ...JSON.parse(localStorage.getItem(storageKey) || "{}") }; } catch { return { ...defaultState }; } }
  function persistState() {
    localStorage.setItem(storageKey, JSON.stringify(state));
    fetch(root.dataset.preferenceUrl, { method: "PUT", headers: jsonHeaders(), body: JSON.stringify({ stateJson: JSON.stringify(state) }) }).catch(() => {});
  }
  async function hydrateServerState() {
    try {
      const response = await fetch(root.dataset.preferenceUrl);
      const data = await response.json();
      if (data.stateJson) { state = { ...defaultState, ...JSON.parse(data.stateJson) }; localStorage.setItem(storageKey, JSON.stringify(state)); applyColumns(); }
    } catch { /* local preference remains available offline */ }
  }
  function jsonHeaders() { return { "Content-Type": "application/json", "RequestVerificationToken": token }; }
  function applyColumns() {
    Object.entries(state.widths || {}).forEach(([key, width]) => setColumnWidth(key, width));
    root.querySelectorAll("[data-column-toggle]").forEach(toggle => {
      const visible = !state.hiddenColumns.includes(toggle.dataset.columnToggle);
      toggle.checked = visible; setColumnVisibility(toggle.dataset.columnToggle, visible, false);
    });
  }
  function setColumnVisibility(column, visible, updateState = true) {
    table.querySelectorAll(`[data-column="${column}"], [data-col="${column}"]`).forEach(element => element.hidden = !visible);
    if (updateState) {
      state.hiddenColumns = state.hiddenColumns.filter(x => x !== column);
      if (!visible) state.hiddenColumns.push(column);
    }
  }
  function selectPage(checked) { currentRows().forEach(row => row.querySelector("[data-row-select]").checked = checked); if (!checked) allFiltered = false; updateSelection(); }
  function selectedIds() { return currentRows().filter(row => row.querySelector("[data-row-select]").checked).map(row => row.dataset.id); }
  function updateSelection() {
    const ids = selectedIds();
    root.querySelector("[data-selected-count]").textContent = allFiltered ? root.dataset.totalRecords : ids.length;
    root.querySelector("[data-bulkbar]").hidden = ids.length === 0;
    currentRows().forEach(row => row.classList.toggle("is-selected", row.querySelector("[data-row-select]").checked));
  }
  async function runBulk(operation) {
    let value = null;
    if (operation === "price-percent") value = prompt("Percentual de ajuste (use negativo para reduzir):", "0");
    if (value === null && operation === "price-percent") return;
    if (operation === "delete" && !confirm("Excluir logicamente os produtos selecionados?")) return;
    const response = await fetch(root.dataset.bulkUrl, { method: "POST", headers: jsonHeaders(), body: JSON.stringify({ ids: selectedIds(), allFiltered, filter: Object.fromEntries(new FormData(form)), operation, value }) });
    const data = await response.json();
    if (!response.ok) return toast(data.message || "Falha na operacao.", "error");
    toast(`${data.affected} produto(s) alterado(s).`, "saved");
    setTimeout(() => location.reload(), 500);
  }
  function beginEdit(cell) {
    if (cell.querySelector("input")) return;
    const old = cell.dataset.value || "";
    const input = document.createElement("input"); input.value = old; input.className = "products-inline-input";
    cell.textContent = ""; cell.appendChild(input); cell.classList.add("is-pending"); input.focus(); input.select();
    input.addEventListener("keydown", async event => {
      if (event.key === "Escape") { cell.textContent = displayValue(cell.dataset.edit, old); cell.classList.remove("is-pending"); cell.focus(); }
      if (event.key === "Enter" || event.key === "Tab") { event.preventDefault(); await saveCell(cell, input.value); if (event.key === "Tab") moveAfter(cell, event.shiftKey ? -1 : 1); }
    });
    input.addEventListener("blur", () => { if (cell.contains(input)) saveCell(cell, input.value); }, { once: true });
  }
  async function saveCell(cell, value, retry = 0) {
    const row = cell.closest("[data-row]");
    cell.classList.add("is-saving"); cell.textContent = "Salvando...";
    try {
      const response = await fetch(root.dataset.inlineUrl, { method: "POST", headers: jsonHeaders(), body: JSON.stringify({ id: row.dataset.id, field: cell.dataset.edit, value }) });
      const data = await response.json();
      if (!response.ok) throw new Error(data.message || "Valor invalido.");
      cell.dataset.value = value; cell.textContent = displayValue(cell.dataset.edit, value);
      cell.classList.remove("is-pending", "is-saving", "is-error"); cell.classList.add("is-saved"); row.classList.add("is-changed");
      setTimeout(() => cell.classList.remove("is-saved"), 1500);
    } catch (error) {
      if (retry < 2) { setTimeout(() => saveCell(cell, value, retry + 1), 300 * (retry + 1)); return; }
      cell.textContent = displayValue(cell.dataset.edit, cell.dataset.value || ""); cell.classList.remove("is-saving"); cell.classList.add("is-error"); toast(error.message, "error");
    }
  }
  function displayValue(field, value) { if (!value) return "-"; if (field === "cost" || field === "price") return Number(value).toLocaleString("pt-BR", { style: "currency", currency: "BRL" }); return value; }
  function moveAfter(cell, offset) { const cells = editableCells(); cells[cells.indexOf(cell) + offset]?.focus(); }
  async function copySelection(cell) { await navigator.clipboard.writeText(cell.dataset.value || cell.textContent.trim()); toast("Copiado.", "saved"); }
  async function pasteAt(cell) {
    const matrix = (await navigator.clipboard.readText()).replace(/\r/g, "").split("\n").filter(Boolean).map(line => line.split("\t"));
    const rows = currentRows(), startRow = rows.indexOf(cell.closest("tr")), columns = Array.from(rows[startRow].querySelectorAll("[data-edit]")), startCol = columns.indexOf(cell);
    for (let r = 0; r < matrix.length; r++) for (let c = 0; c < matrix[r].length; c++) {
      const target = rows[startRow + r]?.querySelectorAll("[data-edit]")[startCol + c];
      if (target) await saveCell(target, matrix[r][c].trim());
    }
  }
  async function fillDown(cell) { const next = cell.closest("tr").nextElementSibling?.querySelector(`[data-edit="${cell.dataset.edit}"]`); if (next) await saveCell(next, cell.dataset.value || ""); }
  async function loadMore() {
    loading = true; root.classList.add("is-loading-more");
    const params = new URLSearchParams(new FormData(form)); params.set("Page", nextPage);
    try {
      const response = await fetch(`${root.dataset.gridUrl}?${params}`), data = await response.json();
      data.items.forEach(item => tbody.insertAdjacentHTML("beforeend", rowHtml(item)));
      hasMore = data.hasMore; nextPage++; applyColumns();
    } finally { loading = false; root.classList.remove("is-loading-more"); }
  }
  function rowHtml(item) {
    const type = { 1: "Proprio", 2: "Terceiro", 3: "Sob encomenda", 4: "Servico" }[item.productType] || "Produto";
    return `<tr data-row data-id="${item.id}"><td class="products-select-cell products-sticky-select"><input type="checkbox" data-row-select></td><td data-column="code" class="products-code products-sticky-col">${escapeHtml(item.internalCode)}</td><td data-column="name" class="products-name products-sticky-col"><span>${escapeHtml(item.name)}</span></td><td data-column="status"><span class="products-badge ${item.isActive ? "is-active" : "is-muted"}">${item.isActive ? "Ativo" : "Inativo"}</span></td><td data-column="type">${type}</td><td data-column="category">${escapeHtml(item.categoryName || "-")}</td><td data-column="brand">${escapeHtml(item.brandName || "-")}</td><td data-column="unit">${escapeHtml(item.unitName)}</td><td data-column="cost" data-edit="cost" data-value="${item.costPrice}" tabindex="0" class="products-number products-editable">${displayValue("cost", item.costPrice)}</td><td data-column="price" data-edit="price" data-value="${item.salePrice}" tabindex="0" class="products-number products-editable">${displayValue("price", item.salePrice)}</td><td data-column="margin" class="products-number">${item.marginPercentage}%</td><td data-column="validity" data-edit="priceValidUntil" data-value="${item.priceValidUntil || ""}" tabindex="0" class="products-editable">${item.priceValidUntil || "Sem validade"}</td><td data-column="stock" data-edit="minimumStock" data-value="${item.minimumStock || ""}" tabindex="0" class="products-editable">${item.controlsStock ? (item.minimumStock || "Aguardando estoque") : "Nao controlado"}</td><td data-column="actions"><a href="${location.pathname}/${item.id}/Edit">Editar</a></td></tr>`;
  }
  function escapeHtml(value) { const span = document.createElement("span"); span.textContent = value || ""; return span.innerHTML; }
  function startResize(event) {
    const column = event.currentTarget.dataset.resizer, col = table.querySelector(`[data-col="${column}"]`), startX = event.clientX, startWidth = col.getBoundingClientRect().width;
    const move = e => setColumnWidth(column, Math.max(96, Math.round(startWidth + e.clientX - startX)) + "px");
    const stop = () => { document.removeEventListener("pointermove", move); document.removeEventListener("pointerup", stop); state.widths[column] = col.style.width; persistState(); };
    document.addEventListener("pointermove", move); document.addEventListener("pointerup", stop);
  }
  function setColumnWidth(column, width) { const col = table.querySelector(`[data-col="${column}"]`); if (col) col.style.width = width; }
  function toast(message, status) { let el = root.querySelector("[data-grid-toast]"); if (!el) { el = document.createElement("div"); el.dataset.gridToast = ""; el.className = "products-grid-toast"; root.appendChild(el); } el.textContent = message; el.dataset.status = status; el.hidden = false; setTimeout(() => el.hidden = true, 2800); }
})();
