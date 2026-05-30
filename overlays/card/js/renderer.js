'use strict';

function formatXp(xp) {
  if (xp >= 1000000) return (xp / 1000000).toFixed(1) + 'M';
  if (xp >= 1000)    return (xp / 1000).toFixed(1) + 'k';
  return String(xp);
}

function setText(id, value) {
  var el = document.getElementById(id);
  if (el) el.textContent = value;
}

function setXpBar(current, forNext) {
  var fill = document.getElementById('xp-fill');
  if (!fill) return;

  var ratio = forNext > 0 ? Math.min(current / forNext, 1) : 0;

  // Reset sans transition pour repartir à zéro proprement
  fill.style.transition = 'none';
  fill.style.transform  = 'scaleX(0)';
  void fill.offsetWidth;

  // Active la transition et anime vers la valeur cible
  fill.style.transition = '';
  fill.style.transform  = 'scaleX(' + ratio.toFixed(4) + ')';
}

// Remplit tous les éléments DOM de la card avec les données du joueur
function populate(data) {
  setText('username', data.username || '---');
  setText('level',    data.level    || '?');
  setText('rank',     data.rank     || '?');

  var img = document.getElementById('avatar');
  if (img) { img.src = data.avatar || ''; img.alt = ''; }

  var current = data.xpCurrent || 0;
  var forNext  = data.xpForNext || 1;
  var level    = data.level     || 1;

  setXpBar(current, forNext);
  setText('xp-current', formatXp(current) + ' XP');
  setText('xp-next',    'Niv. ' + (level + 1) + ' (' + formatXp(forNext - current) + ' manquants)');
}
