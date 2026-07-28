(() => {
    "use strict";

    const root = document.querySelector("[data-dashboard]");
    const dataElement = document.querySelector("#dashboard-data");
    if (!root || !dataElement) return;

    let payload;
    try {
        payload = JSON.parse(dataElement.textContent);
    } catch {
        return;
    }

    const money = new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL", maximumFractionDigits: 0 });
    const number = new Intl.NumberFormat("pt-BR");
    const percent = new Intl.NumberFormat("pt-BR", { minimumFractionDigits: 1, maximumFractionDigits: 1 });
    const colors = [
        "var(--orizon-chart-1)", "var(--orizon-chart-2)", "var(--orizon-chart-3)",
        "var(--orizon-chart-4)", "var(--orizon-chart-5)"
    ];
    let currentKey = "30days";

    const escapeText = value => String(value)
        .replaceAll("&", "&amp;").replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;").replaceAll("\"", "&quot;");

    function points(values, width, height, padding) {
        const max = Math.max(...values, 1);
        const min = Math.min(...values, 0);
        const range = Math.max(max - min, 1);
        return values.map((value, index) => ({
            x: padding + index * ((width - padding * 2) / Math.max(values.length - 1, 1)),
            y: padding + (max - value) / range * (height - padding * 2)
        }));
    }

    function linePath(items) {
        if (!items.length) return "";
        return items.map((item, index) => `${index ? "L" : "M"} ${item.x.toFixed(1)} ${item.y.toFixed(1)}`).join(" ");
    }

    function sparkline(values) {
        const chartPoints = points(values, 120, 40, 3);
        return `<svg class="dashboard-sparkline-svg" viewBox="0 0 120 40" preserveAspectRatio="none" focusable="false"><path d="${linePath(chartPoints)}"/></svg>`;
    }

    function renderKpis(period) {
        const values = {
            revenue: [money.format(period.revenue), period.revenueChange, period.revenueSeries],
            sales: [number.format(period.sales), period.salesChange, period.salesSeries],
            ticket: [money.format(period.averageTicket), null, period.revenueSeries],
            receivable: [money.format(period.receivable), null, period.revenueSeries]
        };

        Object.entries(values).forEach(([key, value]) => {
            const card = root.querySelector(`[data-demo-kpi="${key}"]`);
            if (!card) return;
            card.querySelector("[data-value]").textContent = value[0];
            const change = card.querySelector("[data-change]");
            if (change && value[1] !== null) {
                const positive = value[1] >= 0;
                change.textContent = `${positive ? "↑" : "↓"} ${percent.format(Math.abs(value[1]))}%`;
                card.classList.toggle("orizon-metric-card--positive", positive);
                card.classList.toggle("orizon-metric-card--negative", !positive);
            }
            const spark = card.querySelector("[data-sparkline]");
            if (spark) spark.innerHTML = sparkline(value[2]);
        });

        const receivableContext = root.querySelector('[data-demo-kpi="receivable"] [data-context]');
        if (receivableContext) {
            receivableContext.innerHTML = `<b>${money.format(period.overdue)}</b> vencido no cenário demonstrativo`;
        }
    }

    function renderCommercial(period) {
        const host = root.querySelector('[data-chart="commercial"]');
        if (!host) return;
        const width = 760;
        const height = 270;
        const paddingX = 48;
        const paddingY = 28;
        const all = [...period.revenueSeries, ...period.previousRevenueSeries];
        const max = Math.max(...all, 1);
        const coordinates = series => series.map((value, index) => ({
            x: paddingX + index * ((width - paddingX * 2) / Math.max(series.length - 1, 1)),
            y: paddingY + (1 - value / max) * (height - paddingY * 2)
        }));
        const current = coordinates(period.revenueSeries);
        const previous = coordinates(period.previousRevenueSeries);
        const salesMax = Math.max(...period.salesSeries, 1);
        const sales = period.salesSeries.map((value, index) => ({
            x: paddingX + index * ((width - paddingX * 2) / Math.max(period.salesSeries.length - 1, 1)),
            y: paddingY + (1 - value / salesMax) * (height - paddingY * 2)
        }));
        const area = `${linePath(current)} L ${current.at(-1).x} ${height - paddingY} L ${current[0].x} ${height - paddingY} Z`;
        const grid = [0, .25, .5, .75, 1].map(ratio => {
            const y = paddingY + ratio * (height - paddingY * 2);
            const label = money.format(max * (1 - ratio));
            return `<line class="grid" x1="${paddingX}" x2="${width - paddingX}" y1="${y}" y2="${y}"/><text x="0" y="${y + 4}">${escapeText(label)}</text>`;
        }).join("");
        const labels = period.labels.map((label, index) =>
            `<text text-anchor="middle" x="${current[index].x}" y="${height - 4}">${escapeText(label)}</text>`).join("");
        const dots = current.map((point, index) =>
            `<circle class="point" cx="${point.x}" cy="${point.y}" r="5" tabindex="0" data-series="0" data-index="${index}" data-label="${escapeText(period.labels[index])}" data-value="${escapeText(money.format(period.revenueSeries[index]))}"><title>${escapeText(period.labels[index])}: ${escapeText(money.format(period.revenueSeries[index]))}</title></circle>`).join("");
        const salesDots = sales.map((point, index) =>
            `<circle class="point sales" cx="${point.x}" cy="${point.y}" r="4" tabindex="0" data-series="1" data-index="${index}" data-label="${escapeText(period.labels[index])}" data-value="${escapeText(number.format(period.salesSeries[index]))} vendas"><title>${escapeText(period.labels[index])}: ${number.format(period.salesSeries[index])} vendas</title></circle>`).join("");

        host.innerHTML = `<figure class="orizon-chart" data-orizon-chart data-labels='${escapeText(JSON.stringify(period.labels))}' data-series='[{"name":"Faturamento"},{"name":"Vendas"}]'>
            <div class="dashboard-chart-legend"><span><i style="--legend-color:var(--orizon-color-primary)"></i>Faturamento atual</span><span><i style="--legend-color:var(--orizon-color-success)"></i>Quantidade de vendas</span><span><i style="--legend-color:var(--orizon-color-text-muted)"></i>Faturamento anterior</span></div>
            <div class="orizon-chart__canvas"><svg class="dashboard-svg-chart" viewBox="0 0 ${width} ${height}" preserveAspectRatio="none" role="img" aria-label="Evolução do faturamento e quantidade de vendas em ${escapeText(period.label)}">${grid}<path class="area" d="${area}"/><path class="line previous" d="${linePath(previous)}"/><path class="line" d="${linePath(current)}"/><path class="line sales" d="${linePath(sales)}"/>${dots}${salesDots}${labels}</svg><div class="orizon-chart-tooltip" hidden></div></div>
        </figure>`;
        root.querySelector('[data-chart-summary="commercial"]').textContent =
            `No cenário demonstrativo, ${period.label.toLowerCase()} totaliza ${money.format(period.revenue)} em ${number.format(period.sales)} vendas.`;
    }

    function renderComposition(period) {
        const host = root.querySelector('[data-chart="composition"]');
        if (!host) return;
        const total = period.salesComposition.reduce((sum, item) => sum + item.value, 0);
        let offset = 0;
        const stops = period.salesComposition.map((item, index) => {
            const start = offset;
            offset += item.value / total * 100;
            return `${colors[index]} ${start}% ${offset}%`;
        }).join(",");
        host.innerHTML = `<figure class="orizon-chart" data-orizon-chart><div class="dashboard-doughnut">
            <div class="dashboard-doughnut__graphic" style="background:conic-gradient(${stops})" data-center="${escapeText(money.format(total))}" role="img" aria-label="Composição demonstrativa das vendas por categoria"></div>
            <div class="dashboard-doughnut__legend">${period.salesComposition.map((item, index) => `<div title="${escapeText(item.label)}"><i style="background:${colors[index]}"></i><span>${escapeText(item.label)}</span><b>${percent.format(item.value / total * 100)}%</b></div>`).join("")}</div>
        </div></figure>`;
        const leader = period.salesComposition[0];
        root.querySelector('[data-chart-summary="composition"]').textContent =
            `${leader.label} lidera o cenário, com ${percent.format(leader.value / total * 100)}% do faturamento demonstrativo.`;
    }

    function horizontalBars(items, valueFormatter) {
        if (!items.length) return `<div class="orizon-chart-empty-state"><strong>Sem dados disponíveis</strong><p>Não há itens para este período.</p></div>`;
        const max = Math.max(...items.map(item => item.value), 1);
        return `<div class="dashboard-horizontal-bars">${items.map(item => `<div class="dashboard-horizontal-bar"><span title="${escapeText(item.label)}">${escapeText(item.label)}</span><span class="dashboard-horizontal-bar__track"><i style="--bar-width:${item.value / max * 100}%"></i></span><b>${escapeText(valueFormatter(item.value))}</b></div>`).join("")}</div>`;
    }

    function renderRanking(period) {
        const host = root.querySelector('[data-chart="ranking"]');
        if (!host) return;
        host.innerHTML = horizontalBars(period.topProducts, money.format);
        const first = period.topProducts[0];
        root.querySelector('[data-chart-summary="ranking"]').textContent =
            first ? `${first.label} ocupa a primeira posição, com ${money.format(first.value)} no cenário demonstrativo.` : "Sem dados para o período.";
    }

    function renderCategories() {
        const host = root.querySelector('[data-chart="categories"]');
        if (!host) return;
        host.innerHTML = horizontalBars(payload.categories.map(item => ({ label: item.name, value: item.count })), number.format);
        const total = payload.categories.reduce((sum, item) => sum + item.count, 0);
        root.querySelector('[data-chart-summary="categories"]').textContent =
            total ? `${number.format(total)} produtos ativos distribuídos entre as principais categorias reais.` : "Ainda não há produtos ativos categorizados.";
    }

    function renderFinance(period) {
        const host = root.querySelector("[data-finance]");
        if (!host) return;
        const items = [
            ["Recebido", period.received, "var(--orizon-color-success)"],
            ["A receber", period.receivable, "var(--orizon-color-primary)"],
            ["Vencido", period.overdue, "var(--orizon-color-danger)"]
        ];
        const total = items.reduce((sum, item) => sum + item[1], 0);
        host.innerHTML = `<div class="dashboard-finance__total"><span>Movimentação demonstrativa<br><strong>${money.format(total)}</strong></span><span>${escapeText(period.label)}</span></div>
            <div class="dashboard-finance__bar" aria-label="Composição financeira demonstrativa">${items.map(item => `<i style="width:${item[1] / total * 100}%;background:${item[2]}" title="${escapeText(item[0])}: ${escapeText(money.format(item[1]))}"></i>`).join("")}</div>
            <div class="dashboard-finance__items">${items.map(item => `<div class="dashboard-finance__item"><span>${escapeText(item[0])}</span><strong>${escapeText(money.format(item[1]))}</strong></div>`).join("")}</div>`;
    }

    function render(key) {
        const period = payload.periods[key] || payload.periods["30days"];
        renderKpis(period);
        renderCommercial(period);
        renderComposition(period);
        renderRanking(period);
        renderFinance(period);
        renderCategories();
    }

    function showToast(message) {
        const toast = root.querySelector("[data-toast]");
        toast.textContent = message;
        toast.hidden = false;
        window.setTimeout(() => { toast.hidden = true; }, 2600);
    }

    const periodSelect = root.querySelector("[data-period]");
    const customPanel = root.querySelector("[data-custom-period]");
    periodSelect.addEventListener("change", () => {
        if (periodSelect.value === "custom") {
            customPanel.hidden = false;
            customPanel.querySelector("input")?.focus();
            return;
        }
        customPanel.hidden = true;
        currentKey = periodSelect.value;
        render(currentKey);
    });

    root.querySelector("[data-apply-custom]")?.addEventListener("click", () => {
        const start = root.querySelector("[data-date-start]").value;
        const end = root.querySelector("[data-date-end]").value;
        if (!start || !end || start > end) {
            showToast("Informe um intervalo de datas válido.");
            return;
        }
        currentKey = "30days";
        render(currentKey);
        customPanel.hidden = true;
        showToast("Intervalo aplicado ao cenário demonstrativo de 30 dias.");
    });

    root.querySelector("[data-refresh]")?.addEventListener("click", event => {
        const button = event.currentTarget;
        button.setAttribute("aria-busy", "true");
        render(currentKey);
        window.setTimeout(() => {
            button.removeAttribute("aria-busy");
            showToast("Dashboard atualizado.");
        }, window.matchMedia("(prefers-reduced-motion: reduce)").matches ? 0 : 350);
    });

    render(currentKey);
})();
