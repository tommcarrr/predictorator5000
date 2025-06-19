window.app = (() => {
    const body = document.body;
    const navbar = document.querySelector('.navbar');
    const accordion = document.querySelector('.accordion');
    const toggleButton = document.getElementById('darkModeToggle');

    function setDarkMode(enable) {
        body.classList.toggle('dark-mode', enable);
        if (navbar) {
            navbar.classList.toggle('bg-dark', enable);
            navbar.classList.toggle('navbar-dark', enable);
            navbar.classList.toggle('bg-white', !enable);
            navbar.classList.toggle('navbar-light', !enable);
        }
        if (accordion) {
            accordion.classList.toggle('accordion-dark', enable);
        }
        if (toggleButton) {
            toggleButton.textContent = enable ? 'Light Mode' : 'Dark Mode';
        }
    }

    function saveDarkMode(enable) {
        localStorage.setItem('dark-mode', enable ? 'enabled' : 'disabled');
    }

    function getDarkMode() {
        return localStorage.getItem('dark-mode') === 'enabled';
    }

    function copyToClipboardText(text) {
        navigator.clipboard.writeText(text).then(() => {
            alert('Predictions copied to clipboard!');
        }).catch(err => {
            console.error('Could not copy text: ', err);
        });
    }

    function copyToClipboardHtml(html) {
        navigator.clipboard.write([
            new ClipboardItem({
                'text/html': new Blob([html], { type: 'text/html' })
            })
        ]).then(() => {
            alert('Predictions copied to clipboard!');
        }).catch(err => {
            console.error('Could not copy text: ', err);
        });
    }

    function isMobileDevice() {
        return /Mobi|Android|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent) ||
            (window.innerWidth <= 800 && window.innerHeight <= 600);
    }

    return {
        setDarkMode,
        saveDarkMode,
        getDarkMode,
        copyToClipboardText,
        copyToClipboardHtml,
        isMobileDevice
    };
})();
