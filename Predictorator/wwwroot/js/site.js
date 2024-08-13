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
    let resultText = '';

    let missingScores = false;

    document.querySelectorAll('.date-block').forEach(dateBlock => {
        const dateHeader = dateBlock.querySelector('.date-header').innerText;
        resultText += dateHeader + '\n';

        dateBlock.querySelectorAll('.fixture-row').forEach(row => {
            const homeTeam = row.children[0].innerText.trim();
            const homeScore = row.children[1].querySelectorAll('input')[0].value;
            const awayScore = row.children[1].querySelectorAll('input')[1].value;
            const awayTeam = row.children[2].innerText.trim();

            if (homeScore === '' || awayScore === '') {
                missingScores = true;
            }

            resultText += `${homeTeam}\t${homeScore} - ${awayScore}\t${awayTeam}\n`;
        });

        resultText += '\n';
    });

    if (missingScores) {
        alert('Error: Please fill in all score predictions before copying.');
        return;
    }

    navigator.clipboard.writeText(resultText).then(() => {
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
