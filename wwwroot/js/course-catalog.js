(() => {
    const form = document.getElementById("courseCatalogFilters");
    const results = document.getElementById("courseCatalogResults");
    const resultCount = document.getElementById("courseResultCount");
    const status = document.getElementById("courseCatalogStatus");
    const error = document.getElementById("courseCatalogError");
    const loadingIndicator = document.getElementById("courseSearchLoading");
    const searchInput = document.getElementById("Search");
    const categorySelect = document.getElementById("CategoryId");
    const sortSelect = document.getElementById("Sort");

    if (!form || !results || !resultCount || !status || !error || !loadingIndicator) {
        return;
    }

    let activeRequest;
    let searchTimer;

    function buildUrl(page = 1) {
        const url = new URL(form.action, window.location.origin);
        const parameters = new URLSearchParams(new FormData(form));

        parameters.delete("Page");
        parameters.delete("page");
        if (page > 1) {
            parameters.set("page", page.toString());
        }

        if (parameters.get("Sort") === "newest") {
            parameters.delete("Sort");
        }

        for (const [key, value] of [...parameters.entries()]) {
            if (!value.trim()) {
                parameters.delete(key);
            }
        }

        url.search = parameters.toString();
        return url;
    }

    function setLoading(isLoading) {
        results.setAttribute("aria-busy", isLoading ? "true" : "false");
        results.classList.toggle("opacity-50", isLoading);
        loadingIndicator.classList.toggle("d-none", !isLoading);
    }

    function updateFormFromUrl(url) {
        const parameters = url.searchParams;
        if (searchInput) {
            searchInput.value = parameters.get("Search")
                ?? parameters.get("search")
                ?? "";
        }
        if (categorySelect) {
            categorySelect.value = parameters.get("CategoryId")
                ?? parameters.get("categoryId")
                ?? "";
        }
        if (sortSelect) {
            sortSelect.value = parameters.get("Sort")
                ?? parameters.get("sort")
                ?? "newest";
        }
    }

    async function loadCourses(url, historyMode) {
        activeRequest?.abort();
        const request = new AbortController();
        activeRequest = request;
        setLoading(true);
        error.classList.add("d-none");

        try {
            const response = await fetch(url, {
                headers: { "X-Requested-With": "XMLHttpRequest" },
                signal: request.signal
            });

            if (!response.ok) {
                throw new Error(`Course request failed with status ${response.status}.`);
            }

            results.innerHTML = await response.text();
            const catalog = results.querySelector("[data-course-catalog-results]");
            const total = Number(catalog?.dataset.totalCourses ?? 0);
            resultCount.textContent = `${total} course(s)`;
            status.textContent = `${total} course results loaded.`;

            if (historyMode === "push") {
                window.history.pushState({}, "", url);
            } else if (historyMode === "replace") {
                window.history.replaceState({}, "", url);
            }
        } catch (requestError) {
            if (requestError.name !== "AbortError") {
                error.textContent =
                    "Courses could not be refreshed. Your current results are still shown.";
                error.classList.remove("d-none");
                status.textContent = "Course results could not be refreshed.";
            }
        } finally {
            if (activeRequest === request) {
                setLoading(false);
            }
        }
    }

    form.addEventListener("submit", event => {
        event.preventDefault();
        window.clearTimeout(searchTimer);
        loadCourses(buildUrl(), "push");
    });

    searchInput?.addEventListener("input", () => {
        window.clearTimeout(searchTimer);
        searchTimer = window.setTimeout(() => {
            loadCourses(buildUrl(), "replace");
        }, 450);
    });

    [categorySelect, sortSelect].forEach(select => {
        select?.addEventListener("change", () => {
            window.clearTimeout(searchTimer);
            loadCourses(buildUrl(), "push");
        });
    });

    results.addEventListener("click", event => {
        const pageLink = event.target.closest(".pagination a");
        if (!pageLink) {
            return;
        }

        event.preventDefault();
        loadCourses(new URL(pageLink.href), "push");
        results.scrollIntoView({ behavior: "smooth", block: "start" });
    });

    window.addEventListener("popstate", () => {
        const url = new URL(window.location.href);
        updateFormFromUrl(url);
        loadCourses(url, null);
    });
})();
