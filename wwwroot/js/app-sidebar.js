(() => {
    "use strict";

    const shell = document.querySelector(".app-shell");
    const sidebar = shell?.querySelector(":scope > .app-sidebar");
    if (!shell || !sidebar || sidebar.dataset.enhanced === "true") return;

    sidebar.dataset.enhanced = "true";
    shell.classList.add("sidebar-enhanced");

    const icons = {
        // Bootstrap Icons "book", MIT licensed: https://icons.getbootstrap.com/icons/book/
        course: '<svg class="shell-course-icon" viewBox="0 0 16 16" aria-hidden="true"><path d="M1 2.828c.885-.37 2.154-.769 3.388-.893 1.33-.134 2.458.063 3.112.752v9.746c-.935-.53-2.12-.603-3.213-.493-1.18.12-2.37.461-3.287.811zm7.5-.141c.654-.689 1.782-.886 3.112-.752 1.234.124 2.503.523 3.388.893v9.923c-.918-.35-2.107-.692-3.287-.81-1.094-.111-2.278-.039-3.213.492zM8 1.783C7.015.936 5.587.81 4.287.94c-1.514.153-3.042.672-3.994 1.105A.5.5 0 0 0 0 2.5v11a.5.5 0 0 0 .707.455c.882-.4 2.303-.881 3.68-1.02 1.409-.142 2.59.087 3.223.877a.5.5 0 0 0 .78 0c.633-.79 1.814-1.019 3.222-.877 1.378.139 2.8.62 3.681 1.02A.5.5 0 0 0 16 13.5v-11a.5.5 0 0 0-.293-.455c-.952-.433-2.48-.952-3.994-1.105C10.413.809 8.985.936 8 1.783"/></svg>',
        collection: '<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M5 6.5h14v12H5z"/><path d="M8 3.5h8M8 21h8"/><path d="m10 10 4 2-4 2z"/></svg>',
        billing: '<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M6 3h12v18l-2-1.3L14 21l-2-1.3L10 21l-2-1.3L6 21z"/><path d="M9 8h6M9 12h6M9 16h3"/></svg>'
    };

    const normalizePath = (value) => value.replace(/\/+$/, "").toLowerCase() || "/";
    const currentPath = normalizePath(window.location.pathname);
    const sidebarLinks = [...sidebar.querySelectorAll("a.sidebar-link")];
    let currentLink = null;
    let currentLinkPathLength = -1;

    for (const link of sidebarLinks) {
        const path = normalizePath(new URL(link.href, window.location.origin).pathname);

        const matchesCurrentRoute = currentPath === path
            || (path !== "/" && currentPath.startsWith(`${path}/`));
        if (matchesCurrentRoute && path.length > currentLinkPathLength) {
            currentLink = link;
            currentLinkPathLength = path.length;
        }

        let icon = null;
        if (path === "/courses") icon = icons.course;
        else if (path === "/studentcourses" || path === "/tutorcourses") icon = icons.collection;
        else if (path === "/billing" || path === "/tutorbilling" || path === "/adminbilling") icon = icons.billing;

        if (!icon) continue;
        const iconContainer = link.querySelector(":scope > span:first-child");
        if (!iconContainer) continue;
        iconContainer.classList.add("shell-nav-icon");
        iconContainer.innerHTML = icon;
    }

    if (currentLink) {
        currentLink.classList.add("shell-sidebar-current");
        currentLink.setAttribute("aria-current", "page");
    }

    const controls = document.createElement("div");
    controls.className = "shell-sidebar-controls";
    controls.innerHTML = `
        <button type="button" class="shell-sidebar-toggle shell-sidebar-menu"
                aria-label="Toggle Quick Access sidebar">
            <svg viewBox="0 0 24 24" aria-hidden="true">
                <path d="M4 6h16M4 12h16M4 18h16"></path>
            </svg>
        </button>`;

    const arrow = document.createElement("button");
    arrow.type = "button";
    arrow.className = "shell-sidebar-toggle shell-sidebar-arrow";
    arrow.setAttribute("aria-label", "Collapse Quick Access sidebar");
    arrow.innerHTML = `
        <svg viewBox="0 0 24 24" aria-hidden="true">
            <path d="m14 6-6 6 6 6"></path>
        </svg>`;

    sidebar.prepend(controls);
    sidebar.append(arrow);

    const toggles = [...controls.querySelectorAll("button"), arrow];
    const storageKey = "app-sidebar-collapsed";

    const updateState = (collapsed) => {
        sidebar.classList.toggle("shell-sidebar-collapsed", collapsed);
        for (const toggle of toggles) {
            toggle.setAttribute("aria-expanded", String(!collapsed));
            toggle.title = collapsed ? "Open Quick Access" : "Close Quick Access";
        }
        arrow.setAttribute("aria-label", collapsed
            ? "Expand Quick Access sidebar"
            : "Collapse Quick Access sidebar");
    };

    let collapsed = false;
    try {
        collapsed = window.sessionStorage.getItem(storageKey) === "true";
    } catch {
        // The sidebar remains usable when browser storage is unavailable.
    }
    updateState(collapsed);

    for (const toggle of toggles) {
        toggle.addEventListener("click", () => {
            const nextState = !sidebar.classList.contains("shell-sidebar-collapsed");
            updateState(nextState);
            try {
                window.sessionStorage.setItem(storageKey, String(nextState));
            } catch {
                // Keep the current-page interaction even without persistence.
            }
        });
    }
})();
