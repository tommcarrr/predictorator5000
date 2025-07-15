window.app = (() => {


    function fallbackCopyText(text) {
        const textarea = document.createElement('textarea');
        textarea.value = text;
        textarea.style.position = 'fixed';
        document.body.appendChild(textarea);
        textarea.focus();
        textarea.select();
        try {
            const success = document.execCommand('copy');
            document.body.removeChild(textarea);
            return success;
        } catch (err) {
            console.error('Could not copy text: ', err);
            document.body.removeChild(textarea);
            return false;
        }
    }

    function copyToClipboardText(text) {
        if (navigator.clipboard && navigator.clipboard.writeText) {
            return navigator.clipboard.writeText(text)
                .then(() => true)
                .catch(err => {
                    console.error('Could not copy text: ', err);
                    return fallbackCopyText(text);
                });
        }

        return fallbackCopyText(text);
    }

    function fallbackCopyHtml(html) {
        const listener = (e) => {
            e.clipboardData.setData('text/html', html);
            e.clipboardData.setData('text/plain', html);
            e.preventDefault();
        };

        document.addEventListener('copy', listener);
        const success = document.execCommand('copy');
        document.removeEventListener('copy', listener);
        return success;
    }

    function copyToClipboardHtml(html) {
        if (navigator.clipboard && window.ClipboardItem) {
            const item = new ClipboardItem({
                'text/html': new Blob([html], { type: 'text/html' })
            });
            return navigator.clipboard.write([item])
                .then(() => true)
                .catch(err => {
                    console.error('Could not copy text: ', err);
                    return fallbackCopyHtml(html);
                });
        }

        return fallbackCopyHtml(html);
    }

    function isMobileDevice() {
        return /Mobi|Android|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent) ||
            (window.innerWidth <= 800 && window.innerHeight <= 600);
    }

    function setLocalStorage(key, value) {
        window.localStorage.setItem(key, value);
    }

    function getLocalStorage(key) {
        return window.localStorage.getItem(key);
    }

    return {
        copyToClipboardText,
        copyToClipboardHtml,
        isMobileDevice,
        setLocalStorage,
        getLocalStorage
    };
})();
