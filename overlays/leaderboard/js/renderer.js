'use strict';

function formatXp(xp) {
  if (xp >= 1000000) return (xp / 1000000).toFixed(1) + 'M';
  if (xp >= 1000)    return (xp / 1000).toFixed(1) + 'k';
  return String(xp);
}

function getEl(id) { return document.getElementById(id); }

function setContent(id, value) {
  var el = getEl(id);
  if (el) el.textContent = value;
}

// Met à jour un slot du podium avec les données d'un joueur
function updateSlot(rank, player) {
  var slot = getEl('slot-' + rank);
  if (!slot) return;

  if (!player) {
    slot.style.visibility = 'hidden';
    return;
  }

  slot.style.visibility = 'visible';

  var xpEl  = getEl('xp-' + rank);
  var oldXp = xpEl ? xpEl.dataset.raw : null;
  var newXp = String(player.xp || 0);

  setContent('name-' + rank,  player.username || '---');
  setContent('level-' + rank, player.level    || '?');
  setContent('xp-' + rank,    formatXp(player.xp || 0));

  if (xpEl) xpEl.dataset.raw = newXp;

  var img = getEl('avatar-' + rank);
  if (img) { img.src = player.avatar || ''; img.alt = ''; }

  // Flash si le XP d'une entrée déjà affichée vient de changer
  if (oldXp !== null && oldXp !== newXp) {
    flashCard(rank); // animations.js
  }
}

// Point d'entrée rendu : remplit les 3 slots et déclenche les animations
function renderPodium(data) {
  resetEnterAnimations(); // animations.js

  updateSlot(1, data[0] || null);
  updateSlot(2, data[1] || null);
  updateSlot(3, data[2] || null);

  var overlay = getEl('overlay');
  if (overlay) overlay.classList.remove('hidden');

  triggerEnterAnimations(); // animations.js
  console.log('[Leaderboard] 🎬 Podium rendu — overlay visible');
}
