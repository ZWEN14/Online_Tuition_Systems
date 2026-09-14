(() => {
    "use strict";

    const page = document.body;
    const toggle = document.querySelector("[data-course-theme-toggle]");
    if (!page.classList.contains("course-module-page")) return;

    const storageKey = "course-module-theme";

    const applyTheme = dark => {
        page.classList.toggle("course-theme-dark", dark);
        if (toggle) {
            const action = dark ? "Switch to light theme" : "Switch to dark theme";
            toggle.setAttribute("aria-pressed", String(dark));
            toggle.setAttribute("aria-label", action);
            toggle.setAttribute("title", action);
        }
    };

    let dark = document.documentElement.classList.contains("course-theme-pre-dark");
    try {
        dark = window.sessionStorage.getItem(storageKey) === "dark";
    } catch {
        // Continue with the theme selected before the page rendered.
    }

    applyTheme(dark);
    document.documentElement.classList.remove("course-theme-pre-dark");

    if (toggle) {
        toggle.addEventListener("click", () => {
            dark = !page.classList.contains("course-theme-dark");
            applyTheme(dark);
            try {
                window.sessionStorage.setItem(storageKey, dark ? "dark" : "light");
            } catch {
                // The current page can still change theme without persistence.
            }
        });
    }
})();
