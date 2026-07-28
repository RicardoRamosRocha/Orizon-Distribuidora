(function () {
  "use strict";

  const root = document.querySelector("[data-products-grid]");
  if (!root || root.dataset.gridReady === "true") return;
  root.dataset.gridReady = "true";

  const table = root.querySelector(".products-grid");
  const tbody = table?.querySelector("tbody");
  const scroll = root.querySelector(".products-grid-scroll");
  const form = root.querySelector("[data-server-filter-form]");
  const search = form?.querySelector('[name="Search"]');
  const pageSize = root.querySelector("[data-page-size]");
  const token = root.querySelector('input[name="__RequestVerificationToken"]')?.value || "";
  if (!table || !tbody || !scroll || !form || !search) return;

  const storageKey = "orizon.products.grid.v4";
  const defaultState = { hiddenColumns: [], widths: {}, order: [], pinned: ["code", "name"] };
  const totalRecords = Number(root.dataset.totalRecords) || 0;
  let state = loadState();
  let allFiltered = false;
  let lastSelected = -1;
  let loading = false;
  let toastTimer;
  let hasMore = Number(root.dataset.page) * Number(pageSize?.value || 50) < totalRecords;
  let nextPage = Number(root.dataset.page) + 1;

  hydrateServerState();
  applyColumns();
  updateSelection();

  let debounce;
  search.addEventListener("input", function () {
    clearTimeout(debounce);
    debounce = setTimeout(function () {
      form.elements.Page.value = 1;
      form.submit();
    }, 350);
  });

  pageSize?.addEventListener("change", function () {
    document.getElementById("products-page-size-form")?.requestSubmit();
  });

  document.addEventListener("keydown", function (event) {
    if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "k") {
      event.preventDefault();
      search.focus();
    }
    if (event.key === "Escape") closeMenus();
  });

  document.addEventListener("click", function (event) {
    if (!root.contains(event.target)) closeMenus();
  });

  root.addEventListener("change", function (event) {
    if (event.target.matches("[data-column-toggle]")) {
      setColumnVisibility(event.target.dataset.columnToggle, event.target.checked);
      persistState();
    }
    if (event.target.matches("[data-select-all]")) selectPage(event.target.checked);
  });

  root.addEventListener("click", function (event) {
    const exportLink = event.target.closest("[data-grid-export]");
    if (exportLink) {
      event.preventDefault();
      const params = new URLSearchParams(new FormData(form));
      params.delete("Page");
      params.delete("PageSize");
      root.querySelectorAll("[data-column-toggle]:checked").forEach(function (toggle) {
        params.append("columns", toggle.dataset.columnToggle);
      });
      window.location.href = `${root.dataset.gridUrl.replace(/\/GridData$/, "")}/Export/${exportLink.dataset.gridExport}?${params}`;
      return;
    }

    const columnToggle = event.target.closest("[data-column-menu-toggle]");
    if (columnToggle) {
      toggleMenu(columnToggle, root.querySelector("[data-column-menu]"));
      return;
    }

    const exportToggle = event.target.closest("[data-export-menu-toggle]");
    if (exportToggle) {
      toggleMenu(exportToggle, root.querySelector("[data-export-menu]"));
      return;
    }

    const select = event.target.closest("[data-row-select]");
    if (select) {
      const rows = currentRows();
      const index = rows.indexOf(select.closest("[data-row]"));
      if (event.shiftKey && lastSelected >= 0) {
        for (let i = Math.min(index, lastSelected); i <= Math.max(index, lastSelected); i++) {
          rows[i].querySelector("[data-row-select]").checked = select.checked;
        }
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
      toast(`${totalRecords} produtos filtrados selecionados.`, "saved");
      return;
    }

    const bulk = event.target.closest("[data-bulk-operation]");
    if (bulk) runBulk(bulk);
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

    if (event.key === "Enter") {
      event.preventDefault();
      beginEdit(cell);
    } else if (event.key === "Tab") {
      event.preventDefault();
      cells[index + (event.shiftKey ? -1 : 1)]?.focus();
    } else if (event.key === "ArrowRight") {
      cells[index + 1]?.focus();
    } else if (event.key === "ArrowLeft") {
      cells[index - 1]?.focus();
    } else if (event.key === "ArrowDown") {
      cell.parentElement.nextElementSibling?.querySelector(`[data-edit="${cell.dataset.edit}"]`)?.focus();
    } else if (event.key === "ArrowUp") {
      cell.parentElement.previousElementSibling?.querySelector(`[data-edit="${cell.dataset.edit}"]`)?.focus();
    } else if (event.key === "Home") {
      cells[0]?.focus();
    } else if (event.key === "End") {
      cells.at(-1)?.focus();
    } else if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "c") {
      await copySelection(cell);
    } else if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "v") {
      await pasteAt(cell);
    } else if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "d") {
      await fillDown(cell);
    } else if (event.key === "Delete") {
      await saveCell(cell, "");
    }

    if (document.activeElement?.matches("[data-edit]")) {
      document.activeElement.scrollIntoView({ block: "nearest", inline: "nearest" });
    }
  });

  scroll.addEventListener("scroll", function () {
    if (hasMore && !loading && scroll.scrollTop + scroll.clientHeight >= scroll.scrollHeight - 240) {
      loadMore();
    }
  });

  root.querySelectorAll("[data-resizer]").forEach(function (handle) {
    handle.addEventListener("pointerdown", startResize);
  });

  function currentRows() {
    return Array.from(tbody.querySelectorAll("[data-row]"));
  }

  function editableCells() {
    return Array.from(tbody.querySelectorAll("[data-edit]"));
  }

  function loadState() {
    try {
      return { ...defaultState, ...JSON.parse(localStorage.getItem(storageKey) || "{}") };
    } catch {
      return { ...defaultState };
    }
  }

  function persistState() {
    localStorage.setItem(storageKey, JSON.stringify(state));
    fetch(root.dataset.preferenceUrl, {
      method: "PUT",
      headers: jsonHeaders(),
      body: JSON.stringify({ stateJson: JSON.stringify(state) })
    }).catch(function () {
      toast("Preferência salva apenas neste dispositivo.", "error");
    });
  }

  async function hydrateServerState() {
    try {
      const response = await fetch(root.dataset.preferenceUrl);
      if (!response.ok) return;
      const data = await response.json();
      if (data.stateJson) {
        state = { ...defaultState, ...JSON.parse(data.stateJson) };
        localStorage.setItem(storageKey, JSON.stringify(state));
        applyColumns();
      }
    } catch {
      // A preferência local mantém a grade utilizável sem conexão.
    }
  }

  function jsonHeaders() {
    return { "Content-Type": "application/json", "RequestVerificationToken": token };
  }

  function applyColumns() {
    Object.entries(state.widths || {}).forEach(function ([key, width]) {
      setColumnWidth(key, width);
    });
    root.querySelectorAll("[data-column-toggle]").forEach(function (toggle) {
      const visible = !state.hiddenColumns.includes(toggle.dataset.columnToggle);
      toggle.checked = visible;
      setColumnVisibility(toggle.dataset.columnToggle, visible, false);
    });
  }

  function setColumnVisibility(column, visible, updateState = true) {
    table.querySelectorAll(`[data-column="${column}"], [data-col="${column}"]`).forEach(function (element) {
      element.hidden = !visible;
    });
    if (updateState) {
      state.hiddenColumns = state.hiddenColumns.filter(function (item) { return item !== column; });
      if (!visible) state.hiddenColumns.push(column);
    }
  }

  function selectPage(checked) {
    currentRows().forEach(function (row) {
      row.querySelector("[data-row-select]").checked = checked;
    });
    if (!checked) allFiltered = false;
    updateSelection();
  }

  function selectedIds() {
    return currentRows()
      .filter(function (row) { return row.querySelector("[data-row-select]").checked; })
      .map(function (row) { return row.dataset.id; });
  }

  function updateSelection() {
    const ids = selectedIds();
    const visibleCount = allFiltered ? totalRecords : ids.length;
    root.querySelector("[data-selected-count]").textContent = visibleCount;
    root.querySelector("[data-bulkbar]").hidden = ids.length === 0;
    const selectAll = root.querySelector("[data-select-all]");
    selectAll.checked = currentRows().length > 0 && ids.length === currentRows().length;
    selectAll.indeterminate = ids.length > 0 && ids.length < currentRows().length;
    currentRows().forEach(function (row) {
      const selected = row.querySelector("[data-row-select]").checked;
      row.classList.toggle("is-selected", selected);
      row.setAttribute("aria-selected", selected.toString());
    });
  }

  async function runBulk(button) {
    const operation = button.dataset.bulkOperation;
    let value = null;
    if (operation === "price-percent") {
      value = prompt("Percentual de ajuste (use negativo para reduzir):", "0");
      if (value === null) return;
    }
    if (operation === "delete" && !confirm("Excluir logicamente os produtos selecionados?")) return;

    setBusy(true, button, "Processando…");
    try {
      const response = await fetch(root.dataset.bulkUrl, {
        method: "POST",
        headers: jsonHeaders(),
        body: JSON.stringify({
          ids: selectedIds(),
          allFiltered,
          filter: Object.fromEntries(new FormData(form)),
          operation,
          value
        })
      });
      const data = await readJson(response);
      if (!response.ok) throw new Error(data.message || "Falha na operação.");
      toast(`${data.affected} produto(s) alterado(s).`, "saved");
      setTimeout(function () { window.location.reload(); }, 500);
    } catch (error) {
      toast(error.message || "Falha na operação.", "error");
      setBusy(false, button);
    }
  }

  function beginEdit(cell) {
    if (cell.querySelector("input") || cell.dataset.saving === "true") return;
    const old = cell.dataset.value || "";
    const input = document.createElement("input");
    input.value = old;
    input.className = "products-inline-input";
    input.setAttribute("aria-label", `Editar ${cell.dataset.edit}`);
    cell.textContent = "";
    cell.appendChild(input);
    cell.classList.add("is-pending");
    input.focus();
    input.select();

    let committed = false;
    async function commit() {
      if (committed) return;
      committed = true;
      await saveCell(cell, input.value);
    }

    input.addEventListener("keydown", async function (event) {
      if (event.key === "Escape") {
        committed = true;
        cell.textContent = displayValue(cell.dataset.edit, old);
        cell.classList.remove("is-pending");
        cell.focus();
      } else if (event.key === "Enter" || event.key === "Tab") {
        event.preventDefault();
        await commit();
        if (event.key === "Tab") moveAfter(cell, event.shiftKey ? -1 : 1);
      }
    });
    input.addEventListener("blur", commit, { once: true });
  }

  async function saveCell(cell, value) {
    if (cell.dataset.saving === "true") return;
    const row = cell.closest("[data-row]");
    const previousValue = cell.dataset.value || "";
    cell.dataset.saving = "true";
    cell.classList.add("is-saving");
    cell.classList.remove("is-error");
    cell.textContent = "Salvando…";
    scroll.setAttribute("aria-busy", "true");

    try {
      const response = await fetch(root.dataset.inlineUrl, {
        method: "POST",
        headers: jsonHeaders(),
        body: JSON.stringify({ id: row.dataset.id, field: cell.dataset.edit, value })
      });
      const data = await readJson(response);
      if (!response.ok) throw new Error(data.message || "Valor inválido.");

      cell.dataset.value = value;
      cell.textContent = displayValue(cell.dataset.edit, value);
      cell.classList.remove("is-pending", "is-saving", "is-error");
      cell.classList.add("is-saved");
      row.classList.add("is-changed");
      announce("Alteração salva.");
      setTimeout(function () { cell.classList.remove("is-saved"); }, 1500);
    } catch (error) {
      cell.textContent = displayValue(cell.dataset.edit, previousValue);
      cell.classList.remove("is-pending", "is-saving");
      cell.classList.add("is-error");
      toast(error.message || "Não foi possível salvar a alteração.", "error");
    } finally {
      delete cell.dataset.saving;
      scroll.setAttribute("aria-busy", loading.toString());
    }
  }

  function displayValue(field, value) {
    if (value === null || value === undefined || value === "") {
      return field === "priceValidUntil" ? "Sem validade" : "-";
    }
    if (field === "cost" || field === "price") {
      const number = Number(String(value).replace(",", "."));
      return Number.isFinite(number)
        ? number.toLocaleString("pt-BR", { style: "currency", currency: "BRL" })
        : value;
    }
    if (field === "priceValidUntil") return formatDate(value);
    return value;
  }

  function moveAfter(cell, offset) {
    const cells = editableCells();
    cells[cells.indexOf(cell) + offset]?.focus();
  }

  async function copySelection(cell) {
    try {
      await navigator.clipboard.writeText(cell.dataset.value || cell.textContent.trim());
      toast("Valor copiado.", "saved");
    } catch {
      toast("O navegador não permitiu copiar o valor.", "error");
    }
  }

  async function pasteAt(cell) {
    try {
      const content = await navigator.clipboard.readText();
      const matrix = content.replace(/\r/g, "").split("\n").filter(Boolean).map(function (line) {
        return line.split("\t");
      });
      const rows = currentRows();
      const startRow = rows.indexOf(cell.closest("tr"));
      const columns = Array.from(rows[startRow].querySelectorAll("[data-edit]"));
      const startCol = columns.indexOf(cell);
      for (let rowIndex = 0; rowIndex < matrix.length; rowIndex++) {
        for (let columnIndex = 0; columnIndex < matrix[rowIndex].length; columnIndex++) {
          const target = rows[startRow + rowIndex]?.querySelectorAll("[data-edit]")[startCol + columnIndex];
          if (target) await saveCell(target, matrix[rowIndex][columnIndex].trim());
        }
      }
    } catch {
      toast("O navegador não permitiu colar os valores.", "error");
    }
  }

  async function fillDown(cell) {
    const next = cell.closest("tr").nextElementSibling?.querySelector(`[data-edit="${cell.dataset.edit}"]`);
    if (next) await saveCell(next, cell.dataset.value || "");
  }

  async function loadMore() {
    loading = true;
    root.classList.add("is-loading-more");
    scroll.setAttribute("aria-busy", "true");
    const params = new URLSearchParams(new FormData(form));
    params.set("Page", nextPage);
    params.set("PageSize", pageSize?.value || "50");

    try {
      const response = await fetch(`${root.dataset.gridUrl}?${params}`);
      const data = await readJson(response);
      if (!response.ok) throw new Error(data.message || "Falha ao carregar mais produtos.");
      data.items.forEach(function (item) {
        tbody.insertAdjacentHTML("beforeend", rowHtml(item));
      });
      hasMore = data.hasMore;
      nextPage++;
      applyColumns();
      updateSelection();
      announce(`${data.items.length} produtos adicionais carregados.`);
    } catch (error) {
      toast(error.message || "Falha ao carregar mais produtos.", "error");
    } finally {
      loading = false;
      root.classList.remove("is-loading-more");
      scroll.setAttribute("aria-busy", "false");
    }
  }

  function rowHtml(item) {
    const type = { 1: "Próprio", 2: "Terceiro", 3: "Sob encomenda", 4: "Serviço" }[item.productType] || "Produto";
    const typeClass = { 1: "own", 2: "thirdparty", 3: "madetoorder", 4: "service" }[item.productType] || "product";
    const id = encodeURIComponent(item.id);
    const name = escapeHtml(item.name);
    const minimumStockValue = item.minimumStock ?? "";
    const statusAction = item.isActive ? "Deactivate" : "Activate";
    const statusLabel = item.isActive ? "Inativar" : "Ativar";
    const stock = !item.controlsStock
      ? (item.productType === 4 ? "Não aplicável" : "Não controlado")
      : (item.minimumStock === null || item.minimumStock === undefined ? "Aguardando estoque" : `Min. ${item.minimumStock}`);
    return `<tr data-row data-id="${escapeAttribute(item.id)}" aria-selected="false">
      <td class="products-select-cell products-sticky-select"><input type="checkbox" value="${escapeAttribute(item.id)}" data-row-select aria-label="Selecionar ${escapeAttribute(item.name)}"></td>
      <td data-column="code" class="products-code products-sticky-col">${escapeHtml(item.internalCode)}</td>
      <td data-column="name" class="products-name products-sticky-col"><span>${name}</span>${item.shortDescription ? `<small>${escapeHtml(item.shortDescription)}</small>` : ""}</td>
      <td data-column="status"><span class="products-badge products-badge-status ${item.isActive ? "is-active" : "is-muted"}">${item.isActive ? "Ativo" : "Inativo"}</span></td>
      <td data-column="type"><span class="products-badge products-badge-type is-type-${typeClass}">${type}</span></td>
      <td data-column="category">${escapeHtml(item.categoryName || "-")}</td>
      <td data-column="brand">${escapeHtml(item.brandName || "-")}</td>
      <td data-column="unit">${escapeHtml(item.unitName)}</td>
      <td data-column="cost" data-edit="cost" data-value="${escapeAttribute(item.costPrice)}" tabindex="0" class="products-number products-editable">${displayValue("cost", item.costPrice)}</td>
      <td data-column="price" data-edit="price" data-value="${escapeAttribute(item.salePrice)}" tabindex="0" class="products-number products-editable">${displayValue("price", item.salePrice)}</td>
      <td data-column="margin" class="products-number">${escapeHtml(item.marginPercentage)}%</td>
      <td data-column="validity" data-edit="priceValidUntil" data-value="${escapeAttribute(item.priceValidUntil || "")}" tabindex="0" class="products-editable">${displayValue("priceValidUntil", item.priceValidUntil)}</td>
      <td data-column="stock" data-edit="minimumStock" data-value="${escapeAttribute(minimumStockValue)}" tabindex="0" class="products-editable">${escapeHtml(stock)}</td>
      <td data-column="actions" class="products-row-actions">
        <a href="${root.dataset.gridUrl.replace(/\/GridData$/, "")}/${id}/Edit"><i class="ph ph-pencil-simple"></i> Editar</a>
        <a href="${root.dataset.gridUrl.replace(/\/GridData$/, "")}/${id}/History" aria-label="Histórico de ${escapeAttribute(item.name)}" title="Histórico"><i class="ph ph-clock-counter-clockwise"></i><span class="products-action-label">Histórico</span></a>
        <form method="post" action="${root.dataset.gridUrl.replace(/\/GridData$/, "")}/${id}/${statusAction}">
          <input name="__RequestVerificationToken" type="hidden" value="${escapeAttribute(token)}">
          <button type="submit">${statusLabel}</button>
        </form>
      </td>
    </tr>`;
  }

  function escapeHtml(value) {
    const span = document.createElement("span");
    span.textContent = value === null || value === undefined ? "" : String(value);
    return span.innerHTML;
  }

  function escapeAttribute(value) {
    return escapeHtml(value).replace(/"/g, "&quot;");
  }

  function formatDate(value) {
    const parts = String(value).slice(0, 10).split("-");
    return parts.length === 3 ? `${parts[2]}/${parts[1]}/${parts[0]}` : value;
  }

  function startResize(event) {
    event.preventDefault();
    const column = event.currentTarget.dataset.resizer;
    const col = table.querySelector(`[data-col="${column}"]`);
    if (!col) return;
    const startX = event.clientX;
    const startWidth = col.getBoundingClientRect().width;
    document.body.style.cursor = "col-resize";

    const move = function (moveEvent) {
      setColumnWidth(column, `${Math.max(96, Math.round(startWidth + moveEvent.clientX - startX))}px`);
    };
    const stop = function () {
      document.removeEventListener("pointermove", move);
      document.removeEventListener("pointerup", stop);
      document.body.style.cursor = "";
      state.widths[column] = col.style.width;
      persistState();
    };
    document.addEventListener("pointermove", move);
    document.addEventListener("pointerup", stop);
  }

  function setColumnWidth(column, width) {
    const col = table.querySelector(`[data-col="${column}"]`);
    if (col) col.style.width = width;
  }

  function toggleMenu(button, menu) {
    const shouldOpen = menu.hidden;
    closeMenus();
    menu.hidden = !shouldOpen;
    button.setAttribute("aria-expanded", shouldOpen.toString());
    if (shouldOpen) menu.querySelector("a, input, button")?.focus();
  }

  function closeMenus() {
    root.querySelectorAll("[data-column-menu], [data-export-menu]").forEach(function (menu) {
      menu.hidden = true;
    });
    root.querySelectorAll("[data-column-menu-toggle], [data-export-menu-toggle]").forEach(function (button) {
      button.setAttribute("aria-expanded", "false");
    });
  }

  function setBusy(isBusy, button, busyText) {
    scroll.setAttribute("aria-busy", isBusy.toString());
    if (!button) return;
    if (isBusy) {
      button.dataset.originalText = button.innerHTML;
      button.disabled = true;
      button.textContent = busyText;
    } else {
      button.disabled = false;
      if (button.dataset.originalText) button.innerHTML = button.dataset.originalText;
      delete button.dataset.originalText;
    }
  }

  async function readJson(response) {
    const contentType = response.headers.get("content-type") || "";
    return contentType.includes("application/json") ? response.json() : {};
  }

  function announce(message) {
    let live = root.querySelector("[data-grid-live]");
    if (!live) {
      live = document.createElement("div");
      live.dataset.gridLive = "";
      live.className = "products-visually-hidden";
      live.setAttribute("role", "status");
      live.setAttribute("aria-live", "polite");
      root.appendChild(live);
    }
    live.textContent = "";
    requestAnimationFrame(function () { live.textContent = message; });
  }

  function toast(message, status) {
    let element = root.querySelector("[data-grid-toast]");
    if (!element) {
      element = document.createElement("div");
      element.dataset.gridToast = "";
      element.className = "products-grid-toast";
      element.setAttribute("role", status === "error" ? "alert" : "status");
      element.setAttribute("aria-live", status === "error" ? "assertive" : "polite");
      root.appendChild(element);
    }
    clearTimeout(toastTimer);
    element.textContent = message;
    element.dataset.status = status;
    element.hidden = false;
    toastTimer = setTimeout(function () { element.hidden = true; }, 3200);
  }
})();
