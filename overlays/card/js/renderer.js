'use strict';

// ---- Formatage ----

function formatXp(xp) {
  if (xp >= 1000000) return (xp / 1000000).toFixed(1) + 'M';
  if (xp >= 1000)    return (xp / 1000).toFixed(1) + 'k';
  return String(xp);
}

function formatMessages(count) {
  if (!count || count <= 0) return '0';
  if (count >= 1000000) return (count / 1000000).toFixed(1) + 'M';
  if (count >= 1000)    return (count / 1000).toFixed(1) + 'k';
  return String(count);
}

function formatWatchTime(minutes) {
  if (!minutes || minutes <= 0) return '0m';
  if (minutes < 60) return minutes + 'm';
  var h = Math.floor(minutes / 60);
  var m = minutes % 60;
  return m > 0 ? h + 'h ' + m + 'm' : h + 'h';
}

// ---- Helpers DOM ----

function setText(id, value) {
  var el = document.getElementById(id);
  if (el) el.textContent = value;
}

function setXpBar(current, forNext) {
  var fill = document.getElementById('xp-fill');
  if (!fill) return;

  var ratio = forNext > 0 ? Math.min(current / forNext, 1) : 0;

  // Reset sans transition — supprimer la transition CSS
  fill.style.transition = 'none';
  fill.style.transform  = 'scaleX(0)';

  // Double rAF : attendre que le navigateur ait rendu la frame "reset"
  // avant de réactiver la transition — pas de reflow synchrone
  requestAnimationFrame(function () {
    requestAnimationFrame(function () {
      fill.style.transition = '';
      fill.style.transform  = 'scaleX(' + ratio.toFixed(4) + ')';
    });
  });
}

// ---- Population principale ----
// Appelée par main.js → window.showCard(data)
// Tous les champs V2 sont optionnels — compatibilité payload V1 garantie.

function populate(data) {
  // Identité
  setText('username', data.username || '---');

  // Titre V2 — .card-title:empty { display: none } masque automatiquement si vide
  setText('card-title', data.title || '');

  // Niveau
  setText('level', data.level != null ? String(data.level) : '?');

  // Rang — badge avatar uniquement (pas de doublon dans col-info)
  setText('rank', data.rank != null ? String(data.rank) : '?');

  // Stats V2
  setText('messages', data.messages  != null ? formatMessages(data.messages)   : '—');
  setText('watchtime', data.watchTime != null ? formatWatchTime(data.watchTime) : '—');

  // Avatar
  var img = document.getElementById('avatar');
  if (img) { img.src = data.avatar || ''; img.alt = ''; }

  // Barre XP
  var current = data.xpCurrent != null ? data.xpCurrent : 0;
  var forNext  = data.xpForNext != null ? data.xpForNext : 1;
  setXpBar(current, forNext);

  // XP texte : "2450 / 3100 XP"
  setText('xp-current', formatXp(current));
  setText('xp-next',    formatXp(forNext) + ' XP');

  // Badge bonus XP
  var badge     = document.getElementById('bonus-badge');
  var bonusText = document.getElementById('bonus-text');
  if (badge) {
    if (data.bonusActive) {
      var mult = data.bonusMultiplier ? 'x' + data.bonusMultiplier : 'x2';
      var mins = data.bonusMinutesLeft ? ' · ' + data.bonusMinutesLeft + ' min' : '';
      if (bonusText) bonusText.textContent = mult + mins;
      badge.classList.remove('hidden');
    } else {
      badge.classList.add('hidden');
    }
  }
}

// Flash de la card si déjà visible — double rAF, pas de reflow synchrone
function flashCard() {
  var card = document.getElementById('card');
  if (!card) return;

  card.classList.remove('flash');
  requestAnimationFrame(function () {
    requestAnimationFrame(function () {
      card.classList.add('flash');
      card.addEventListener('animationend', function onFlash() {
        card.classList.remove('flash');
        card.removeEventListener('animationend', onFlash);
      });
    });
  });
}
