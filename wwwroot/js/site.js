(function () {
    'use strict';

    if (document.querySelector('[data-booking-index]')) {
        window.addEventListener('pageshow', event => {
            if (event.persisted) window.location.reload();
        });
    }

    document.addEventListener('click', function (event) {
        const back = event.target.closest('[data-back-fallback]');
        if (back) {
            if (window.history.length > 1 && document.referrer) {
                window.history.back();
            } else {
                window.location.href = back.dataset.backFallback;
            }
            return;
        }

        const link = event.target.closest('a[data-ajax="true"]');
        if (!link || link.dataset.ajaxBusy === 'true') return;

        event.preventDefault();
        link.dataset.ajaxBusy = 'true';
        ajaxRequest(link.href, 'GET', null, link.dataset.ajaxTarget)
            .finally(() => { link.dataset.ajaxBusy = 'false'; });
    });

    document.addEventListener('submit', async function (event) {
        const form = event.target.closest('form[data-ajax-booking="true"]');
        if (!form) return;
        event.preventDefault();
        const button = form.querySelector('button');
        if (button) button.disabled = true;
        try {
            const response = await fetch(form.action, {
                method: 'POST',
                body: new FormData(form),
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            });
            const result = await response.json();
            if (!response.ok || !result.success) throw new Error(result.message);
            document.querySelector('[data-booking-status]')?.setAttribute('data-booking-status', result.status);
            window.showAjaxMessage?.(result.message, true);
            window.setTimeout(() => window.location.reload(), 350);
        } catch {
            window.showAjaxMessage?.('The booking action could not be completed. Please try again.', false);
        } finally {
            if (button) button.disabled = false;
        }
    });

    document.querySelector('[name="BookingDate"]')?.addEventListener('change', event => {
        const target = document.querySelector('[data-booking-date]');
        if (target && event.target.value) {
            target.textContent = new Date(`${event.target.value}T00:00:00`).toLocaleDateString(undefined, { day: '2-digit', month: 'short', year: 'numeric' });
        }
    });

    async function ajaxRequest(url, method, body, target) {
        const options = { method, headers: { 'X-Requested-With': 'XMLHttpRequest' } };
        if (body) options.body = body;
        const response = await fetch(url, options);
        if (response.redirected) {
            window.location.href = response.url;
            return;
        }

        const contentType = response.headers.get('content-type') || '';
        if (contentType.includes('application/json')) {
            const result = await response.json();
            if (result.redirect) window.location.href = result.redirect;
            else if (result.reload) window.location.reload();
            else if (result.html && target) document.querySelector(target).innerHTML = result.html;
            else if (result.message) showAjaxMessage(result.message, result.success !== false);
            return;
        }

        const html = await response.text();
        if (target && document.querySelector(target)) {
            const targetElement = document.querySelector(target);
            const parsed = new DOMParser().parseFromString(html, 'text/html').querySelector(target);
            targetElement.innerHTML = parsed ? parsed.innerHTML : html;
        } else {
            document.open();
            document.write(html);
            document.close();
        }
    }

    document.addEventListener('submit', async function (event) {
        const form = event.target.closest('form[data-ajax="true"]');
        if (!form || form.dataset.ajaxBusy === 'true') return;

        event.preventDefault();
        form.dataset.ajaxBusy = 'true';
        const submit = form.querySelector('[type="submit"]');
        const originalText = submit?.innerHTML;
        if (submit) {
            submit.disabled = true;
            submit.innerHTML = '<span class="spinner-border spinner-border-sm me-1" aria-hidden="true"></span> Working...';
        }

        try {
            const method = (form.method || 'POST').toUpperCase();
            const formData = new FormData(form);
            const options = { method, headers: { 'X-Requested-With': 'XMLHttpRequest' } };
            let url = form.action || window.location.href;
            if (method === 'GET') {
                url = `${url}${url.includes('?') ? '&' : '?'}${new URLSearchParams(formData).toString()}`;
            } else {
                options.body = formData;
            }
            await ajaxRequest(url, method, method === 'GET' ? null : formData, form.dataset.ajaxTarget);
        } catch (error) {
            showAjaxMessage('The request failed. Please try again.', false);
        } finally {
            form.dataset.ajaxBusy = 'false';
            if (submit) {
                submit.disabled = false;
                submit.innerHTML = originalText;
            }
        }
    });

    const adminForm = document.querySelector('form[data-ajax="true"][data-ajax-target="#adminTable"]');
    if (adminForm) {
        let timer;
        const refresh = () => {
            window.clearTimeout(timer);
            timer = window.setTimeout(() => adminForm.requestSubmit(), 250);
        };
        adminForm.querySelector('[name="query"]')?.addEventListener('input', refresh);
        adminForm.querySelector('[name="sort"]')?.addEventListener('change', refresh);
        document.querySelector('#clearAdminSearch')?.addEventListener('click', () => {
            const query = adminForm.querySelector('[name="query"]');
            if (!query.value) return;
            query.value = '';
            query.dispatchEvent(new Event('input', { bubbles: true }));
            query.focus();
        });
    }

    const photoInput = document.querySelector('#photoInput');
    const photoPreview = document.querySelector('#photoPreview');
    const photoCanvas = document.querySelector('#photoCanvas');
    const photoStatus = document.querySelector('#photoStatus');
    const photoZoomControl = document.querySelector('#photoZoom');
    const photoEditorPage = document.querySelector('#photoEditorPage');
    const applyPhotoEdit = document.querySelector('#applyPhotoEdit');
    const openPhotoEditor = document.querySelector('#openPhotoEditor');

    let photoSource;
    let photoRotation = 0;
    let photoZoom = 1;
    let photoOffsetX = 0;
    let photoOffsetY = 0;
    let dragStart;

    function setPhotoStatus(message, isError = false) {
        if (!photoStatus) return;

        photoStatus.textContent = message;
        photoStatus.classList.toggle('text-danger', isError);
    }

    function showProcessedPreview() {
        if (!photoSource || !photoCanvas) return;

        const size = photoCanvas.width || 320;
        const context = photoCanvas.getContext('2d');

        const radians = photoRotation * Math.PI / 180;

        const scale = Math.max(
            size / photoSource.width,
            size / photoSource.height
        ) * photoZoom;

        const width = photoSource.width * scale;
        const height = photoSource.height * scale;

        photoCanvas.width = size;
        photoCanvas.height = size;

        context.clearRect(0, 0, size, size);

        context.save();

        context.translate(size / 2, size / 2);
        context.rotate(radians);

        context.drawImage(
            photoSource,
            -width / 2 + photoOffsetX,
            -height / 2 + photoOffsetY,
            width,
            height
        );

        context.restore();

        photoPreview?.querySelector('img, span')?.classList.add('d-none');
        photoCanvas.classList.remove('d-none');
    }

    function loadPhoto(file) {
        if (!file || !file.type.startsWith('image/')) {
            setPhotoStatus('Please choose an image file.', true);
            return;
        }

        const reader = new FileReader();

        reader.onload = event => {
            const image = new Image();

            image.onload = () => {
                photoSource = image;

                photoRotation = 0;
                photoZoom = 1;
                photoOffsetX = 0;
                photoOffsetY = 0;

                if (photoZoomControl) {
                    photoZoomControl.value = '1';
                }

                showProcessedPreview();
            };

            image.src = event.target.result;
        };

        reader.readAsDataURL(file);
    }

    photoInput?.addEventListener('change', () => {
        const file = photoInput.files?.[0];

        if (!file) return;

        loadPhoto(file);

        const reader = new FileReader();

        reader.onload = () => {
            sessionStorage.setItem(
                'profileEditorSource',
                reader.result
            );
        };

        reader.readAsDataURL(file);
    });

    openPhotoEditor?.addEventListener('click', () => {

        const file = photoInput?.files?.[0];

        if (file) {

            const reader = new FileReader();

            reader.onload = () => {

                sessionStorage.setItem(
                    'profileEditorSource',
                    reader.result
                );

                window.location.href = openPhotoEditor.dataset.url ||
                    '/Account/PhotoEditor';
            };

            reader.readAsDataURL(file);

        } else {

            window.location.href =
                openPhotoEditor.dataset.url ||
                '/Account/PhotoEditor';
        }
    });

    photoZoomControl?.addEventListener('input', () => {

        photoZoom = Number(photoZoomControl.value);

        showProcessedPreview();
    });


    document.querySelector('#rotateLeft')?.addEventListener('click', () => {

        if (!photoSource) {
            return setPhotoStatus(
                'Choose an image first.',
                true
            );
        }

        photoRotation = (photoRotation + 270) % 360;

        showProcessedPreview();
    });


    document.querySelector('#rotateRight')?.addEventListener('click', () => {

        if (!photoSource) {
            return setPhotoStatus(
                'Choose an image first.',
                true
            );
        }

        photoRotation = (photoRotation + 90) % 360;

        showProcessedPreview();
    });


    document.querySelector('#cropPhoto')?.addEventListener('click', () => {

        if (!photoSource) {
            return setPhotoStatus(
                'Choose an image first.',
                true
            );
        }

        showProcessedPreview();

        setPhotoStatus('Square crop applied.');
    });

    photoCanvas?.addEventListener('pointerdown', event => {

        if (!photoEditorPage || !photoSource) return;

        photoCanvas.setPointerCapture(event.pointerId);

        dragStart = {
            x: event.clientX,
            y: event.clientY,
            offsetX: photoOffsetX,
            offsetY: photoOffsetY
        };

        photoCanvas.classList.add('is-dragging');
    });


    photoCanvas?.addEventListener('pointermove', event => {

        if (!dragStart) return;

        photoOffsetX =
            dragStart.offsetX +
            event.clientX -
            dragStart.x;

        photoOffsetY =
            dragStart.offsetY +
            event.clientY -
            dragStart.y;

        showProcessedPreview();
    });


    photoCanvas?.addEventListener('pointerup', () => {

        dragStart = null;

        photoCanvas.classList.remove('is-dragging');
    });


    photoCanvas?.addEventListener('pointercancel', () => {

        dragStart = null;

        photoCanvas.classList.remove('is-dragging');
    });

    function loadPhotoFromDataUrl(dataUrl) {

        if (!dataUrl) return;

        const image = new Image();

        image.onload = () => {

            photoSource = image;

            photoRotation = 0;
            photoZoom = 1;
            photoOffsetX = 0;
            photoOffsetY = 0;

            if (photoZoomControl) {
                photoZoomControl.value = '1';
            }

            showProcessedPreview();
        };

        image.onerror = () => {

            setPhotoStatus(
                'Unable to load the selected photo.',
                true
            );
        };

        image.src = dataUrl;
    }

    if (photoEditorPage) {

        const editorSource =
            sessionStorage.getItem('profileEditorSource');

        if (editorSource) {

            loadPhotoFromDataUrl(editorSource);

            setPhotoStatus(
                'Selected photo loaded into the editor.'
            );

        } else {

            const initialPhoto =
                document.querySelector('#initialPhoto');

            if (initialPhoto?.tagName === 'IMG') {

                loadPhotoFromDataUrl(initialPhoto.src);

            } else {

                setPhotoStatus(
                    'Choose an image to begin.'
                );
            }
        }
    }

    if (!photoEditorPage) {

        const savedPhoto =
            sessionStorage.getItem('profileEditorResult');

        if (savedPhoto) {

            sessionStorage.removeItem(
                'profileEditorResult'
            );

            loadPhotoFromDataUrl(savedPhoto);

            fetch(savedPhoto)
                .then(response => response.blob())
                .then(blob => {

                    const file = new File(
                        [blob],
                        'profile-photo.jpg',
                        {
                            type: 'image/jpeg'
                        }
                    );

                    const transfer = new DataTransfer();

                    transfer.items.add(file);

                    if (photoInput) {
                        photoInput.files =
                            transfer.files;
                    }

                    setPhotoStatus(
                        'Edited photo ready to upload.'
                    );
                })
                .catch(error => {

                    console.error(
                        'Unable to restore edited photo:',
                        error
                    );

                    setPhotoStatus(
                        'Unable to restore the edited photo.',
                        true
                    );
                });
        }
    }

    applyPhotoEdit?.addEventListener('click', () => {
        if (!photoSource || !photoCanvas || !photoEditorPage) {
            setPhotoStatus(
                'Choose or capture an image first.',
                true
            );

            return;
        }

        photoCanvas.toBlob(blob => {

            if (!blob) {
                setPhotoStatus(
                    'The edited photo could not be created.',
                    true
                );

                return;
            }

            const reader = new FileReader();

            reader.onload = () => {

                /*
                 * Only now is the edited/cropped image created
                 * as the final profile photo.
                 */
                sessionStorage.setItem(
                    'profileEditorResult',
                    reader.result
                );

                sessionStorage.removeItem(
                    'profileEditorSource'
                );

                window.location.href =
                    photoEditorPage.dataset.returnUrl;
            };

            reader.readAsDataURL(blob);

        }, 'image/jpeg', 0.9);
    });

    window.showAjaxMessage = function (message, success) {
        const alert = document.createElement('div');
        alert.className = `alert ${success ? 'alert-success' : 'alert-danger'} alert-dismissible fade show ajax-message`;
        alert.setAttribute('role', 'alert');
        alert.innerHTML = `${message}<button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>`;
        document.querySelector('.app-main')?.prepend(alert);
    };
})();
