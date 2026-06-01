'use strict';

// ---- Helpers DOM ----

function restartAnim(el) {
  if (!el) return;
  el.style.animation = 'none';
  el.offsetHeight; // reflow — force reset de l'animation CSS
  el.style.animation = '';
}

// ---- Population ----
// Appelée par main.js → window.showCheckIn(data)

function populate(data) {
  var cardSize   = data.cardSize || 10;
  var isComplete = (data.event || '').toLowerCase() === 'checkin_cardcomplete';
  var filled     = isComplete ? cardSize : (data.count || 0);

  document.getElementById('checkin-username').textContent = data.username || '';
  document.getElementById('checkin-xp').textContent       = '+' + (data.xpGained || 0) + ' XP';

  var labelEl = document.getElementById('checkin-progress-label');
  if (labelEl) labelEl.textContent = filled + ' / ' + cardSize + ' cases';

  // Regénère les cases à chaque affichage (animations CSS repartent à zéro)
  var boxesEl = document.getElementById('checkin-boxes');
  boxesEl.innerHTML = '';
  for (var i = 0; i < cardSize; i++) {
    var box = document.createElement('div');
    box.className = 'checkin-box' + (i < filled ? ' filled' : '');
    boxesEl.appendChild(box);
  }

  var container      = document.getElementById('checkin-container');
  var cardCompleteEl = document.getElementById('checkin-card-complete');

  if (isComplete) {
    container.classList.add('card-complete');
    cardCompleteEl.classList.remove('hidden');
  } else {
    container.classList.remove('card-complete');
    cardCompleteEl.classList.add('hidden');
  }

  // Reset animations enfants — utile si re-show rapide
  var animated = ['checkin-icon', 'checkin-header', 'checkin-username',
                  'checkin-xp',   'checkin-progress-label'];
  animated.forEach(function (id) { restartAnim(document.getElementById(id)); });
}
