'use strict';

// ============================================================
// leaderboard.js — Streamer.bot XP System
// L'overlay AFFICHE les données — il ne calcule rien.
//
// API Streamer.bot (dans l'action C#) :
//   CPH.ObsSendBrowserSourceScriptUrl("Leaderboard",
//     "window.updateLeaderboard([...])");
//
// Format attendu :
//   [
//     { rank: 1, username: "Mystya",  level: 42, xp: 18500, avatar: "" },
//     { rank: 2, username: "Tya",     level: 31, xp: 11200, avatar: "" },
//     { rank: 3, username: "Zephyr",  level: 28, xp: 9800,  avatar: "" }
//   ]
//
// avatar : URL Twitch ou "" pour fallback couleur
// ============================================================

// ---- API publique ----

/**
 * Appelé par Streamer.bot pour mettre à jour l'affichage.
 * @param {Array} top3 - Tableau de 1 à 3 entrées de leaderboard.
 */
window.updateLeaderboard = function (top3) {
  if (!Array.isArray(top3) || top3.length === 0) {
    console.warn('[Leaderboard] ⚠️ updateLeaderboard — données vides ou invalides :', top3);
    return;
  }
  console.log('[Leaderboard] 🎨 Rendu podium —', top3.length, 'entrée(s) :', top3.map(function(e){ return e.username; }));
  renderPodium(top3);
};

/**
 * Change le thème de l'overlay.
 * Thèmes disponibles : default | blue | red | green
 * @param {string} theme
 */
window.setTheme = function (theme) {
  document.body.className = 'theme-' + theme;
};

// ---- Rendu ----

function renderPodium(data) {
  console.log('[Leaderboard] 🎬 Animation déclenchée — overlay visible');
  resetEnterAnimations();

  updateSlot(1, data[0] || null);
  updateSlot(2, data[1] || null);
  updateSlot(3, data[2] || null);

  showOverlay();
  triggerEnterAnimations();
}

function updateSlot(rank, player) {
  var slot = getEl('slot-' + rank);
  if (!slot) return;

  if (!player) {
    slot.style.visibility = 'hidden';
    return;
  }

  slot.style.visibility = 'visible';

  var oldXp = getEl('xp-' + rank) ? getEl('xp-' + rank).dataset.raw : null;
  var newXp = String(player.xp || 0);

  setContent('name-' + rank,  player.username || '---');
  setContent('level-' + rank, player.level    || '?');
  setContent('xp-' + rank,    formatXp(player.xp || 0));

  getEl('xp-' + rank).dataset.raw = newXp;
  setAvatar(rank, player.avatar || '');

  // Flash si XP a changé sur une entrée déjà affichée
  if (oldXp !== null && oldXp !== newXp) {
    flashCard(rank);
  }
}

// ---- Helpers DOM ----

function getEl(id) {
  return document.getElementById(id);
}

function setContent(id, value) {
  var el = getEl(id);
  if (el) el.textContent = value;
}

function setAvatar(rank, src) {
  var img = getEl('avatar-' + rank);
  if (!img) return;
  img.src = src;
  img.alt = '';
}

function formatXp(xp) {
  if (xp >= 1000000) return (xp / 1000000).toFixed(1) + 'M';
  if (xp >= 1000)    return (xp / 1000).toFixed(1) + 'k';
  return String(xp);
}

// ---- Overlay visibilité ----

function showOverlay() {
  var overlay = getEl('overlay');
  if (overlay) overlay.classList.remove('hidden');
}

// ---- Animations d'entrée ----

function resetEnterAnimations() {
  [1, 2, 3].forEach(function (rank) {
    var slot = getEl('slot-' + rank);
    if (!slot) return;
    slot.classList.remove('enter');
    // Force reflow minimal pour permettre la relance de l'animation CSS
    void slot.offsetWidth;
  });
}

function triggerEnterAnimations() {
  [1, 2, 3].forEach(function (rank) {
    var slot = getEl('slot-' + rank);
    if (slot) slot.classList.add('enter');
  });
}

// ---- Flash mise à jour ----

function flashCard(rank) {
  var slot = getEl('slot-' + rank);
  var card = slot ? slot.querySelector('.card') : null;
  if (!card) return;

  card.classList.remove('flash');
  void card.offsetWidth;
  card.classList.add('flash');

  card.addEventListener('animationend', function onEnd() {
    card.classList.remove('flash');
    card.removeEventListener('animationend', onEnd);
  });
}

// ---- Réception depuis Streamer.bot (obs-browser emit_event) ----
// CPH.ObsSendRaw("CallVendorRequest", ...) émet un CustomEvent sur window.
// e.detail contient directement le tableau Top3Entry.

// ---- WebSocket Streamer.bot ----
// Reçoit { event: 'updateLeaderboard', players: [...] } via General.Custom

(function connectWS() {
  var ws = new WebSocket('ws://127.0.0.1:8080/');

  ws.addEventListener('open', function () {
    console.log('[Leaderboard] ✅ WebSocket connecté à Streamer.bot');
    ws.send(JSON.stringify({
      request: 'Subscribe',
      id: 'leaderboard-overlay',
      events: { General: ['Custom'] }
    }));
  });

  ws.addEventListener('message', function (e) {
    try {
      var msg = JSON.parse(e.data);
      if (msg.data)                                 msg = msg.data;
      if (typeof msg === 'string')                  msg = JSON.parse(msg);
      if (msg.data && typeof msg.data === 'string') msg = JSON.parse(msg.data);

      if ((msg.event || '').toLowerCase() !== 'updateleaderboard') return;

      console.log('[Leaderboard] 📨 Message reçu :', msg.players);
      window.updateLeaderboard(msg.players);
    } catch (err) {
      console.error('[Leaderboard] ❌ Erreur parsing:', err);
    }
  });

  ws.addEventListener('close', function () {
    console.warn('[Leaderboard] ⚠️ WebSocket fermé — reconnexion dans 2s');
    setTimeout(connectWS, 2000);
  });

  ws.addEventListener('error', function () {
    console.error('[Leaderboard] ❌ WebSocket erreur — Streamer.bot est-il lancé ?');
  });
}());

// ---- Checks DOM au démarrage ----

(function init() {
  var required = ['overlay', 'slot-1', 'slot-2', 'slot-3',
                  'name-1', 'name-2', 'name-3',
                  'xp-1',   'xp-2',   'xp-3',
                  'level-1','level-2','level-3'];

  var missing = required.filter(function (id) {
    return !document.getElementById(id);
  });

  if (missing.length > 0) {
    console.error('[Leaderboard] ❌ Éléments DOM manquants :', missing.join(', '));
  } else {
    console.log('[Leaderboard] ✅ DOM OK');
  }

  console.log('[Leaderboard] ✅ Prêt — connexion WebSocket en cours...');
  console.log('[Leaderboard]    Dev mode : ajouter ?dev à l\'URL');
}());

// ---- Dev mode ----
// Ouvrir leaderboard.html?dev dans un navigateur pour prévisualiser

if (new URLSearchParams(window.location.search).has('dev')) {
  window.updateLeaderboard([
    { rank: 1, username: 'Mystya',    level: 42, xp: 18500, avatar: '' },
    { rank: 2, username: 'Tya_Plays', level: 31, xp: 11200, avatar: '' },
    { rank: 3, username: 'ZephyrTV',  level: 28, xp: 9800,  avatar: '' },
  ]);
}
