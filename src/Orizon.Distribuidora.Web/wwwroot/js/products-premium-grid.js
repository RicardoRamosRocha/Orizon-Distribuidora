(function () {
  const root = document.querySelector("[data-products-grid]");

  if (!root) {
    return;
  }

  const storageKey = "orizon.products.grid.v2";
  const themeKey = "orizon.products.theme";
  const table = root.querySelector(".products-grid");
  const rows = Array.from(root.querySelectorAll("[data-row]"));
  const searchInput = root.querySelector('[name="Search"]');
  const selectAll = root.querySelector("[data-select-all]");
  const bulkbar = root.querySelector("[data-bulkbar]");
  const selectedCount = root.querySelector("[data-selected-count]");
  const columnMenu = root.querySelector("[data-column-menu]");
  const columnMenuToggle = root.querySelector("[data-column-menu-toggle]");
  const themeToggle = root.querySelector("[data-theme-toggle]");
  const defaultState = {
    hiddenColumns: [],
    widths: {}
  };
  let state = loadState();

  initializeTheme();
  applyColumnPreferences();
  updateSelectionSummary();

  root.addEventListener("input", function (event) {
    const target = event.target;

    if (target.matches("[data-row-select]")) {
      updateRowSelection(target.closest("[data-row]"), target.checked);
      updateSelectionSummary();
      return;
    }

    if (target.matches("[data-select-all]")) {
      setPageRowsSelected(target.checked);
      return;
    }

    if (target.matches("[data-column-toggle]")) {
      setColumnVisibility(target.dataset.columnToggle, target.checked);
      persistState();
    }
  });

  root.addEventListener("click", function (event) {
    const row = event.target.closest("[data-row]");

    if (event.target.closest("[data-column-menu-toggle]")) {
      toggleColumnMenu();
      return;
    }

    if (event.target.closest("[data-theme-toggle]")) {
      toggleTheme();
      return;
    }

    if (row && !event.target.matches("input, button, a, select") && !event.target.closest("form")) {
      const checkbox = row.querySelector("[data-row-select]");
      checkbox.checked = !checkbox.checked;
      updateRowSelection(row, checkbox.checked);
      updateSelectionSummary();
    }
  });

  root.addEventListener("submit", function (event) {
    const form = event.target.closest("[data-bulk-form]");

    if (!form) {
      return;
    }

    syncBulkForm(form);
  });

  document.addEventListener("keydown", function (event) {
    if (event.key === "Escape") {
      closeColumnMenu();
      return;
    }

    if (isTyping(event.target)) {
      return;
    }

    if (event.key === "/") {
      event.preventDefault();
      searchInput?.focus();
      return;
    }

    if (event.key.toLowerCase() === "n") {
      const createLink = root.querySelector('a[href$="/Create"]');
      createLink?.click();
    }
  });

  document.addEventListener("click", function (event) {
    if (!root.contains(event.target)) {
      closeColumnMenu();
    }
  });

  root.querySelectorAll("[data-resizer]").forEach(function (handle) {
    handle.addEventListener("pointerdown", startResize);
  });

  function loadState() {
    try {
      return { ...defaultState, ...JSON.parse(localStorage.getItem(storageKey) || "{}") };
    } catch {
      return { ...defaultState };
    }
  }

  function persistState() {
    localStorage.setItem(storageKey, JSON.stringify(state));
  }

  function initializeTheme() {
    const savedTheme = localStorage.getItem(themeKey) || "light";
    document.documentElement.dataset.productsTheme = savedTheme;
    updateThemeIcon(savedTheme);
  }

  function toggleTheme() {
    const nextTheme = document.documentElement.dataset.productsTheme === "dark" ? "light" : "dark";
    document.documentElement.dataset.productsTheme = nextTheme;
    localStorage.setItem(themeKey, nextTheme);
    updateThemeIcon(nextTheme);
  }

  function updateThemeIcon(theme) {
    const icon = themeToggle?.querySelector("i");

    if (icon) {
      icon.className = theme === "dark" ? "ph ph-sun" : "ph ph-moon";
    }
  }

  function setPageRowsSelected(checked) {
    rows.forEach(function (row) {
      row.querySelector("[data-row-select]").checked = checked;
      updateRowSelection(row, checked);
    });
    updateSelectionSummary();
  }

  function updateRowSelection(row, checked) {
    row?.classList.toggle("is-selected", checked);
  }

  function selectedRows() {
    return rows.filter(function (row) {
      return row.querySelector("[data-row-select]").checked;
    });
  }

  function updateSelectionSummary() {
    const selected = selectedRows();

    if (selectedCount) {
      selectedCount.textContent = selected.length.toString();
    }

    if (bulkbar) {
      bulkbar.hidden = selected.length === 0;
    }

    if (selectAll) {
      selectAll.checked = rows.length > 0 && selected.length === rows.length;
      selectAll.indeterminate = selected.length > 0 && selected.length < rows.length;
    }
  }

  function syncBulkForm(form) {
    form.querySelectorAll('input[name="selectedIds"]').forEach(function (input) {
      input.remove();
    });

    selectedRows().forEach(function (row) {
      const input = document.createElement("input");
      input.type = "hidden";
      input.name = "selectedIds";
      input.value = row.dataset.id;
      form.appendChild(input);
    });
  }

  function toggleColumnMenu() {
    const isOpen = !columnMenu.hidden;
    columnMenu.hidden = isOpen;
    columnMenuToggle.setAttribute("aria-expanded", (!isOpen).toString());
  }

  function closeColumnMenu() {
    if (!columnMenu || !columnMenuToggle) {
      return;
    }

    columnMenu.hidden = true;
    columnMenuToggle.setAttribute("aria-expanded", "false");
  }

  function applyColumnPreferences() {
    Object.keys(state.widths || {}).forEach(function (column) {
      setColumnWidth(column, state.widths[column]);
    });

    root.querySelectorAll("[data-column-toggle]").forEach(function (toggle) {
      const column = toggle.dataset.columnToggle;
      const isVisible = !state.hiddenColumns.includes(column);
      toggle.checked = isVisible;
      setColumnVisibility(column, isVisible, false);
    });
  }

  function setColumnVisibility(column, isVisible, updateToggle) {
    table.querySelectorAll(`[data-column="${column}"], [data-col="${column}"]`).forEach(function (element) {
      element.hidden = !isVisible;
    });

    if (updateToggle !== false) {
      const toggle = root.querySelector(`[data-column-toggle="${column}"]`);
      if (toggle) {
        toggle.checked = isVisible;
      }
    }

    state.hiddenColumns = state.hiddenColumns.filter(function (item) {
      return item !== column;
    });

    if (!isVisible) {
      state.hiddenColumns.push(column);
    }
  }

  function startResize(event) {
    const column = event.currentTarget.dataset.resizer;
    const col = table.querySelector(`[data-col="${column}"]`);

    if (!col) {
      return;
    }

    const startX = event.clientX;
    const startWidth = col.getBoundingClientRect().width;

    event.currentTarget.setPointerCapture(event.pointerId);

    function move(moveEvent) {
      const width = Math.max(96, Math.round(startWidth + moveEvent.clientX - startX));
      setColumnWidth(column, width);
    }

    function stop() {
      document.removeEventListener("pointermove", move);
      document.removeEventListener("pointerup", stop);
      state.widths[column] = table.querySelector(`[data-col="${column}"]`).style.width;
      persistState();
    }

    document.addEventListener("pointermove", move);
    document.addEventListener("pointerup", stop);
  }

  function setColumnWidth(column, width) {
    const value = typeof width === "number" ? `${width}px` : width;
    const col = table.querySelector(`[data-col="${column}"]`);

    if (col) {
      col.style.width = value;
    }
  }

  function isTyping(target) {
    return target instanceof HTMLInputElement ||
      target instanceof HTMLTextAreaElement ||
      target instanceof HTMLSelectElement ||
      target?.isContentEditable;
  }
})();
