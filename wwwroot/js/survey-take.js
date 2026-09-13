// Survey answering: display one section and follow its configured destination.
// The controller checks the same route again before saving any response.
(() => {
    'use strict';
    const form = document.getElementById('section-survey-form');
    if (!form) return;
    const sections = [...form.querySelectorAll('.survey-section')];
    if (sections.length === 0) return;
    const history = [];
    let current = sections[0];
    const GO_TO_SECTION = '2';
    const SUBMIT = '3';

    // Only the visible section participates in browser required-field checks.
    function setRequirements(section, enabled) {
        section.querySelectorAll('.survey-question').forEach(question => {
            const controls = [...question.querySelectorAll('.answer-control')];
            controls.forEach(control => {
                control.required = false;
                control.setCustomValidity('');
            });
            if (!enabled || question.dataset.required !== 'true') return;
            const checks = controls.filter(control => control.type === 'checkbox');
            if (checks.length > 0) {
                if (!checks.some(control => control.checked))
                    checks[0].setCustomValidity('Select at least one option.');
            } else if (controls.length > 0) {
                controls[0].required = true;
            }
        });
    }

    // A selected option can override the default destination below the section.
    function selectedRoute() {
        const dropdowns = [...current.querySelectorAll('select.routing-answer')];
        const selected = dropdowns.map(control => control.selectedOptions[0]);
        selected.push(...current.querySelectorAll('.routing-answer input[type="radio"]:checked'));
        const option = selected.find(item => item?.value &&
            [GO_TO_SECTION, SUBMIT].includes(item.dataset.action));
        const source = option || current;
        return { action: source.dataset.action, destination: source.dataset.destination };
    }

    function nextSection() {
        return sections.find(section => Number(section.dataset.order) > Number(current.dataset.order));
    }

    function updateNavigation() {
        const route = selectedRoute();
        const ends = route.action === SUBMIT || (!nextSection() && route.action !== GO_TO_SECTION);
        current.querySelector('.back-section').disabled = history.length <= 1;
        current.querySelector('.next-section').hidden = ends;
        current.querySelector('.submit-survey').hidden = !ends;
    }

    function show(section, addToHistory = true) {
        sections.forEach(item => {
            item.hidden = item !== section;
            setRequirements(item, item === section);
        });
        current = section;
        if (addToHistory && history.at(-1) !== section) history.push(section);
        updateNavigation();
        document.getElementById('survey-progress').style.width =
            `${Math.round(history.length / sections.length * 100)}%`;
        window.scrollTo({ top: 0, behavior: 'smooth' });
    }

    function validate() {
        setRequirements(current, true);
        return [...current.querySelectorAll('input,select,textarea')]
            .every(control => control.reportValidity());
    }

    function next() {
        if (!validate()) return;
        const route = selectedRoute();
        if (route.action === SUBMIT) {
            form.requestSubmit();
            return;
        }
        let destination = nextSection();
        if (route.action === GO_TO_SECTION)
            destination = sections.find(section => section.dataset.sectionId === route.destination);
        if (destination) show(destination);
        else form.requestSubmit();
    }

    form.addEventListener('change', () => {
        setRequirements(current, true);
        updateNavigation();
    });
    form.addEventListener('submit', event => {
        if (!validate()) event.preventDefault();
    });
    form.querySelectorAll('.next-section').forEach(button => button.addEventListener('click', next));
    form.querySelectorAll('.back-section').forEach(button => button.addEventListener('click', () => {
        if (history.length <= 1) return;
        history.pop();
        show(history.at(-1), false);
    }));
    show(current);
})();
