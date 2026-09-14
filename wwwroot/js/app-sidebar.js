(() => {
    "use strict";

    const shell = document.querySelector(".app-shell");
    const sidebar = shell?.querySelector(":scope > .app-sidebar");
    if (!shell || !sidebar || sidebar.dataset.enhanced === "true") return;

    sidebar.dataset.enhanced = "true";
    shell.classList.add("sidebar-enhanced");

    const normalizePath = (value) => value.replace(/\/+$/, "").toLowerCase() || "/";
    const currentPath = normalizePath(window.location.pathname);
    const sidebarSection = (shell.dataset.sidebarSection
        || shell.dataset.currentController
        || "").toLowerCase();
    const sidebarLinks = [...sidebar.querySelectorAll("a.sidebar-link")];
    let currentLink = null;
    let currentLinkScore = -1;

    for (const link of sidebarLinks) {
        const path = normalizePath(new URL(link.href, window.location.origin).pathname);
        const linkController = path.split("/").filter(Boolean)[0] || "";
        let matchScore = linkController === sidebarSection ? 1000 : -1;

        if (path !== "/" && currentPath.startsWith(`${path}/`)) {
            matchScore = 2000 + path.length;
        }
        if (currentPath === path) {
            matchScore = 3000 + path.length;
        }

        if (matchScore > currentLinkScore) {
            currentLink = link;
            currentLinkScore = matchScore;
        }
    }

    if (currentLink) {
        currentLink.classList.add("shell-sidebar-current");
        currentLink.setAttribute("aria-current", "page");
    }

    const controls = sidebar.querySelector(":scope > .shell-sidebar-controls");
    if (!controls) return;

    const toggles = [...controls.querySelectorAll("button")];
    const storageKey = "app-sidebar-collapsed";
    let transitionTimer = null;

    const updateState = (collapsed) => {
        sidebar.classList.toggle("shell-sidebar-collapsed", collapsed);
        for (const toggle of toggles) {
            toggle.setAttribute("aria-expanded", String(!collapsed));
            toggle.title = collapsed ? "Open Quick Access" : "Close Quick Access";
        }
    };

    let collapsed = document.documentElement.classList.contains("shell-sidebar-precollapsed");
    try {
        collapsed = window.sessionStorage.getItem(storageKey) === "true";
    } catch {
        // The sidebar remains usable when browser storage is unavailable.
    }
    updateState(collapsed);
    document.documentElement.classList.remove("shell-sidebar-precollapsed");

    for (const toggle of toggles) {
        toggle.addEventListener("click", () => {
            const nextState = !sidebar.classList.contains("shell-sidebar-collapsed");
            sidebar.classList.add("shell-sidebar-transitioning");
            updateState(nextState);

            window.clearTimeout(transitionTimer);
            const transitionDuration = window.matchMedia("(prefers-reduced-motion: reduce)").matches
                ? 0
                : 150;
            transitionTimer = window.setTimeout(() => {
                sidebar.classList.remove("shell-sidebar-transitioning");
            }, transitionDuration);

            try {
                window.sessionStorage.setItem(storageKey, String(nextState));
            } catch {
                // Keep the current-page interaction even without persistence.
            }
        });
    }
})();
