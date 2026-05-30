'use strict';

// ============================================================
// card.js — Streamer.bot XP System
// L'overlay AFFICHE les données — il ne calcule rien.
//
// API Streamer.bot (dans l'action C#) :
//   CPH.ObsSendBrowserSourceScriptUrl("ProfileCard",
//     "window.showCard({...})");
//
// Format attendu :
//   {
//     username:   "Mystya",
//     avatar:     "https://...",   // "" pour fallback couleur
//     level:      12,
//     xpCurrent:  1450,            // XP dans le niveau actuel
//     xpForNext:  283,             // XP requis pour monter de niveau
//     rank:       4                // rang leaderboard
//   }
//
// Données fournies par XpService.GetProgress() côté backend.
// ============================================================

var DISPLAY_DURATION_MS = 8000;  // durée d'affichage avant auto-dismiss
var dismissTimer = null;

// ---- API publique ----

/**
 * Affiche la card avec les données du joueur.
 * Appelé par Streamer.bot via ExecuteJavaScript.
 * @param {Object} data
 */
window.showCard = function (data) {
  if (!data || !data.username) {
    console.warn('[Card] ⚠️ showCard — données invalides :', data);
    return;
  }
  console.log('[Card] 🎨 Affichage :', data.username, '| Niv.', data.level, '| Rang #', data.rank, '| XP', data.xpCurrent, '/', data.xpForNext);

  cancelDismiss();
  populate(data);
  animateIn();

  dismissTimer = setTimeout(function () {
    window.hideCard();
  }, DISPLAY_DURATION_MS);
};

/**
 * Masque la card immédiatement (sortie animée).
 * Peut être appelé manuellement par Streamer.bot.
 */
window.hideCard = function () {
  cancelDismiss();
  animateOut();
};

/**
 * Change le thème. Thèmes disponibles : default | blue | red | green
 * @param {string} theme
 */
window.setTheme = function (theme) {
  document.body.className = 'theme-' + theme;
};

// ---- Remplissage DOM ----

function populate(data) {
  setText('username', data.username || '---');
  setText('level',    data.level    || '?');
  setText('rank',     data.rank     || '?');

  setAvatar(data.avatar || '');
  setXpBar(data.xpCurrent || 0, data.xpForNext || 1);
  setXpText(data.xpCurrent || 0, data.xpForNext || 1, data.level || 1);
}

function setText(id, value) {
  var el = document.getElementById(id);
  if (el) el.textContent = value;
}

function setAvatar(src) {
  var img = document.getElementById('avatar');
  if (!img) return;
  img.src = src;
  img.alt = '';
}

function setXpBar(current, forNext) {
  var fill = document.getElementById('xp-fill');
  if (!fill) return;

  var ratio = forNext > 0 ? Math.min(current / forNext, 1) : 0;

  // Reset à 0 sans transition pour repartir proprement
  fill.style.transition = 'none';
  fill.style.transform  = 'scaleX(0)';

  // Lecture offsetWidth force un reflow minimal pour valider le reset
  void fill.offsetWidth;

  // Réactive la transition et anime vers la valeur cible
  fill.style.transition = '';
  fill.style.transform  = 'scaleX(' + ratio.toFixed(4) + ')';
}

function setXpText(current, forNext, level) {
  setText('xp-current', formatXp(current) + ' XP');
  setText('xp-next',    'Niv. ' + (level + 1) + ' (' + formatXp(forNext - current) + ' manquants)');
}

function formatXp(xp) {
  if (xp >= 1000000) return (xp / 1000000).toFixed(1) + 'M';
  if (xp >= 1000)    return (xp / 1000).toFixed(1) + 'k';
  return String(xp);
}

// ---- Animations ----

function animateIn() {
  var card = document.getElementById('card');
  if (!card) return;

  card.classList.remove('hidden', 'leaving', 'entering');
  void card.offsetWidth;
  card.classList.add('entering');

  card.addEventListener('animationend', function onIn() {
    card.classList.remove('entering');
    card.removeEventListener('animationend', onIn);
  });
}

function animateOut() {
  var card = document.getElementById('card');
  if (!card) return;

  card.classList.remove('entering');
  card.classList.add('leaving');

  card.addEventListener('animationend', function onOut() {
    card.classList.remove('leaving');
    card.classList.add('hidden');
    resetBar();
    card.removeEventListener('animationend', onOut);
  });
}

function resetBar() {
  var fill = document.getElementById('xp-fill');
  if (fill) {
    fill.style.transition = 'none';
    fill.style.transform  = 'scaleX(0)';
  }
}

function cancelDismiss() {
  if (dismissTimer !== null) {
    clearTimeout(dismissTimer);
    dismissTimer = null;
  }
}

// ---- Réception depuis Streamer.bot (obs-browser emit_event) ----
// CPH.ObsSendRaw("CallVendorRequest", ...) émet un CustomEvent sur window.
// e.detail contient directement le CardPayload object.

// ---- WebSocket Streamer.bot ----
// Reçoit { event: 'showCard', card: {...} } ou { event: 'hideCard' } via General.Custom

(function connectWS() {
  var ws = new WebSocket('ws://127.0.0.1:8080/');

  ws.addEventListener('open', function () {
    console.log('[Card] ✅ WebSocket connecté à Streamer.bot');
    ws.send(JSON.stringify({
      request: 'Subscribe',
      id: 'card-overlay',
      events: { General: ['Custom'] }
    }));
  });

  ws.addEventListener('message', function (e) {
    try {
      var msg = JSON.parse(e.data);
      if (msg.data)                                 msg = msg.data;
      if (typeof msg === 'string')                  msg = JSON.parse(msg);
      if (msg.data && typeof msg.data === 'string') msg = JSON.parse(msg.data);

      var ev = (msg.event || '').toLowerCase();

      if (ev === 'showcard') {
        console.log('[Card] 📨 showCard reçu :', msg.card);
        window.showCard(msg.card);
        return;
      }
      if (ev === 'hidecard') {
        console.log('[Card] 📨 hideCard reçu');
        window.hideCard();
      }
    } catch (err) {
      console.error('[Card] ❌ Erreur parsing:', err);
    }
  });

  ws.addEventListener('close', function () {
    console.warn('[Card] ⚠️ WebSocket fermé — reconnexion dans 2s');
    setTimeout(connectWS, 2000);
  });

  ws.addEventListener('error', function () {
    console.error('[Card] ❌ WebSocket erreur — Streamer.bot est-il lancé ?');
  });
}());

// ---- Checks DOM au démarrage ----

(function init() {
  var required = ['card', 'avatar', 'username', 'level', 'rank', 'xp-fill', 'xp-current', 'xp-next'];

  var missing = required.filter(function (id) {
    return !document.getElementById(id);
  });

  if (missing.length > 0) {
    console.error('[Card] ❌ Éléments DOM manquants :', missing.join(', '));
  } else {
    console.log('[Card] ✅ DOM OK');
  }

  console.log('[Card] ✅ Prêt — connexion WebSocket en cours...');
  console.log('[Card]    Dev mode : ajouter ?dev à l\'URL');
}());

// ---- Dev mode ----
// Ouvrir card.html?dev dans un navigateur pour prévisualiser

if (new URLSearchParams(window.location.search).has('dev')) {
  window.showCard({
    username:   'Mystya',
    avatar:     '',
    level:      12,
    xpCurrent:  183,
    xpForNext:  520,
    rank:       4
  });
}
