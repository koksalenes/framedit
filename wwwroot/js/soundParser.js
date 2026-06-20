(function () {
    const fileInput   = document.getElementById('videoFileInput');
    const dropzone    = document.getElementById('dropzone');
    const browseBtn   = document.getElementById('browseBtn');
    const emptyState  = document.getElementById('emptyState');
    const videoState  = document.getElementById('videoState');
    const videoPlayer = document.getElementById('videoPlayer');
    const videoNameEl = document.getElementById('videoName');
    const videoSizeEl = document.getElementById('videoSize');
    const resetBtn    = document.getElementById('resetBtn');
    const soundForm   = document.getElementById('soundForm');
    const submitBtn   = document.getElementById('submitBtn');
    const btnLabel    = submitBtn.querySelector('.btn-label');
    const btnProc     = submitBtn.querySelector('.btn-processing');
    const errorBanner = document.getElementById('errorBanner');
    const errorText   = document.getElementById('errorText');
    const formatSelect = document.getElementById('format');

    let objectUrl = null;

    function formatBytes(bytes) {
        if (bytes < 1000)  return bytes + ' B';
        if (bytes < 1e6)   return (bytes / 1e3).toFixed(1) + ' KB';
        if (bytes < 1e9)   return (bytes / 1e6).toFixed(1) + ' MB';
        return (bytes / 1e9).toFixed(2) + ' GB';
    }

    function showError(msg) {
        errorText.textContent = msg;
        errorBanner.hidden = false;
        errorBanner.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
    }

    function hideError() { errorBanner.hidden = true; }

    function setProcessing(active) {
        btnLabel.hidden = active;
        btnProc.hidden  = !active;
        submitBtn.disabled = active;
    }

    function showVideo(file) {
        if (objectUrl) URL.revokeObjectURL(objectUrl);
        objectUrl = URL.createObjectURL(file);
        videoPlayer.src = objectUrl;
        videoNameEl.textContent = file.name;
        videoSizeEl.textContent = formatBytes(file.size);
        hideError();
        emptyState.hidden = true;
        videoState.hidden = false;
    }

    function resetAll() {
        if (objectUrl) { URL.revokeObjectURL(objectUrl); objectUrl = null; }
        videoPlayer.src = '';
        fileInput.value = '';
        hideError();
        setProcessing(false);
        emptyState.hidden = false;
        videoState.hidden = true;
    }

    browseBtn.addEventListener('click', () => fileInput.click());
    dropzone.addEventListener('click', (e) => { if (!browseBtn.contains(e.target)) fileInput.click(); });
    fileInput.addEventListener('change', () => { if (fileInput.files.length) showVideo(fileInput.files[0]); });
    resetBtn.addEventListener('click', resetAll);

    dropzone.addEventListener('dragover', (e) => { e.preventDefault(); dropzone.classList.add('dragging'); });
    dropzone.addEventListener('dragleave', (e) => { if (!dropzone.contains(e.relatedTarget)) dropzone.classList.remove('dragging'); });
    dropzone.addEventListener('drop', (e) => {
        e.preventDefault();
        dropzone.classList.remove('dragging');
        const file = e.dataTransfer.files[0];
        if (!file?.type.startsWith('video/')) return;
        const dt = new DataTransfer();
        dt.items.add(file);
        fileInput.files = dt.files;
        showVideo(file);
    });

    soundForm.addEventListener('submit', async (e) => {
        e.preventDefault();
        hideError();
        setProcessing(true);

        try {
            const formData = new FormData(soundForm);
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            const headers = token ? { 'RequestVerificationToken': token } : {};

            const res = await fetch('/sound-parser/extract', {
                method: 'POST',
                body: formData,
                headers
            });

            if (!res.ok) {
                const data = await res.json().catch(() => ({}));
                showError(data.error ?? 'Something went wrong. Please try again.');
                return;
            }

            const disposition = res.headers.get('Content-Disposition') ?? '';
            const nameMatch   = /filename\*?=['"]?(?:UTF-\d['"]*)?([^;\r\n"']+)['"]?/i.exec(disposition);
            const baseName    = videoNameEl.textContent.replace(/\.[^.]+$/, '');
            const filename    = nameMatch?.[1] ?? `${baseName}.${formatSelect.value}`;

            const blob = await res.blob();
            const url  = URL.createObjectURL(blob);
            const a    = document.createElement('a');
            a.href     = url;
            a.download = filename;
            document.body.appendChild(a);
            a.click();
            a.remove();
            URL.revokeObjectURL(url);
        } catch {
            showError('Network error. Please check your connection and try again.');
        } finally {
            setProcessing(false);
        }
    });
})();
