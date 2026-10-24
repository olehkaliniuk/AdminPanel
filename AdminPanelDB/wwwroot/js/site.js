const defaultAccents = {
    dark: '#2a2a2a',
    light: '#ffffff',
    blue: '#c4d4e8'
};

document.addEventListener("DOMContentLoaded", function () {

    const picker = document.getElementById('colorPicker');
    const themeToggle = document.getElementById('themeToggle');
    const resetBtn = document.getElementById('resetColorBtn');
    const menuToggle = document.getElementById('colorMenuToggle');
    const menu = document.getElementById('colorMenu');
    const fontSelect = document.getElementById("fontselector");

    const themes = ['dark', 'light', 'blue'];

    const themeIcons = {
        dark: '<i class="fas fa-moon"></i>',
        light: '<i class="fas fa-sun"></i>',
        blue: '<i class="fas fa-water"></i>'
    };

    function applyAccent(color) {
        document.documentElement.style.setProperty('--color-btn-bg', color);

        const r = parseInt(color.substr(1, 2), 16);
        const g = parseInt(color.substr(3, 2), 16);
        const b = parseInt(color.substr(5, 2), 16);

        const brightness = (r * 299 + g * 587 + b * 114) / 1000;
        const textColor = brightness > 128 ? '#111111' : '#e0e0e0';

        document.documentElement.style.setProperty('--color-btn-text', textColor);
    }

    function getTheme() {
        return localStorage.getItem('theme') || 'dark';
    }

    function getAccent() {
        return localStorage.getItem('accentColor');
    }

    function setTheme(theme) {

        document.documentElement.classList.remove('light-theme', 'blue-theme');

        if (theme === 'light') document.documentElement.classList.add('light-theme');
        if (theme === 'blue') document.documentElement.classList.add('blue-theme');

        localStorage.setItem('theme', theme);

        const accentMode = localStorage.getItem('accentMode');
        const accent = getAccent();

        let color;

        if (accentMode === 'custom' && accent) {
            color = accent;
        } else {
            color = defaultAccents[theme];
        }

        applyAccent(color);

        if (picker) picker.value = color;

        if (themeToggle) {
            themeToggle.innerHTML = themeIcons[theme];
        }
    }

    function nextTheme() {
        const current = getTheme();
        return themes[(themes.indexOf(current) + 1) % themes.length];
    }

    themeToggle?.addEventListener('click', () => {
        setTheme(nextTheme());
    });

    menuToggle?.addEventListener('click', () => {
        menu.classList.toggle('show');
    });

    picker?.addEventListener('input', (e) => {
        const color = e.target.value;

        applyAccent(color);

        localStorage.setItem('accentColor', color);
        localStorage.setItem('accentMode', 'custom');
    });

    resetBtn?.addEventListener('click', () => {
        localStorage.removeItem('accentColor');
        localStorage.setItem('accentMode', 'default');

        const theme = getTheme();
        const color = defaultAccents[theme];

        applyAccent(color);

        if (picker) picker.value = color;
    });

    fontSelect?.addEventListener('change', () => {
        const size = fontSelect.value;
        document.documentElement.style.fontSize = size + 'rem';
        localStorage.setItem('fontSizePx', size);
    });

    (function init() {

        const theme = getTheme();
        const accent = getAccent();
        const mode = localStorage.getItem('accentMode') || 'default';

        document.documentElement.classList.remove('light-theme', 'blue-theme');

        if (theme === 'light') document.documentElement.classList.add('light-theme');
        if (theme === 'blue') document.documentElement.classList.add('blue-theme');

        let color;

        if (mode === 'custom' && accent) {
            color = accent;
        } else {
            color = defaultAccents[theme];
        }

        applyAccent(color);

        if (picker) picker.value = color;

        if (themeToggle) {
            themeToggle.innerHTML = themeIcons[theme];
        }

        const font = localStorage.getItem('fontSizePx');
        if (font) {
            fontSelect.value = font;
            document.documentElement.style.fontSize = font + 'rem';
        }
    })();

});

// Fehlerbox nach 4 Sekunden ausblenden und nach 4,5 Sekunden entfernen.
const box = document.getElementById('error-box');
if (box) {
    setTimeout(() => { box.style.opacity = '0'; }, 4000);
    setTimeout(() => { box.remove(); }, 4500);
}

let toastTimer;

// CSRF-Token holen.
function getCsrfToken() {
    const tokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
    return tokenElement ? tokenElement.value : '';
}

function showToast(message, type = 'info') {
    let box = document.getElementById('error-box');
    if (!box) {
        box = document.createElement('div');
        box.id = 'error-box';
        document.body.appendChild(box);
    }

    box.className = `toast-box toast-${type}`;
    box.textContent = message;
    box.style.display = 'block';
    box.style.opacity = '1';

    clearTimeout(toastTimer);
    toastTimer = setTimeout(() => {
        box.style.opacity = '0';
        toastTimer = setTimeout(() => { box.style.display = 'none'; }, 500);
    }, 4000);
}

// Meldung für die Zeit nach einem Seiten-Reload merken.
function showToastAfterReload(message, type = 'info') {
    try {
        sessionStorage.setItem('toast', JSON.stringify({ message, type }));
    } catch { }
}

// Gemerkte Meldung nach dem Reload anzeigen.
document.addEventListener('DOMContentLoaded', () => {
    try {
        const saved = sessionStorage.getItem('toast');
        if (saved) {
            sessionStorage.removeItem('toast');
            const { message, type } = JSON.parse(saved);
            showToast(message, type);
        }
    } catch { }
});