// DOM Elements
const toggleButton = document.getElementById('darkModeToggle');
const body = document.body;
const navbar = document.querySelector('.navbar');
const accordion = document.querySelector('.accordion');

// Initialization
initializeDarkMode();
initializeEventListeners();

// Dark Mode Functions
function initializeDarkMode() {
    const darkModeEnabled = localStorage.getItem('dark-mode') === 'enabled';
    toggleDarkMode(darkModeEnabled);

    toggleButton.addEventListener('click', () => {
        const isEnabled = !body.classList.contains('dark-mode');
        toggleDarkMode(isEnabled);
    });
}

function toggleDarkMode(enable) {
    body.classList.toggle('dark-mode', enable);
    navbar.classList.toggle('bg-dark', enable);
    navbar.classList.toggle('navbar-dark', enable);
    navbar.classList.toggle('bg-white', !enable);
    navbar.classList.toggle('navbar-light', !enable);
    accordion.classList.toggle('accordion-dark', enable);
    toggleButton.textContent = enable ? 'Light Mode' : 'Dark Mode';
    localStorage.setItem('dark-mode', enable ? 'enabled' : 'disabled');
}

// Event Listeners Initialization
function initializeEventListeners() {
    document.getElementById('copyBtn').addEventListener('click', handleCopyButtonClick);
    document.getElementById('fillRandomBtn').addEventListener('click', fillRandomScores);
    document.getElementById('clearBtn').addEventListener('click', clearScores);
}

// Copy Data Functions
function handleCopyButtonClick() {
    const isMobile = isMobileDevice();
    const data = extractFixtureData();

    if (data.missingScores) {
        alert('Error: Please fill in all score predictions before copying.');
        return;
    }

    if (isMobile) {
        copyToClipboardText(data.text);
    } else {
        copyToClipboardHtml(data.html);
    }
}

function extractFixtureData() {
    let resultText = '';
    let resultHtml = '<table border="1" cellpadding="5" cellspacing="0" style="border-collapse: collapse;">';
    let missingScores = false;

    document.querySelectorAll('.date-block').forEach(dateBlock => {
        const dateHeader = dateBlock.querySelector('.date-header').innerText;
        resultText += `${dateHeader}\n`;
        resultHtml += createHtmlTableHeader(dateHeader);

        dateBlock.querySelectorAll('.fixture-row').forEach(row => {
            const homeTeam = row.children[0].innerText.trim();
            const homeScore = row.children[1].querySelectorAll('input')[0].value;
            const awayScore = row.children[1].querySelectorAll('input')[1].value;
            const awayTeam = row.children[2].innerText.trim();

            if (homeScore === '' || awayScore === '') {
                missingScores = true;
            }

            resultText += `${homeTeam}    ${homeScore} - ${awayScore}    ${awayTeam}\n`;
            resultHtml += createHtmlTableRow(homeTeam, homeScore, awayScore, awayTeam);
        });

        resultText += '\n';
    });

    resultHtml += '</tbody></table><br/>';

    return { text: resultText, html: resultHtml, missingScores };
}

function createHtmlTableHeader(dateHeader) {
    return `<thead><tr><th colspan="3" style="background-color: #f2f2f2; text-align: center; padding: 10px;">${dateHeader}</th></tr>
            <tr><th style="background-color: #d9d9d9; text-align: left; padding: 5px; min-width: 120px">Home Team</th>
            <th style="background-color: #d9d9d9; text-align: center; padding: 5px;">Score</th>
            <th style="background-color: #d9d9d9; text-align: right; padding: 5px; min-width: 120px">Away Team</th></tr></thead><tbody>`;
}

function createHtmlTableRow(homeTeam, homeScore, awayScore, awayTeam) {
    return `<tr><td style="padding: 5px; text-align: left;">${homeTeam}</td>
                <td style="padding: 5px; text-align: center;">${homeScore} - ${awayScore}</td>
                <td style="padding: 5px; text-align: right;">${awayTeam}</td></tr>`;
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

// Random Score and Clear Functions
function fillRandomScores() {
    const possibleScores = [0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 3, 3, 4];

    document.querySelectorAll('.score-input').forEach(input => {
        if (input.value === '') {
            input.value = getRandomScore(possibleScores);
        }
    });
}

function getRandomScore(possibleScores) {
    return possibleScores[Math.floor(Math.random() * possibleScores.length)];
}

function clearScores() {
    document.querySelectorAll('.score-input').forEach(input => {
        if (!input.readOnly) {
            input.value = '';
        }
    });
}

// Utility Function
function isMobileDevice() {
    return /Mobi|Android|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent) ||
        (window.innerWidth <= 800 && window.innerHeight <= 600);
}
