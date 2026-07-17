(function () {
    const rows = Array.from(document.querySelectorAll("[data-select-row]"));
    const selectPage = document.querySelector("[data-select-page]");
    const bulkbar = document.querySelector("[data-products-bulkbar]");
    const count = document.querySelector("[data-selected-count]");
    const forms = Array.from(document.querySelectorAll("[data-bulk-form]"));

    if (!rows.length || !bulkbar || !count) {
        return;
    }

    function selectedValues() {
        return rows.filter(row => row.checked).map(row => row.value);
    }

    function sync() {
        const values = selectedValues();
        count.textContent = String(values.length);
        bulkbar.hidden = values.length === 0;

        if (selectPage) {
            selectPage.checked = values.length === rows.length;
            selectPage.indeterminate = values.length > 0 && values.length < rows.length;
        }

        forms.forEach(form => {
            form.querySelectorAll("input[name='selectedIds']").forEach(input => input.remove());
            values.forEach(value => {
                const input = document.createElement("input");
                input.type = "hidden";
                input.name = "selectedIds";
                input.value = value;
                form.appendChild(input);
            });
        });
    }

    rows.forEach(row => row.addEventListener("change", sync));

    if (selectPage) {
        selectPage.addEventListener("change", () => {
            rows.forEach(row => {
                row.checked = selectPage.checked;
            });
            sync();
        });
    }

    forms.forEach(form => {
        form.addEventListener("submit", event => {
            if (selectedValues().length === 0 || !window.confirm("Aplicar ação aos produtos selecionados nesta página?")) {
                event.preventDefault();
            }
        });
    });

    sync();
}());
