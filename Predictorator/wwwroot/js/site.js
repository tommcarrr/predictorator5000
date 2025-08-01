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

    let ceefaxTimer = null;

    function updateCeefaxClock() {
        const now = new Date();
        const dateEl = document.getElementById('ceefaxDate');
        const timeEl = document.getElementById('ceefaxTime');
        if (dateEl) {
            dateEl.textContent = now.toLocaleDateString('en-GB', { weekday: 'short', day: '2-digit', month: 'short' });
        }
        if (timeEl) {
            timeEl.textContent = now.toLocaleTimeString('en-GB', { hour12: false });
        }
    }

    function setCeefax(enabled) {
        document.body.classList.toggle('ceefax', enabled);
        document.cookie = `ceefaxMode=${enabled};path=/;max-age=31536000`;
        if (enabled) {
            updateCeefaxClock();
            ceefaxTimer = setInterval(updateCeefaxClock, 1000);
        } else if (ceefaxTimer) {
            clearInterval(ceefaxTimer);
            ceefaxTimer = null;
        }
    }

    function isMobileDevice() {
        return /Mobi|Android|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent) ||
            (window.innerWidth <= 800 && window.innerHeight <= 600);
    }

    function registerToastHandler(dotnetHelper) {
        window.toastHelper = dotnetHelper;
    }

    function showToast(message, severity) {
        if (window.toastHelper) {
            window.toastHelper.invokeMethodAsync('ShowToast', message, severity);
        }
    }
    
    function copyPredictions() {
        const groups = document.querySelectorAll('.fixture-group');
        if (groups.length === 0) {
            showToast('No predictions available to copy.', 'error');
            return;
        }

        let text = '';
        let html = '<table border="1" cellpadding="5" cellspacing="0" style="border-collapse: collapse;">';
        let missing = false;

        groups.forEach(group => {
            const dateHeader = group.getAttribute('data-date') || '';
            text += dateHeader + '\n';
            html += `<thead><tr><th colspan="3" style="background-color: #f2f2f2; text-align: center; padding: 10px;">${dateHeader}</th></tr>`;
            html += '<tr><th style="background-color: #d9d9d9; text-align: left; padding: 5px; min-width: 120px">Home Team</th>' +
                '<th style="background-color: #d9d9d9; text-align: center; padding: 5px;">Score</th>' +
                '<th style="background-color: #d9d9d9; text-align: right; padding: 5px; min-width: 120px">Away Team</th></tr></thead><tbody>';

            const rows = group.querySelectorAll('.fixture-row');
            rows.forEach(row => {
                const homeTeam = row.querySelector('.home-name')?.textContent.trim() || '';
                const awayTeam = row.querySelector('.away-name')?.textContent.trim() || '';
                const inputs = row.querySelectorAll('.score-input input');
                const homeScore = inputs[0]?.value ?? '';
                const awayScore = inputs[1]?.value ?? '';
                if (homeScore === '' || awayScore === '') missing = true;

                text += `${homeTeam}    ${homeScore} - ${awayScore}    ${awayTeam}\n`;
                html += `<tr><td style="padding: 5px; text-align: left;">${homeTeam}</td>` +
                    `<td style="padding: 5px; text-align: center;">${homeScore} - ${awayScore}</td>` +
                    `<td style="padding: 5px; text-align: right;">${awayTeam}</td></tr>`;
            });

            text += '\n';
            html += '</tbody>';
        });

        html += '</table><br/>';

        if (missing) {
            showToast('Error: Please fill in all score predictions before copying.', 'error');
            return;
        }

        const mobile = isMobileDevice();
        const copyPromise = mobile ? copyToClipboardText(text) : copyToClipboardHtml(html);
        Promise.resolve(copyPromise).then(copied => {
            if (copied) {
                showToast('Predictions copied to clipboard!', 'success');
            } else {
                showToast('Failed to copy predictions to clipboard.', 'error');
            }
        });
    }

    async function login(data) {
        const response = await fetch('/login', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(data)
        });
        if (response.ok) {
            window.location.href = '/admin';
            return '';
        }

        return await response.text();
    }

    return {
        copyPredictions,
        registerToastHandler,
        setCeefax,
        login
    };
})();

document.addEventListener('click', function (e) {
    if (e.target && e.target.parentNode.id === 'copyBtn') {
        window.app.copyPredictions();
    }
});
