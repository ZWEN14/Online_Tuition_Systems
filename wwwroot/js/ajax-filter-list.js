(() => {
    "use strict";

    document.addEventListener("DOMContentLoaded", () => {
        const form = document.querySelector("[data-ajax-list-form]");
        const results = document.querySelector("[data-ajax-list-results]");

        if (!form || !results) {
            return;
        }

        const pageInput = form.querySelector("[data-page-input]");
        const searchInput = form.querySelector("[data-search-input]");
        const resetButton = form.querySelector("[data-ajax-reset]");
        const status = form.querySelector("[data-ajax-status]");
        const ajaxUrl = form.dataset.ajaxUrl;
        const indexUrl = form.dataset.indexUrl;
        const defaultSort = form.dataset.defaultSort;
        let debounceTimer;
        let activeRequest;

        const loadResults = async () => {
            activeRequest?.abort();
            activeRequest = new AbortController();

            const parameters = new URLSearchParams(new FormData(form));
            status.textContent = "Loading...";
            results.setAttribute("aria-busy", "true");

            try {
                const response = await fetch(`${ajaxUrl}?${parameters}`, {
                    headers: { "X-Requested-With": "XMLHttpRequest" },
                    signal: activeRequest.signal
                });

                if (!response.ok) {
                    throw new Error("The filtered list could not be loaded.");
                }

                results.innerHTML = await response.text();
                history.replaceState(null, "", `${indexUrl}?${parameters}`);
                status.textContent = "Updated";
            } catch (error) {
                if (error.name !== "AbortError") {
                    status.textContent = error.message;
                }
            } finally {
                results.removeAttribute("aria-busy");
            }
        };

        const startNewSearch = () => {
            pageInput.value = "1";
            loadResults();
        };

        form.addEventListener("submit", event => {
            event.preventDefault();
            startNewSearch();
        });

        form.addEventListener("change", event => {
            if (event.target.matches("select")) {
                startNewSearch();
            }
        });

        searchInput?.addEventListener("input", () => {
            clearTimeout(debounceTimer);
            debounceTimer = setTimeout(startNewSearch, 350);
        });

        resetButton?.addEventListener("click", () => {
            for (const element of form.elements) {
                if (element.name === "Page") {
                    element.value = "1";
                } else if (element.name === "Sort") {
                    element.value = defaultSort;
                } else if (element.matches("input[type='text'], input[type='search'], select")) {
                    element.value = "";
                }
            }

            loadResults();
        });

        results.addEventListener("click", event => {
            const pageLink = event.target.closest("[data-ajax-page]");

            if (!pageLink) {
                return;
            }

            event.preventDefault();
            pageInput.value = pageLink.dataset.ajaxPage;
            loadResults();
        });
    });
})();
