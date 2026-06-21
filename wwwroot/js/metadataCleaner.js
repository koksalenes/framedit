(function () {
    const { MAX_FILES, MAX_BYTES, MAX_BYTES_DISPLAY } = APP;

    const fileInput      = document.getElementById('fileInput');
    const dropzone       = document.getElementById('dropzone');
    const browseBtn      = document.getElementById('browseBtn');
    const addMoreBtn     = document.getElementById('addMoreBtn');
    const emptyState     = document.getElementById('emptyState');
    const fileListState  = document.getElementById('fileListState');
    const fileListEl     = document.getElementById('fileList');
    const fileCountEl    = document.getElementById('fileCount');
    const totalSizeEl    = document.getElementById('totalSize');
    const limitWarning   = document.getElementById('limitWarning');
    const limitWarnText  = document.getElementById('limitWarningText');
    const clearAllBtn    = document.getElementById('clearAllBtn');
    const cleanForm      = document.getElementById('cleanForm');
    const submitBtn      = document.getElementById('submitBtn');
    const btnLabel       = submitBtn.querySelector('.btn-label');
    const btnProc        = submitBtn.querySelector('.btn-processing');
    const errorBanner    = document.getElementById('errorBanner');
    const errorText      = document.getElementById('errorText');

    let files = [];

    function formatBytes(b) {
        if (b < 1000)          return b + ' B';
        if (b < 1e6)           return (b / 1e3).toFixed(1) + ' KB';
        if (b < 1e9)           return (b / 1e6).toFixed(1) + ' MB';
        return (b / 1e9).toFixed(2) + ' GB';
    }

    function ext(file) {
        return file.name.slice(file.name.lastIndexOf('.') + 1).toUpperCase();
    }

    function showError(msg) {
        errorText.textContent = msg;
        errorBanner.hidden = false;
        errorBanner.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
    }

    function hideError() {
        errorBanner.hidden = true;
    }

    function setProcessing(active) {
        btnLabel.hidden = active;
        btnProc.hidden  = !active;
        submitBtn.disabled = active;
    }

    function fileIcon(file) {
        const type = file.type;
        if (type.startsWith('image/')) {
            return `<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="3" y="3" width="18" height="18" rx="2"/><circle cx="8.5" cy="8.5" r="1.5"/><path d="M21 15l-5-5L5 21"/></svg>`;
        }
        if (type.startsWith('video/')) {
            return `<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polygon points="23 7 16 12 23 17 23 7"/><rect x="1" y="5" width="15" height="14" rx="2"/></svg>`;
        }
        return `<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><path d="M14 2v6h6"/></svg>`;
    }

    function renderList() {
        const total = files.reduce((s, f) => s + f.size, 0);

        fileListEl.innerHTML = files.map((f, i) => `
            <li class="file-item" data-index="${i}">
                <span class="file-item-icon">${fileIcon(f)}</span>
                <span class="file-item-name" title="${f.name}">${f.name}</span>
                <span class="file-item-meta">
                    <span class="file-ext-badge">${ext(f)}</span>
                    <span class="file-size">${formatBytes(f.size)}</span>
                </span>
                <button type="button" class="file-remove" data-index="${i}" title="Remove">
                    <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
                        <line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/>
                    </svg>
                </button>
            </li>`).join('');

        fileCountEl.textContent = `${files.length} file${files.length === 1 ? '' : 's'}`;
        totalSizeEl.textContent = formatBytes(total);

        const overCount = files.length > MAX_FILES;
        const overSize  = total > MAX_BYTES;

        if (overCount || overSize) {
            limitWarning.hidden = false;
            limitWarnText.textContent = overCount
                ? `Max ${MAX_FILES} files — remove ${files.length - MAX_FILES} to continue`
                : `Total exceeds ${MAX_BYTES_DISPLAY} — remove some files to continue`;
            submitBtn.disabled = true;
        } else {
            limitWarning.hidden = true;
            submitBtn.disabled = false;
        }

        fileListEl.querySelectorAll('.file-remove').forEach(btn => {
            btn.addEventListener('click', () => {
                files.splice(Number(btn.dataset.index), 1);
                if (files.length === 0) { showEmpty(); } else { renderList(); }
            });
        });
    }

    function showFileList() {
        emptyState.hidden   = true;
        fileListState.hidden = false;
        renderList();
    }

    function showEmpty() {
        files = [];
        fileInput.value = '';
        emptyState.hidden   = false;
        fileListState.hidden = true;
        hideError();
        setProcessing(false);
    }

    function addFiles(newFiles) {
        for (const f of newFiles) {
            const dup = files.some(e => e.name === f.name && e.size === f.size);
            if (!dup) files.push(f);
        }
        if (files.length > 0) showFileList();
    }

    browseBtn.addEventListener('click', () => fileInput.click());
    addMoreBtn.addEventListener('click', () => fileInput.click());
    dropzone.addEventListener('click', (e) => { if (!browseBtn.contains(e.target)) fileInput.click(); });
    clearAllBtn.addEventListener('click', showEmpty);

    fileInput.addEventListener('change', () => {
        if (fileInput.files.length) { addFiles(Array.from(fileInput.files)); fileInput.value = ''; }
    });

    dropzone.addEventListener('dragover', (e) => { e.preventDefault(); dropzone.classList.add('dragging'); });
    dropzone.addEventListener('dragleave', (e) => { if (!dropzone.contains(e.relatedTarget)) dropzone.classList.remove('dragging'); });
    dropzone.addEventListener('drop', (e) => {
        e.preventDefault();
        dropzone.classList.remove('dragging');
        if (e.dataTransfer.files.length) addFiles(Array.from(e.dataTransfer.files));
    });

    cleanForm.addEventListener('submit', async (e) => {
        e.preventDefault();
        hideError();

        const total = files.reduce((s, f) => s + f.size, 0);
        if (files.length === 0)          return showError('Please add at least one file.');
        if (files.length > MAX_FILES)    return showError(`Max ${MAX_FILES} files allowed.`);
        if (total > MAX_BYTES)           return showError(`Total size exceeds ${MAX_BYTES_DISPLAY}.`);

        setProcessing(true);

        try {
            const formData = new FormData();
            files.forEach(f => formData.append('files', f, f.name));

            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            const headers = token ? { 'RequestVerificationToken': token } : {};

            const res = await fetch('/metadata-cleaner/clean', {
                method: 'POST',
                body: formData,
                headers
            });

            if (!res.ok) {
                const data = await res.json().catch(() => ({}));
                showError(data.error ?? 'Something went wrong. Please try again.');
                return;
            }

            const blob = await res.blob();
            const url  = URL.createObjectURL(blob);
            const a    = document.createElement('a');
            a.href     = url;
            a.download = 'framedit-metadata-cleaner.zip';
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
