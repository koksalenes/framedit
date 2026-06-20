(function () {
    const toggle   = document.getElementById('toolsToggle');
    const dropdown = document.getElementById('toolsDropdown');
    if (!toggle || !dropdown) return;

    function open() {
        dropdown.hidden = false;
        toggle.setAttribute('aria-expanded', 'true');
    }

    function close() {
        dropdown.hidden = true;
        toggle.setAttribute('aria-expanded', 'false');
    }

    toggle.addEventListener('click', (e) => {
        e.stopPropagation();
        dropdown.hidden ? open() : close();
    });

    document.addEventListener('click', (e) => {
        if (!toggle.contains(e.target) && !dropdown.contains(e.target)) close();
    });

    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape') close();
    });
})();
