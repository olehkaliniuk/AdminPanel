document.addEventListener("DOMContentLoaded", function () {
    const picker = document.getElementById('colorPicker');
    const themeToggle = document.getElementById('themeToggle');
    const resetBtn = document.getElementById('resetColorBtn');
    const menuToggle = document.getElementById('colorMenuToggle');
    const menu = document.getElementById('colorMenu');
    const fontSelect = document.getElementById("fontselector");

    if (!picker || !themeToggle || !resetBtn || !menuToggle || !menu || !fontSelect)
    {
        return;
    }

    // Themen und Icons.
    const themes = ['dark', 'light', 'blue'];
    const themeIcons = {
        dark: '<i class="fas fa-moon"></i>',
        light: '<i class="fas fa-sun"></i>',
        blue: '<i class="fas fa-water"></i>'
    };

    // Standard-Akzentfarben.
    const defaultAccents = {
        dark: '#2a2a2a',
        light: '#f5f5f5',
        blue: '#b9c7de'
    };

    // Kontrastfarbe berechnen.
    function getContrastColor(hex) {
        const r = parseInt(hex.substr(1, 2), 16);
        const g = parseInt(hex.substr(3, 2), 16);
        const b = parseInt(hex.substr(5, 2), 16);
        const brightness = (r * 299 + g * 587 + b * 114) / 1000;
        return brightness > 128 ? '#111111' : '#e0e0e0';
    }

    // Akzentfarbe anwenden.
    function applyAccent(color) {
        const textColor = getContrastColor(color);
        const target = document.body;
        target.style.setProperty('--color-btn-bg', color);
        target.style.setProperty('--color-btn-text', textColor);
    }

    // Gespeicherte Akzentfarbe laden.
    function loadAccent() {
        const saved = localStorage.getItem('accentColor');
        const currentTheme = localStorage.getItem('theme') || 'dark';
        const color = saved || defaultAccents[currentTheme];
        applyAccent(color);
        picker.value = color;
    }

    // Thema setzen.
    function setTheme(theme) {
        document.body.classList.remove('light-theme', 'blue-theme');
        if (theme === 'light')
        {
            document.body.classList.add('light-theme');
        }
        if (theme === 'blue')
        {
            document.body.classList.add('blue-theme');
        }
        localStorage.setItem('theme', theme);
        themeToggle.innerHTML = themeIcons[theme];

        // Standard-Akzentfarbe setzen.
        const defaultColor = defaultAccents[theme];
        applyAccent(defaultColor);
        picker.value = defaultColor;
        localStorage.setItem('accentColor', defaultColor);
    }

    // Nächstes Thema berechnen.
    function nextTheme() {
        const current = localStorage.getItem('theme') || 'dark';
        const nextIndex = (themes.indexOf(current) + 1) % themes.length;
        return themes[nextIndex];
    }

    // Theme-Button.
    themeToggle.addEventListener('click', () => {
        const newTheme = nextTheme();
        setTheme(newTheme);
    });

    // Farbmenü ein-/ausblenden.
    menuToggle.addEventListener('click', () => {
        menu.classList.toggle('show');
    });

    // Akzentfarbe ändern.
    picker.addEventListener('input', (e) => {
        const color = e.target.value;
        applyAccent(color);
        localStorage.setItem('accentColor', color);
    });

    // Akzentfarbe zurücksetzen.
    resetBtn.addEventListener('click', () => {
        const currentTheme = localStorage.getItem('theme') || 'dark';
        const defaultColor = defaultAccents[currentTheme];
        applyAccent(defaultColor);
        picker.value = defaultColor;
        localStorage.removeItem('accentColor');
    });

    // Schriftgröße laden.
    const savedFont = localStorage.getItem('fontSizePx');
    if (savedFont) {
        document.body.style.fontSize = savedFont + 'rem';
        fontSelect.value = savedFont;
    }

    // Schriftgröße ändern.
    fontSelect.addEventListener('change', () => {
        const fontSize = fontSelect.value;
        document.body.style.fontSize = fontSize + 'rem';
        localStorage.setItem('fontSizePx', fontSize);
    });

    // Initialisierung.
    (function init() {
        const savedTheme = localStorage.getItem('theme') || 'dark';
        document.body.classList.remove('light-theme', 'blue-theme');
        if (savedTheme === 'light')
        {
            document.body.classList.add('light-theme');
        }
        if (savedTheme === 'blue')
        {
            document.body.classList.add('blue-theme');
        }
        themeToggle.innerHTML = themeIcons[savedTheme];

        loadAccent();
    })();

});


