window.app = (() => {


    async function copyToClipboardText(text) {
        try {
            if (navigator.clipboard && window.isSecureContext) {
                await navigator.clipboard.writeText(text);
                return true;
            }
        } catch (err) {
            console.error('Could not use clipboard API: ', err);
        }

        try {
            const textarea = document.createElement('textarea');
            textarea.value = text;
            textarea.style.position = 'fixed';
            textarea.style.top = '0';
            textarea.style.left = '0';
            textarea.style.width = '1px';
            textarea.style.height = '1px';
            textarea.style.padding = '0';
            textarea.style.border = 'none';
            textarea.style.outline = 'none';
            textarea.style.boxShadow = 'none';
            textarea.style.background = 'transparent';
            document.body.appendChild(textarea);
            textarea.focus();
            textarea.select();
            const successful = document.execCommand('copy');
            document.body.removeChild(textarea);
            return successful;
        } catch (err) {
            console.error('Fallback copy failed: ', err);
            return false;
        }
    }

    async function copyToClipboardHtml(html) {
        try {
            if (navigator.clipboard && window.isSecureContext) {
                await navigator.clipboard.write([
                    new ClipboardItem({
                        'text/html': new Blob([html], { type: 'text/html' })
                    })
                ]);
                return true;
            }
        } catch (err) {
            console.error('Could not use clipboard API: ', err);
        }

        try {
            const container = document.createElement('div');
            container.innerHTML = html;
            document.body.appendChild(container);
            const range = document.createRange();
            range.selectNodeContents(container);
            const selection = window.getSelection();
            selection.removeAllRanges();
            selection.addRange(range);
            const successful = document.execCommand('copy');
            selection.removeAllRanges();
            document.body.removeChild(container);
            return successful;
        } catch (err) {
            console.error('Fallback copy failed: ', err);
            return false;
        }
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
