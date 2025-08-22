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
        const homeName = row.querySelector('.home-name')?.textContent.trim() || 'Home';
        const awayName = row.querySelector('.away-name')?.textContent.trim() || 'Away';
        const playerName = playerIsHome ? homeName : awayName;
        const computerName = playerIsHome ? awayName : homeName;

        const teamColors = {
            'arsenal': ['#ef0107', '#ffffff'],
            'aston villa': ['#7a003c', '#95bfe5'],
            'bournemouth': ['#da291c', '#000000'],
            'brentford': ['#e30613', '#ffffff'],
            'brighton': ['#0057b8', '#ffffff'],
            'burnley': ['#6c1d45', '#99badd'],
            'chelsea': ['#034694', '#ffffff'],
            'crystal palace': ['#c4122e', '#1b458f'],
            'everton': ['#003399', '#ffffff'],
            'fulham': ['#000000', '#ffffff'],
            'liverpool': ['#c8102e', '#ffffff'],
            'luton': ['#ff8200', '#002f6c'],
            'manchester city': ['#6cabdd', '#1c2c5b'],
            'manchester united': ['#da291c', '#fbe122'],
            'newcastle': ['#000000', '#ffffff'],
            'nottingham forest': ['#dd0000', '#ffffff'],
            'sheffield united': ['#ee2737', '#ffffff'],
            'tottenham': ['#132257', '#ffffff'],
            'west ham': ['#7a263a', '#1bb1e7'],
            'wolverhampton': ['#fdb913', '#231f20'],
            'leicester city': ['#003090', '#ffffff'],
            'leeds united': ['#ffffff', '#1d428a'],
            'southampton': ['#d71920', '#ffffff'],
            'norwich city': ['#fff200', '#007a33'],
            'watford': ['#fff200', '#000000'],
            'west bromwich albion': ['#003876', '#ffffff'],
            'blackburn rovers': ['#0057b8', '#ffffff'],
            'middlesbrough': ['#ce000c', '#ffffff'],
            'sunderland': ['#e03a3e', '#ffffff'],
            'swansea city': ['#ffffff', '#000000'],
            'stoke city': ['#e03a3e', '#ffffff'],
            'cardiff city': ['#0070b5', '#ffffff'],
            'hull city': ['#fdb913', '#000000'],
            'coventry city': ['#74b2de', '#000000'],
            'birmingham city': ['#0e4c92', '#ffffff'],
            'queens park rangers': ['#1d5aa6', '#ffffff'],
            'derby county': ['#ffffff', '#000000'],
            'ipswich town': ['#0057b8', '#ffffff'],
            'preston north end': ['#ffffff', '#001b44'],
            'reading': ['#003090', '#ffffff'],
            'sheffield wednesday': ['#0057b8', '#ffffff'],
            'millwall': ['#0046ae', '#ffffff'],
            'bristol city': ['#d71920', '#ffffff'],
            'blackpool': ['#ff8200', '#ffffff'],
            'bolton wanderers': ['#ffffff', '#1b458f'],
            'charlton athletic': ['#e41b17', '#ffffff'],
            'portsmouth': ['#003399', '#ffffff'],
            'wigan athletic': ['#0054a6', '#ffffff']
        };

        const teamAliases = {
            'villa': 'aston villa',
            'avfc': 'aston villa',
            'afc bournemouth': 'bournemouth',
            'brighton & hove albion': 'brighton',
            'brighton and hove albion': 'brighton',
            'palace': 'crystal palace',
            'everton fc': 'everton',
            'fulham fc': 'fulham',
            'liverpool fc': 'liverpool',
            'luton town': 'luton',
            'man city': 'manchester city',
            'mcfc': 'manchester city',
            'man united': 'manchester united',
            'man utd': 'manchester united',
            'man u': 'manchester united',
            'mufc': 'manchester united',
            'newcastle united': 'newcastle',
            'forest': 'nottingham forest',
            'notts forest': 'nottingham forest',
            'sheff united': 'sheffield united',
            'sheff utd': 'sheffield united',
            'blades': 'sheffield united',
            'tottenham hotspur': 'tottenham',
            'spurs': 'tottenham',
            'west ham united': 'west ham',
            'hammers': 'west ham',
            'wolves': 'wolverhampton',
            'wolverhampton wanderers': 'wolverhampton',
            'leicester': 'leicester city',
            'foxes': 'leicester city',
            'leeds': 'leeds united',
            'saints': 'southampton',
            'southampton fc': 'southampton',
            'norwich': 'norwich city',
            'canaries': 'norwich city',
            'west brom': 'west bromwich albion',
            'west bromwich': 'west bromwich albion',
            'baggies': 'west bromwich albion',
            'blackburn': 'blackburn rovers',
            'rovers': 'blackburn rovers',
            'boro': 'middlesbrough',
            'sunderland afc': 'sunderland',
            'black cats': 'sunderland',
            'swans': 'swansea city',
            'stoke': 'stoke city',
            'potters': 'stoke city',
            'cardiff': 'cardiff city',
            'bluebirds': 'cardiff city',
            'hull': 'hull city',
            'tigers': 'hull city',
            'coventry': 'coventry city',
            'birmingham': 'birmingham city',
            'bcfc': 'birmingham city',
            'qpr': 'queens park rangers',
            'derby': 'derby county',
            'rams': 'derby county',
            'ipswich': 'ipswich town',
            'tractor boys': 'ipswich town',
            'pne': 'preston north end',
            'preston': 'preston north end',
            'royals': 'reading',
            'sheff weds': 'sheffield wednesday',
            'sheff wed': 'sheffield wednesday',
            'wednesday': 'sheffield wednesday',
            'lions': 'millwall',
            'bristol': 'bristol city',
            'seasiders': 'blackpool',
            'bolton': 'bolton wanderers',
            'trotters': 'bolton wanderers',
            'charlton': 'charlton athletic',
            'addicks': 'charlton athletic',
            'pompey': 'portsmouth',
            'portsmouth fc': 'portsmouth',
            'wigan': 'wigan athletic',
            'latics': 'wigan athletic'
        };

        Object.entries(teamAliases).forEach(([alias, canonical]) => {
            teamColors[alias] = teamColors[canonical];
        });

        function getContrast(hex) {
            const c = hex.substring(1);
            const r = parseInt(c.substring(0, 2), 16) / 255;
            const g = parseInt(c.substring(2, 4), 16) / 255;
            const b = parseInt(c.substring(4, 6), 16) / 255;
            const luminance = 0.299 * r + 0.587 * g + 0.114 * b;
            return luminance > 0.5 ? '#000000' : '#ffffff';
        }

        const getTeamColors = name => {
            const colors = teamColors[name.toLowerCase()] || ['#000000', '#ffffff'];
            let [bg, fg] = colors;
            if (!fg || bg.toLowerCase() === fg.toLowerCase()) {
                fg = getContrast(bg);
            }
            return [bg, fg];
        };
        const [playerBg, playerFg] = getTeamColors(playerName);
        const [computerBg, computerFg] = getTeamColors(computerName);

        function colorizeName(el, name, colors) {
            const [bg, fg] = colors;
            el.textContent = name;
            el.style.backgroundColor = bg;
            el.style.color = fg;
            el.style.padding = '0 4px';
        }

        overlay.innerHTML = `
            <div id="pongScore">
                <span id="pongPlayerName"></span>
                <span id="pongPlayerScore">0</span>
                -
                <span id="pongComputerScore">0</span>
                <span id="pongComputerName"></span>
            </div>
            <div id="pongTimer">10</div>
            <canvas id="pongCanvas" width="300" height="150"></canvas>`;
        document.body.appendChild(overlay);

        const canvas = overlay.querySelector('#pongCanvas');
        const ctx = canvas.getContext('2d');
        const basePaddleHeight = 40;
        const paddleWidth = 5;
        let playerPaddleHeight = basePaddleHeight;
        let computerPaddleHeight = basePaddleHeight;
        const lowerPlayer = playerName.toLowerCase();
        const lowerComputer = computerName.toLowerCase();
        if (
            (lowerPlayer === 'everton' || lowerPlayer === 'liverpool') &&
            (lowerComputer === 'everton' || lowerComputer === 'liverpool')
        ) {
            const adjust = name => name === 'everton' ? basePaddleHeight * 1.5 : basePaddleHeight * 0.5;
            playerPaddleHeight = adjust(lowerPlayer);
            computerPaddleHeight = adjust(lowerComputer);
        }
        overlay.dataset.playerPaddleHeight = playerPaddleHeight;
        overlay.dataset.computerPaddleHeight = computerPaddleHeight;
        let playerY = canvas.height / 2 - playerPaddleHeight / 2;
        let computerY = canvas.height / 2 - computerPaddleHeight / 2;
        let ballX = canvas.width / 2;
        let ballY = canvas.height / 2;
        let ballVX = 3;
        let ballVY = 3;
        let playerScore = 0;
        let compScore = 0;
        let running = true;
        const computerSpeed = 2;

        const playerNameEl = overlay.querySelector('#pongPlayerName');
        const playerScoreEl = overlay.querySelector('#pongPlayerScore');
        const compNameEl = overlay.querySelector('#pongComputerName');
        const compScoreEl = overlay.querySelector('#pongComputerScore');
        const timerEl = overlay.querySelector('#pongTimer');
        colorizeName(playerNameEl, playerName, [playerBg, playerFg]);
        colorizeName(compNameEl, computerName, [computerBg, computerFg]);
        timerEl.textContent = '10';

        let timeLeft = 10;

        function endGame() {
            running = false;
            overlay.remove();
            const inputs = row.querySelectorAll('.score-input input');
            if (inputs.length >= 2) {
                inputs[playerIsHome ? 0 : 1].value = playerScore;
                inputs[playerIsHome ? 0 : 1].dispatchEvent(new Event('input', { bubbles: true }));
                inputs[playerIsHome ? 1 : 0].value = compScore;
                inputs[playerIsHome ? 1 : 0].dispatchEvent(new Event('input', { bubbles: true }));
            }
        }

        const countdown = setInterval(() => {
            timeLeft--;
            timerEl.textContent = String(timeLeft);
            if (timeLeft <= 0) {
                clearInterval(countdown);
                endGame();
            }
        }, 1000);

        function resetBall() {
            ballX = canvas.width / 2;
            ballY = canvas.height / 2;
            ballVX = -ballVX;
            ballVY = 3 * (Math.random() > 0.5 ? 1 : -1);
        }

        canvas.addEventListener('touchmove', e => {
            const rect = canvas.getBoundingClientRect();
            playerY = e.touches[0].clientY - rect.top - playerPaddleHeight / 2;
            e.preventDefault();
        });
        canvas.addEventListener('mousemove', e => {
            const rect = canvas.getBoundingClientRect();
            playerY = e.clientY - rect.top - playerPaddleHeight / 2;
        });

        function update() {
            ballX += ballVX;
            ballY += ballVY;
            if (ballY < 0 || ballY > canvas.height) ballVY = -ballVY;

            if (ballX <= paddleWidth) {
                if (ballY > playerY && ballY < playerY + playerPaddleHeight) {
                    ballVX = -ballVX;
                    ballVY += (Math.random() - 0.5);
                } else {
                    compScore++;
                    compScoreEl.textContent = compScore;
                    resetBall();
                }
            }

            if (ballX >= canvas.width - paddleWidth) {
                if (ballY > computerY && ballY < computerY + computerPaddleHeight) {
                    ballVX = -ballVX;
                    ballVY += (Math.random() - 0.5);
                } else {
                    playerScore++;
                    playerScoreEl.textContent = playerScore;
                    resetBall();
                }
            }

            if (ballY > computerY + computerPaddleHeight / 2) computerY += computerSpeed;
            else if (ballY < computerY + computerPaddleHeight / 2) computerY -= computerSpeed;
        }

        function draw() {
            ctx.fillStyle = playerBg;
            ctx.fillRect(0, 0, canvas.width / 2, canvas.height);
            ctx.fillStyle = computerBg;
            ctx.fillRect(canvas.width / 2, 0, canvas.width / 2, canvas.height);
            ctx.fillStyle = playerFg;
            ctx.fillRect(0, playerY, paddleWidth, playerPaddleHeight);
            ctx.fillStyle = computerFg;
            ctx.fillRect(canvas.width - paddleWidth, computerY, paddleWidth, computerPaddleHeight);
            ctx.beginPath();
            ctx.fillStyle = '#fff';
            ctx.arc(ballX, ballY, 3, 0, Math.PI * 2);
            ctx.fill();
        }

        function loop() {
            if (!running) return;
            update();
            draw();
            requestAnimationFrame(loop);
        }
        loop();
    }

    function registerPongEasterEgg() {
        document.querySelectorAll('.team-name').forEach(nameEl => {
            let tapCount = 0;
            let tapTimer = null;
            const onTap = (e) => {
                e.preventDefault();
                tapCount++;
                if (tapTimer) clearTimeout(tapTimer);
                if (tapCount === 3) {
                    const row = nameEl.closest('.fixture-row');
                    const playerIsHome = nameEl.classList.contains('home-name');
                    startPongGame(row, playerIsHome);
                    tapCount = 0;
                    return;
                }
                tapTimer = setTimeout(() => { tapCount = 0; }, 600);
            };
            nameEl.addEventListener('touchstart', onTap, { passive: false });
            nameEl.addEventListener('click', onTap);
            nameEl.addEventListener('contextmenu', e => e.preventDefault());
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
