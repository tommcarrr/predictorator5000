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

    function isSafari() {
        const ua = navigator.userAgent;
        return ua.includes('Safari') && !ua.includes('Chrome') && !ua.includes('Chromium');
    }

    function copyToClipboardText(text) {
        if (!isSafari() && navigator.clipboard && navigator.clipboard.writeText) {
            return navigator.clipboard.writeText(text)
                .then(() => true)
                .catch(err => {
                    console.error('Could not copy text: ', err);
                    return fallbackCopyText(text);
                });
        }

        return Promise.resolve(fallbackCopyText(text));
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
        if (!isSafari() && navigator.clipboard && window.ClipboardItem) {
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

        return Promise.resolve(fallbackCopyHtml(html));
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

    function registerScoreInputs(delay = 500) {
        const timers = new Map();
        document.querySelectorAll('.score-input input').forEach(input => {
            input.addEventListener('input', () => {
                const existing = timers.get(input);
                if (existing) clearTimeout(existing);
                if (input.value === '') return;
                const timer = setTimeout(() => {
                    const all = Array.from(document.querySelectorAll('.score-input input'));
                    let idx = all.indexOf(input) + 1;
                    while (idx < all.length && all[idx].disabled) idx++;
                    if (idx < all.length) {
                        all[idx].focus();
                    }
                }, delay);
                timers.set(input, timer);
            });
        });
    }

    function startPongGame(row, playerIsHome) {
        if (document.getElementById('pongOverlay')) return;

        const overlay = document.createElement('div');
        overlay.id = 'pongOverlay';
        overlay.innerHTML = '<canvas id="pongCanvas" width="300" height="150"></canvas>';
        document.body.appendChild(overlay);

        const canvas = overlay.querySelector('#pongCanvas');
        const ctx = canvas.getContext('2d');
        const paddleHeight = 40;
        const paddleWidth = 5;
        let playerY = canvas.height / 2 - paddleHeight / 2;
        let computerY = playerY;
        let ballX = canvas.width / 2;
        let ballY = canvas.height / 2;
        let ballVX = 2;
        let ballVY = 2;
        let playerScore = 0;
        let compScore = 0;
        let running = true;

        function resetBall() {
            ballX = canvas.width / 2;
            ballY = canvas.height / 2;
            ballVX = -ballVX;
            ballVY = 2 * (Math.random() > 0.5 ? 1 : -1);
        }

        canvas.addEventListener('touchmove', e => {
            const rect = canvas.getBoundingClientRect();
            playerY = e.touches[0].clientY - rect.top - paddleHeight / 2;
            e.preventDefault();
        });
        canvas.addEventListener('mousemove', e => {
            const rect = canvas.getBoundingClientRect();
            playerY = e.clientY - rect.top - paddleHeight / 2;
        });

        function update() {
            ballX += ballVX;
            ballY += ballVY;
            if (ballY < 0 || ballY > canvas.height) ballVY = -ballVY;

            if (ballX <= paddleWidth) {
                if (ballY > playerY && ballY < playerY + paddleHeight) {
                    ballVX = -ballVX;
                } else {
                    compScore++;
                    resetBall();
                }
            }

            if (ballX >= canvas.width - paddleWidth) {
                if (ballY > computerY && ballY < computerY + paddleHeight) {
                    ballVX = -ballVX;
                } else {
                    playerScore++;
                    resetBall();
                }
            }

            if (ballY > computerY + paddleHeight / 2) computerY += 2;
            else if (ballY < computerY + paddleHeight / 2) computerY -= 2;
        }

        function draw() {
            ctx.fillStyle = 'black';
            ctx.fillRect(0, 0, canvas.width, canvas.height);
            ctx.fillStyle = 'white';
            ctx.fillRect(0, playerY, paddleWidth, paddleHeight);
            ctx.fillRect(canvas.width - paddleWidth, computerY, paddleWidth, paddleHeight);
            ctx.beginPath();
            ctx.arc(ballX, ballY, 3, 0, Math.PI * 2);
            ctx.fill();
            ctx.font = '16px sans-serif';
            ctx.fillText(playerScore, canvas.width / 4, 20);
            ctx.fillText(compScore, canvas.width * 3 / 4, 20);
        }

        function loop() {
            if (!running) return;
            update();
            draw();
            requestAnimationFrame(loop);
        }
        loop();

        setTimeout(() => {
            running = false;
            overlay.remove();
            const inputs = row.querySelectorAll('.score-input input');
            if (inputs.length >= 2) {
                inputs[playerIsHome ? 0 : 1].value = playerScore;
                inputs[playerIsHome ? 0 : 1].dispatchEvent(new Event('input', { bubbles: true }));
                inputs[playerIsHome ? 1 : 0].value = compScore;
                inputs[playerIsHome ? 1 : 0].dispatchEvent(new Event('input', { bubbles: true }));
            }
        }, 30000);
    }

    function registerPongEasterEgg() {
        if (!isMobileDevice()) return;
        document.querySelectorAll('.team-name').forEach(nameEl => {
            let timer = null;
            const start = () => {
                timer = setTimeout(() => {
                    const row = nameEl.closest('.fixture-row');
                    const playerIsHome = nameEl.classList.contains('home-name');
                    startPongGame(row, playerIsHome);
                }, 3000);
            };
            const cancel = () => { if (timer) { clearTimeout(timer); timer = null; } };
            nameEl.addEventListener('touchstart', start);
            nameEl.addEventListener('touchend', cancel);
            nameEl.addEventListener('touchcancel', cancel);
            nameEl.addEventListener('touchmove', cancel);
        });
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

      function downloadFile(name, content) {
          const blob = new Blob([content], { type: 'text/csv' });
          const link = document.createElement('a');
          link.href = URL.createObjectURL(blob);
          link.download = name;
          document.body.appendChild(link);
          link.click();
          document.body.removeChild(link);
          URL.revokeObjectURL(link.href);
      }

      return {
          copyPredictions,
          copyToClipboardText,
          copyToClipboardHtml,
          registerScoreInputs,
          registerPongEasterEgg,
          registerToastHandler,
          setCeefax,
          login,
          downloadFile
      };
  })();

document.addEventListener('click', function (e) {
    if (e.target && e.target.parentNode.id === 'copyBtn') {
        window.app.copyPredictions();
    }
});
