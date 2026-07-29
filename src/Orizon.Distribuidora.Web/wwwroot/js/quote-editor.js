(() => {
  "use strict";

  const parseDecimal = value => {
    const raw = String(value ?? "").trim().replace(/\s/g, "");
    if (!raw) return 0;
    const normalized = raw.includes(",")
      ? raw.replace(/\./g, "").replace(",", ".")
      : raw;
    const parsed = Number(normalized);
    return Number.isFinite(parsed) ? parsed : 0;
  };
  const roundMoney = value => Math.round((value + Number.EPSILON) * 100) / 100;
  const clampQuantity = value => Math.max(1, parseDecimal(value));
  const money = value => roundMoney(value).toLocaleString("pt-BR", { style: "currency", currency: "BRL" });

  const initialize = root => {
    if (!root || root.dataset.quoteEditorInitialized === "true") return;
    root.dataset.quoteEditorInitialized = "true";

    const body = root.querySelector("[data-items]");
    const picker = root.querySelector("[data-product-picker]");
    const search = root.querySelector("[data-product-search]");
    const results = root.querySelector("[data-product-results]");
    const status = root.querySelector("[data-product-status]");
    const empty = root.querySelector("[data-items-empty]");

    const reindex = () => body.querySelectorAll("[data-item-row]").forEach((row, index) =>
      row.querySelectorAll("[name]").forEach(input => {
        input.name = input.name.replace(/Items\[\d+\]/, `Items[${index}]`);
        if (input.id) input.id = input.id.replace(/Items_\d+__/, `Items_${index}__`);
      }));

    const calculate = () => {
      let subtotal = 0;
      body.querySelectorAll("[data-item-row]").forEach(row => {
        const quantity = clampQuantity(row.querySelector("[data-quantity]").value);
        const price = Math.max(0, parseDecimal(row.querySelector("[data-price]").value));
        const discount = Math.max(0, parseDecimal(row.querySelector("[data-discount]").value));
        const total = Math.max(0, roundMoney(quantity * price) - discount);
        row.querySelector("[data-line-total]").textContent = money(total);
        subtotal += total;
      });
      subtotal = roundMoney(subtotal);
      const total = Math.max(0, roundMoney(
        subtotal - parseDecimal(root.querySelector("[data-document-discount]").value) +
        parseDecimal(root.querySelector("[data-freight]").value) +
        parseDecimal(root.querySelector("[data-charges]").value)));
      root.querySelector("[data-document-subtotal]").textContent = money(subtotal);
      root.querySelector("[data-document-total]").textContent = money(total);
      empty.hidden = body.querySelector("[data-item-row]") !== null;
    };

    const setQuantity = (input, value) => {
      input.value = String(clampQuantity(value)).replace(".", ",");
      input.setCustomValidity("");
      calculate();
    };

    const add = product => {
      if (body.querySelector(`[data-product-id="${CSS.escape(product.id)}"]`)) {
        status.textContent = "Este produto já foi adicionado.";
        return;
      }
      const index = body.querySelectorAll("[data-item-row]").length;
      const warehouses = root.querySelector("[data-warehouse-template]").innerHTML;
      const row = document.createElement("tr");
      row.dataset.itemRow = "";
      row.dataset.productId = product.id;
      row.innerHTML = `<td class="item-product"><input type="hidden" name="Items[${index}].ProductId" value="${product.id}"><input name="Items[${index}].ProductLabel" value="" readonly></td><td><select name="Items[${index}].WarehouseId">${warehouses}</select></td><td><div class="quantity-control"><button type="button" data-quantity-decrease aria-label="Diminuir quantidade">−</button><input name="Items[${index}].Quantity" type="text" inputmode="decimal" min="1" step="1" value="1" data-quantity aria-label="Quantidade"><button type="button" data-quantity-increase aria-label="Aumentar quantidade">+</button></div></td><td><input name="Items[${index}].UnitPrice" type="text" inputmode="decimal" data-price aria-label="Preço unitário"></td><td><input name="Items[${index}].Discount" type="text" inputmode="decimal" value="0" data-discount aria-label="Desconto do item"></td><td class="line-total num" data-line-total></td><td><button type="button" class="commercial-link danger remove-item" data-remove-item aria-label="Remover item">Remover</button></td>`;
      row.querySelector("[name$='.ProductLabel']").value = `${product.code} · ${product.description} · ${product.unit}`;
      row.querySelector("[data-price]").value = String(product.unitPrice).replace(".", ",");
      const warehouse = row.querySelector("select");
      if (product.defaultWarehouseId) warehouse.value = product.defaultWarehouseId;
      body.appendChild(row);
      picker.hidden = true;
      status.textContent = `${product.description} adicionado.`;
      calculate();
    };

    let timer;
    search?.addEventListener("input", () => {
      clearTimeout(timer);
      status.textContent = "Buscando produtos…";
      timer = setTimeout(async () => {
        try {
          const table = root.querySelector("[name=PriceTableId]").value;
          const response = await fetch(`${root.dataset.productsUrl}?q=${encodeURIComponent(search.value)}&priceTableId=${encodeURIComponent(table)}`);
          const products = response.ok ? await response.json() : [];
          results.replaceChildren(...products.map(product => {
            const button = document.createElement("button");
            button.type = "button";
            button.setAttribute("role", "option");
            button.innerHTML = `<strong></strong><span></span><small></small>`;
            button.querySelector("strong").textContent = product.code;
            button.querySelector("span").textContent = product.description;
            button.querySelector("small").textContent = `${product.unit} · ${money(product.unitPrice)}`;
            button.addEventListener("click", () => add(product), { once: true });
            return button;
          }));
          status.textContent = products.length ? `${products.length} produto(s) encontrado(s).` : "Nenhum produto encontrado.";
        } catch {
          results.replaceChildren();
          status.textContent = "Não foi possível buscar produtos. Tente novamente.";
        }
      }, 250);
    });

    root.addEventListener("click", event => {
      if (event.target.closest("[data-add-item]")) {
        picker.hidden = false;
        status.textContent = "";
        search.focus();
        search.dispatchEvent(new Event("input"));
        return;
      }
      if (event.target.closest("[data-close-picker]")) { picker.hidden = true; return; }
      const row = event.target.closest("[data-item-row]");
      const input = row?.querySelector("[data-quantity]");
      if (event.target.closest("[data-quantity-increase]")) setQuantity(input, clampQuantity(input.value) + 1);
      if (event.target.closest("[data-quantity-decrease]")) setQuantity(input, clampQuantity(input.value) - 1);
      const remove = event.target.closest("[data-remove-item]");
      if (remove && window.confirm("Remover este item do orçamento?")) {
        remove.closest("[data-item-row]").remove();
        reindex();
        calculate();
      }
    });

    root.addEventListener("keydown", event => {
      if (!event.target.matches("[data-quantity]") || !["ArrowUp", "ArrowDown"].includes(event.key)) return;
      event.preventDefault();
      setQuantity(event.target, clampQuantity(event.target.value) + (event.key === "ArrowUp" ? 1 : -1));
    });
    root.addEventListener("input", event => {
      if (event.target.matches("[data-quantity],[data-price],[data-discount],[data-document-discount],[data-freight],[data-charges]")) calculate();
    });
    root.addEventListener("change", event => {
      if (!event.target.matches("[data-quantity]")) return;
      const quantity = parseDecimal(event.target.value);
      if (quantity < 1) {
        event.target.setCustomValidity("A quantidade mínima é 1.");
        setQuantity(event.target, 1);
      } else {
        setQuantity(event.target, quantity);
      }
    });
    calculate();
  };

  window.OrizonQuoteEditor = Object.freeze({ parseDecimal, clampQuantity, roundMoney, initialize });
  document.querySelectorAll("[data-quote-editor]").forEach(initialize);
})();
