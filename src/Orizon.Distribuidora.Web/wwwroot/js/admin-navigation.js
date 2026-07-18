(() => {
    "use strict";

    const shell = document.querySelector("[data-admin-layout]");
    if (!shell) return;

    const storage = {
        collapsed: "orizon.admin.sidebar.collapsed",
        groups: "orizon.admin.sidebar.groups",
        lastPage: "orizon.admin.lastPage"
    };
    const desktop = window.matchMedia("(min-width: 1024px)");
    const safeRead = (key, fallback = null) => {
        try { return localStorage.getItem(key) ?? fallback; } catch { return fallback; }
    };
    const safeWrite = (key, value) => {
        try { localStorage.setItem(key, value); } catch { /* Storage can be unavailable. */ }
    };

    if (desktop.matches && safeRead(storage.collapsed) === "true") {
        shell.classList.add("is-sidebar-collapsed");
    }

    const savedGroups = new Set((safeRead(storage.groups, "") || "").split(",").filter(Boolean));
    document.querySelectorAll("[data-sidebar-group]").forEach(group => {
        const key = group.dataset.sidebarGroup;
        if (savedGroups.has(key)) group.classList.add("is-expanded");
        const toggle = group.querySelector("[data-sidebar-group-toggle]");
        toggle?.setAttribute("aria-expanded", String(group.classList.contains("is-expanded")));
        toggle?.addEventListener("click", () => {
            group.classList.toggle("is-expanded");
            toggle.setAttribute("aria-expanded", String(group.classList.contains("is-expanded")));
            const expanded = [...document.querySelectorAll("[data-sidebar-group].is-expanded")].map(item => item.dataset.sidebarGroup);
            safeWrite(storage.groups, expanded.join(","));
        });
    });

    const syncCollapsedState = () => {
        const collapsed = shell.classList.contains("is-sidebar-collapsed");
        safeWrite(storage.collapsed, String(collapsed));
        const button = document.querySelector("[data-sidebar-collapse]");
        button?.setAttribute("aria-expanded", String(!collapsed));
        if (button) button.setAttribute("aria-label", collapsed ? "Expandir menu lateral" : "Recolher menu lateral");
    };
    document.querySelectorAll("[data-sidebar-collapse], [data-sidebar-toggle]").forEach(button =>
        button.addEventListener("click", () => window.setTimeout(syncCollapsedState, 0)));

    if (location.pathname.startsWith("/Admin/")) safeWrite(storage.lastPage, location.pathname + location.search);
    const lastPage = safeRead(storage.lastPage);
    if (lastPage?.startsWith("/Admin/")) document.querySelector("[data-last-page-link]")?.setAttribute("href", lastPage);

    document.querySelectorAll(".orizon-sidebar-sublink, .orizon-sidebar-navigation > .orizon-sidebar-link").forEach(link => {
        link.addEventListener("click", () => {
            if (!desktop.matches) shell.classList.remove("is-sidebar-open");
        });
    });

    const userToggle = document.querySelector("[data-user-menu-toggle]");
    const userPanel = document.querySelector(".orizon-user-menu-panel");
    const closeUserMenu = () => {
        if (!userPanel) return;
        userPanel.hidden = true;
        userToggle?.setAttribute("aria-expanded", "false");
    };
    userToggle?.addEventListener("click", event => {
        event.stopPropagation();
        if (!userPanel) return;
        userPanel.hidden = !userPanel.hidden;
        userToggle.setAttribute("aria-expanded", String(!userPanel.hidden));
    });
    document.addEventListener("click", closeUserMenu);
    userPanel?.addEventListener("click", event => event.stopPropagation());

    document.addEventListener("keydown", event => {
        if (event.key === "/" && !event.ctrlKey && !event.metaKey && !event.altKey) {
            const search = document.querySelector(".orizon-topbar-search input");
            if (search && !search.disabled) { event.preventDefault(); search.focus(); }
        }
        if (event.key === "Escape") closeUserMenu();
    });
})();
