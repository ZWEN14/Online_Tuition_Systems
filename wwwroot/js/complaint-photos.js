// Keep the visible list and the files submitted by the form in sync.
(() => {
    const input = document.getElementById('Photos');
    const list = document.getElementById('complaint-photo-list');
    const status = document.getElementById('complaint-photo-status');
    if (!input || !list || !status) return;

    function render() {
        const files = Array.from(input.files);
        list.replaceChildren();
        files.forEach((file, index) => {
            const row = document.createElement('li');
            row.className = 'list-group-item d-flex align-items-center justify-content-between gap-3';
            const label = document.createElement('span');
            label.className = 'text-break';
            label.textContent = `${file.name} (${Math.ceil(file.size / 1024)} KB)`;
            const remove = document.createElement('button');
            remove.type = 'button';
            remove.className = 'btn btn-sm btn-outline-danger flex-shrink-0';
            remove.textContent = 'Remove';
            remove.setAttribute('aria-label', `Remove ${file.name}`);
            remove.addEventListener('click', () => {
                // FileList is read-only: build a replacement without this item.
                const remaining = new DataTransfer();
                Array.from(input.files).forEach((item, position) => {
                    if (position !== index) remaining.items.add(item);
                });
                input.files = remaining.files;
                render();
                const buttons = list.querySelectorAll('button');
                if (buttons.length) buttons[Math.min(index, buttons.length - 1)].focus();
                else input.focus();
            });
            row.append(label, remove);
            list.append(row);
        });
        status.textContent = files.length ? `${files.length} photo(s) selected.` : 'No photos selected. Photos are optional.';
    }

    input.addEventListener('change', render);
    input.form?.addEventListener('reset', () => setTimeout(render, 0));
    render();
})();
