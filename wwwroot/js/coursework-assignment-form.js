(() => {
    "use strict";

    if (window.bootstrap?.Tooltip) {
        document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach((element) => {
            window.bootstrap.Tooltip.getOrCreateInstance(element);
        });
    }

    const form = document.querySelector("[data-assignment-form]");
    if (!form) return;

    const attachmentButton = form.querySelector("[data-attachment-toggle]");
    const attachmentPanel = form.querySelector("[data-attachment-panel]");
    attachmentButton?.addEventListener("click", () => {
        const willShow = attachmentPanel.hidden;
        attachmentPanel.hidden = !willShow;
        attachmentButton.classList.toggle("active", willShow);
        attachmentButton.setAttribute("aria-expanded", willShow ? "true" : "false");
        if (willShow) attachmentPanel.querySelector('input[type="file"]')?.focus();
    });

    const marksPanel = form.querySelector("[data-marks-panel]");
    const marksInput = marksPanel?.querySelector("input");
    const gradingInputs = form.querySelectorAll('input[name="IsGraded"]');
    const updateGrading = () => {
        const graded = form.querySelector('input[name="IsGraded"]:checked')?.value === "true";
        if (!marksPanel || !marksInput) return;
        marksPanel.hidden = !graded;
        marksInput.disabled = !graded;
    };
    gradingInputs.forEach((input) => input.addEventListener("change", updateGrading));
    updateGrading();

    const publishingInputs = form.querySelectorAll('input[name="PublishingMode"]');
    const submitButton = document.querySelector("[data-assignment-submit]");
    const updateSubmitLabel = () => {
        const publish = form.querySelector('input[name="PublishingMode"]:checked')?.value === "Publish";
        if (submitButton && submitButton.dataset.editing !== "true") {
            submitButton.textContent = publish ? "Publish assignment" : "Save draft";
        }
    };
    publishingInputs.forEach((input) => input.addEventListener("change", updateSubmitLabel));
    updateSubmitLabel();
})();
