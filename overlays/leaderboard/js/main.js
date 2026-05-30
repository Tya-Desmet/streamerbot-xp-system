'use strict';

// ---- API publique (appelée par socket.js ou depuis la console) ----

window.updateLeaderboard = function (top3) {
  if (!Array.isArray(top3) || top3.length === 0) {
    console.warn('[Leaderboard] ⚠️ updateLeaderboard — données vides ou invalides :', top3);
    return;
  }
  console.log('[Leaderboard] 🎨 Rendu podium —', top3.length, 'entrée(s) :', top3.map(function (e) { return e.username; }));
  renderPodium(top3); // renderer.js
};

// Changement de thème dynamique — swapping le <link id="theme-css">
window.setTheme = function (theme) {
  var link = document.getElementById('theme-css');
  if (!link) return;
  link.href = '../themes/' + theme + '/theme.css';
  console.log('[Leaderboard] 🎨 Thème :', theme);
};

// ---- Vérifications au démarrage ----

(function init() {
  var required = [
    'overlay',
    'slot-1',  'slot-2',  'slot-3',
    'name-1',  'name-2',  'name-3',
    'xp-1',    'xp-2',    'xp-3',
    'level-1', 'level-2', 'level-3'
  ];

  var missing = required.filter(function (id) { return !document.getElementById(id); });

  if (missing.length > 0) {
    console.error('[Leaderboard] ❌ Éléments DOM manquants :', missing.join(', '));
  } else {
    console.log('[Leaderboard] ✅ DOM OK');
  }

  console.log('[Leaderboard] ✅ Prêt — connexion WebSocket en cours...');
  console.log('[Leaderboard]    Dev mode : ajouter ?dev à l\'URL');
  console.log('[Leaderboard]    Switch thème : window.setTheme("rpg")');
}());

// ---- Dev mode : données fictives sans Streamer.bot ----
if (new URLSearchParams(window.location.search).has('dev')) {
  window.updateLeaderboard([
    { rank: 1, username: 'Mystya',    level: 42, xp: 18500, avatar: '' },
    { rank: 2, username: 'Tya_Plays', level: 31, xp: 11200, avatar: '' },
    { rank: 3, username: 'ZephyrTV',  level: 28, xp: 9800,  avatar: '' }
  ]);
}
