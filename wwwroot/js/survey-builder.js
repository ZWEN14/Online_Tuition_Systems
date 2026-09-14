// Shared Create/Edit builder. Array positions become MVC form field indexes.
(() => {
    'use strict';
    const host = document.getElementById('section-builder');
    if (!host) return;
    const form = document.getElementById('survey-builder-form');
    const sections = JSON.parse(document.getElementById('survey-builder-data').textContent);
    const types = {1: 'Text', 2: 'Rating', 3: 'Multiple Choice', 4: 'Yes / No', 5: 'Checkbox / Multiple Select', 6: 'Dropdown', 7: 'Photo / File Upload'};
    const choice = q => [3, 5, 6].includes(Number(q.type));
    const routing = q => [3, 6].includes(Number(q.type));
    const key = () => crypto.randomUUID().replaceAll('-', '');
    const newQuestion = () => ({id: 0, clientKey: key(), text: '', type: 1, isRequired: true, options: []});
    function element(tag, className, text) {
        const node = document.createElement(tag);
        if (className) node.className = className;
        if (text != null) node.textContent = text;
        return node;
    }
    function hidden(parent, name, value) {
        const input = element('input'); input.type = 'hidden'; input.name = name; input.value = value ?? ''; parent.append(input); return input;
    }
    function field(parent, label, name, value, update, max, textarea = false) {
        const wrap = element('label', 'd-block mb-3 flex-grow-1');
        wrap.append(element('span', 'form-label', label));
        const input = element(textarea ? 'textarea' : 'input', 'form-control');
        input.name = name; input.value = value ?? ''; input.maxLength = max;
        input.required = label !== 'Description';
        if (textarea) input.rows = 2;
        input.addEventListener('input', () => update(input.value));
        wrap.append(input); parent.append(wrap); return input;
    }
    function button(parent, text, run) {
        const b = element('button', 'btn btn-sm btn-outline-secondary', text); b.type = 'button'; b.addEventListener('click', run); parent.append(b); return b;
    }
    // Offer only later sections so the survey cannot loop back on itself.
    function destination(parent, label, name, value, index, update) {
        const wrap = element('label', 'd-block mb-3 flex-grow-1'); wrap.append(element('span', 'form-label', label));
        const select = element('select', 'form-select'); select.name = name;
        const candidates = [['', label === 'After this answer' ? 'Use section setting' : 'Next section'], ...sections.slice(index + 1).map((s, j) => [s.clientKey, `Go to section ${index + j + 2}: ${s.title || 'Untitled'}`]), ['submit', 'Submit form']];
        if (value && !candidates.some(x => x[0] === value)) candidates.push([value, 'Choose a new destination (section removed or moved)']);
        candidates.forEach(([v, text]) => { const o = element('option', '', text); o.value = v; select.append(o); });
        select.value = value || '';
        select.addEventListener('change', () => { update(select.value); select.setCustomValidity(''); });
        if (value && !sections.slice(index + 1).some(x => x.clientKey === value) && value !== 'submit') select.setCustomValidity('Choose a later section or submit form.');
        wrap.append(select); parent.append(wrap);
    }
    // Update destination labels after the administrator renames a section.
    function refreshDestinationLabels() {
        host.querySelectorAll('select[name$="DestinationKey"] option').forEach(option => {
            const index = sections.findIndex(s => s.clientKey === option.value);
            if (index >= 0) option.textContent = `Go to section ${index + 1}: ${sections[index].title || 'Untitled'}`;
        });
    }
    // Rebuild the form after adding, removing or reordering sections/questions.
    function render() {
        host.replaceChildren();
        sections.forEach((s, si) => {
            const prefix = `Sections[${si}]`;
            const card = element('section', 'card mb-4 section-card');
            const header = element('div', 'card-header d-flex flex-wrap gap-2 justify-content-between align-items-center');
            header.append(element('strong', '', `Section ${si + 1} of ${sections.length}`));
            const actions = element('div', 'd-flex flex-wrap gap-2');
            button(actions, 'Move up', () => { [sections[si - 1], sections[si]] = [s, sections[si - 1]]; render(); }).disabled = si === 0;
            button(actions, 'Move down', () => { [sections[si + 1], sections[si]] = [s, sections[si + 1]]; render(); }).disabled = si === sections.length - 1;
            button(actions, 'Remove section', () => { if (confirm('Remove this section and its questions?')) { sections.splice(si, 1); render(); } }).disabled = sections.length === 1;
            header.append(actions); card.append(header);
            const body = element('div', 'card-body'); card.append(body);
            hidden(body, `${prefix}.Id`, s.id); hidden(body, `${prefix}.ClientKey`, s.clientKey);
            field(body, 'Section title', `${prefix}.Title`, s.title, v => { s.title = v; refreshDestinationLabels(); }, 150);
            field(body, 'Description', `${prefix}.Description`, s.description, v => s.description = v, 500);
            s.questions.forEach((q, qi) => {
                const qp = `${prefix}.Questions[${qi}]`;
                const question = element('div', 'border rounded p-3 mb-3 question-card');
                const top = element('div', 'd-flex justify-content-between mb-3');
                top.append(element('strong', '', `Question ${qi + 1}`));
                button(top, 'Remove question', () => { s.questions.splice(qi, 1); render(); }).disabled = s.questions.length === 1;
                question.append(top); hidden(question, `${qp}.Id`, q.id); hidden(question, `${qp}.ClientKey`, q.clientKey);
                field(question, 'Question', `${qp}.Text`, q.text, v => q.text = v, 500, true);
                const label = element('label', 'd-block mb-3'); label.append(element('span', 'form-label', 'Question type'));
                const type = element('select', 'form-select'); type.name = `${qp}.Type`;
                Object.entries(types).forEach(([v, text]) => { const option = element('option', '', text); option.value = v; type.append(option); });
                type.value = q.type; type.addEventListener('change', () => { q.type = Number(type.value); if (!choice(q)) q.options = []; else if (!q.options.length) q.options = [{id: 0, text: ''}, {id: 0, text: ''}]; if (!routing(q)) q.options.forEach(o => o.destinationKey = ''); render(); });
                label.append(type); question.append(label);
                if (choice(q)) {
                    q.options.forEach((o, oi) => {
                        const row = element('div', 'd-flex flex-wrap align-items-end gap-3');
                        hidden(row, `${qp}.Options[${oi}].Id`, o.id);
                        field(row, `Option ${oi + 1}`, `${qp}.Options[${oi}].Text`, o.text, v => o.text = v, 200);
                        if (routing(q)) destination(row, 'After this answer', `${qp}.Options[${oi}].DestinationKey`, o.destinationKey, si, v => o.destinationKey = v);
                        button(row, 'Remove option', () => { q.options.splice(oi, 1); render(); }).disabled = q.options.length <= 2;
                        question.append(row);
                    });
                    button(question, 'Add option', () => { q.options.push({id: 0, text: '', destinationKey: ''}); render(); }).disabled = q.options.length >= 50;
                    if (routing(q)) question.append(element('p', 'small text-muted mt-2', 'Choose a destination beside each option. Option text alone does not set a route. Use section setting follows the destination below.'));
                }
                if (Number(q.type) === 7) question.append(element('p', 'text-muted', 'Respondents can upload up to 5 JPG, PNG, WebP, PDF, or TXT files, up to 5 MB each.'));
                const required = element('label', 'd-flex align-items-center gap-2 mt-3');
                const check = element('input', 'form-check-input'); check.type = 'checkbox'; check.checked = q.isRequired;
                const value = hidden(question, `${qp}.IsRequired`, String(q.isRequired));
                check.addEventListener('change', () => { q.isRequired = check.checked; value.value = String(check.checked); });
                required.append(check, document.createTextNode('Required')); question.append(required); body.append(question);
            });
            button(body, 'Add question', () => { s.questions.push(newQuestion()); render(); });
            const footer = element('div', 'card-footer');
            destination(footer, 'After this section', `${prefix}.DestinationKey`, s.destinationKey, si, v => s.destinationKey = v);
            footer.append(element('small', 'text-muted', 'An answer with a specific destination or Submit form overrides this setting.'));
            card.append(footer); host.append(card);
        });
    }
    document.getElementById('add-section').addEventListener('click', () => { sections.push({id: 0, clientKey: key(), title: `Section ${sections.length + 1}`, description: '', destinationKey: '', questions: [newQuestion()]}); render(); host.lastElementChild.scrollIntoView({behavior: 'smooth'}); });
    form.addEventListener('submit', e => { if (!form.reportValidity()) e.preventDefault(); });
    render();
})();
