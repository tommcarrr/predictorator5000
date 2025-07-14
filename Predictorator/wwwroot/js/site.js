window.app = (() => {


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
        copyToClipboardText,
        copyToClipboardHtml,
        isMobileDevice
    };
})();
