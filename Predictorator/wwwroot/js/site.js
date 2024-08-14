const toggleButton = document.getElementById('darkModeToggle');
const body = document.body;
const navbar = document.querySelector('.navbar');
const accordion = document.querySelector('.accordion');

const toggleDarkMode = (enable) => {
    body.classList.toggle('dark-mode', enable);
    navbar.classList.toggle('bg-dark', enable);
    navbar.classList.toggle('navbar-dark', enable);
    navbar.classList.toggle('bg-white', !enable);
    navbar.classList.toggle('navbar-light', !enable);
    accordion.classList.toggle('accordion-dark', enable);
    toggleButton.textContent = enable ? 'Light Mode' : 'Dark Mode';
    localStorage.setItem('dark-mode', enable ? 'enabled' : 'disabled');
};

toggleDarkMode(localStorage.getItem('dark-mode') === 'enabled');

toggleButton.addEventListener('click', () => toggleDarkMode(!body.classList.contains('dark-mode')));

document.getElementById('copyBtn').addEventListener('click', () => {

    let resultHtml = '';

    let missingScores = false;
    resultHtml += `<table border="1" cellpadding="5" cellspacing="0" style="border-collapse: collapse;">`;
    document.querySelectorAll('.date-block').forEach(dateBlock => {
        const dateHeader = dateBlock.querySelector('.date-header').innerText;

        // Start a new table for each date

        resultHtml += `<thead><tr><th colspan="3" style="background-color: #f2f2f2; text-align: center; padding: 10px;">${dateHeader}</th></tr>`;
        resultHtml += '<tr><th style="background-color: #d9d9d9; text-align: left; padding: 5px; min-width: 120px">Home Team</th>';
        resultHtml += '<th style="background-color: #d9d9d9; text-align: center; padding: 5px;">Score</th>';
        resultHtml += '<th style="background-color: #d9d9d9; text-align: right; padding: 5px; min-width: 120px">Away Team</th></tr></thead><tbody>';

        dateBlock.querySelectorAll('.fixture-row').forEach(row => {
            const homeTeam = row.children[0].innerText.trim();
            const homeScore = row.children[1].querySelectorAll('input')[0].value;
            const awayScore = row.children[1].querySelectorAll('input')[1].value;
            const awayTeam = row.children[2].innerText.trim();

            if (homeScore === '' || awayScore === '') {
                missingScores = true;
            }

            resultHtml += `<tr><td style="padding: 5px; text-align: left;">${homeTeam}</td><td style="padding: 5px; text-align: center;">${homeScore} - ${awayScore}</td><td style="padding: 5px; text-align: right;">${awayTeam}</td></tr>`;
        });

    });

    resultHtml += '</tbody></table><br/>';
    if (missingScores) {
        alert('Error: Please fill in all score predictions before copying.');
        return;
    }

    // Use the Clipboard API to copy the HTML content
    navigator.clipboard.write([
        new ClipboardItem({
            'text/html': new Blob([resultHtml], { type: 'text/html' })
        })
    ]).then(() => {
        alert('Predictions copied to clipboard!');
    }).catch(err => {
        console.error('Could not copy text: ', err);
    });
});


document.getElementById('fillRandomBtn').addEventListener('click', () => {

    const possibleScores = [0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 3, 3, 4];

    function getRandomScore() {
        return possibleScores[Math.floor(Math.random() * possibleScores.length)];
    }

    document.querySelectorAll('.score-input').forEach(input => {
        if (input.value === '') {
            input.value = getRandomScore();
        }
    });
});

document.getElementById('clearBtn').addEventListener('click', () => {

    document.querySelectorAll('.score-input').forEach(input => {
        if (!input.readOnly) {
            input.value = '';
        }
    });
});
