(function () {
    const form = document.querySelector("[data-product-form]");
    if (!form) {
        return;
    }

    const type = form.querySelector("[data-product-type]");
    const controlsStock = form.querySelector("[data-controls-stock]");
    const stockInputs = Array.from(form.querySelectorAll("[data-stock-input]"));
    const cost = form.querySelector("[data-cost]");
    const price = form.querySelector("[data-price]");
    const margin = form.querySelector("[data-margin]");
    const category = form.querySelector("[data-category-select]");
    const subcategory = form.querySelector("[data-subcategory-select]");
    const warehouse = form.querySelector("[data-warehouse-select]");
    const location = form.querySelector("[data-location-select]");

    function isNoStockType() {
        return type && (type.value === "ThirdParty" || type.value === "Service");
    }

    function syncStock() {
        if (!controlsStock) {
            return;
        }

        if (isNoStockType()) {
            controlsStock.checked = false;
            controlsStock.disabled = true;
        } else {
            controlsStock.disabled = false;
        }

        const disabled = !controlsStock.checked;
        stockInputs.forEach(input => {
            input.disabled = disabled;
            if (disabled) {
                input.value = "";
            }
        });
    }

    function syncMargin() {
        if (!cost || !price || !margin) {
            return;
        }

        const costValue = Number(cost.value || 0);
        const priceValue = Number(price.value || 0);
        const value = priceValue > 0 ? ((priceValue - costValue) / priceValue) * 100 : 0;
        margin.value = `${value.toFixed(2)}%`;
    }

    async function loadOptions(source, target, urlFactory) {
        if (!source || !target) {
            return;
        }

        target.innerHTML = "<option value=\"\">Selecione</option>";
        if (!source.value) {
            return;
        }

        const response = await fetch(urlFactory(source.value), { headers: { "Accept": "application/json" } });
        if (!response.ok) {
            return;
        }

        const items = await response.json();
        items.forEach(item => {
            const option = document.createElement("option");
            option.value = item.id;
            option.textContent = item.text;
            target.appendChild(option);
        });
    }

    type?.addEventListener("change", syncStock);
    controlsStock?.addEventListener("change", syncStock);
    cost?.addEventListener("input", syncMargin);
    price?.addEventListener("input", syncMargin);
    category?.addEventListener("change", () => loadOptions(category, subcategory, id => `/Admin/Products/Subcategories?categoryId=${encodeURIComponent(id)}`));
    warehouse?.addEventListener("change", () => loadOptions(warehouse, location, id => `/Admin/Products/Locations?warehouseId=${encodeURIComponent(id)}`));

    syncStock();
    syncMargin();
}());
