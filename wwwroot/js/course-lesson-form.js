(() => {
    "use strict";

    if (window.bootstrap?.Tooltip) {
        document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach((element) => {
            window.bootstrap.Tooltip.getOrCreateInstance(element);
        });
    }

    const lessonForm = document.querySelector("[data-lesson-form]");
    if (!lessonForm) {
        return;
    }

    const toggleResourcePanel = (type) => {
        const panel = lessonForm.querySelector(`[data-resource-panel="${type}"]`);
        const button = lessonForm.querySelector(`[data-resource-reveal="${type}"]`);
        if (!panel || !button) {
            return;
        }

        const willShow = panel.hidden;
        panel.hidden = !willShow;
        button.classList.toggle("active", willShow);
        button.setAttribute("aria-expanded", willShow ? "true" : "false");
        if (willShow) {
            panel.querySelector("input")?.focus();
        }
    };

    lessonForm.querySelectorAll("[data-resource-reveal]").forEach((button) => {
        button.addEventListener("click", () => {
            toggleResourcePanel(button.dataset.resourceReveal);
        });
    });

    const schedulePanel = lessonForm.querySelector("[data-schedule-panel]");
    const scheduleInput = schedulePanel?.querySelector("input");
    const publishingInputs = lessonForm.querySelectorAll('input[name="PublishingMode"]');
    const updatePublishingPanel = (moveFocus = false) => {
        const selected = lessonForm.querySelector('input[name="PublishingMode"]:checked');
        if (!schedulePanel || !selected) {
            return;
        }

        schedulePanel.hidden = selected.value !== "Schedule";
        if (scheduleInput) {
            scheduleInput.disabled = schedulePanel.hidden;
        }
        if (!schedulePanel.hidden && moveFocus) {
            scheduleInput?.focus();
        }
    };

    publishingInputs.forEach((input) => {
        input.addEventListener("change", () => updatePublishingPanel(true));
    });
    updatePublishingPanel();

})();
