(() => {
    document.querySelectorAll("[data-billing-report-filter]").forEach(form => {
        const fromInput = form.querySelector('[name="FromDateMyt"]');
        const toInput = form.querySelector('[name="ToDateMyt"]');

        if (!fromInput || !toInput) {
            return;
        }

        const validateRange = () => {
            toInput.min = fromInput.value;
            const invalid = fromInput.value && toInput.value && toInput.value < fromInput.value;
            toInput.setCustomValidity(invalid
                ? "The To date must be on or after the From date."
                : "");
        };

        fromInput.addEventListener("change", validateRange);
        toInput.addEventListener("change", validateRange);
        validateRange();
    });
})();
