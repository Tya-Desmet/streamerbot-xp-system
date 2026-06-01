'use strict';

var _params             = new URLSearchParams(window.location.search);
var DISPLAY_DURATION_MS = parseInt(_params.get('display') || '8000', 10);
var dismissTimer        = null;

function cancelDismiss() {
  if (dismissTimer !== null) {
    clearTimeout(dismissTimer);
    dismissTimer = null;
  }
}

// ---- API publique ----

window.showCard = function (data) {
  if (!data || !data.username) {
    console.warn('[Card] showCard — données invalides :', data);
    return;
  }
  console.log(
    '[Card] Affichage :', data.username,
    '| Titre :', data.title || '(aucun)',
    '| Niv.', data.level,
    '| Rang #', data.rank,
    '| XP', data.xpCurrent, '/', data.xpForNext,
    '| Msg', data.messages,
    '| Watch', data.watchTime, 'min'
  );

  cancelDismiss();

  // Flash si la card est déjà visible (re-affichage), sinon animation d'entrée
  var card = document.getElementById('card');
  var isVisible = card && !card.classList.contains('hidden');

  populate(data);  // renderer.js

  if (isVisible) {
    flashCard();   // renderer.js — flash rapide sans ré-animer l'entrée
  } else {
    animateIn();   // animations.js
  }

  dismissTimer = setTimeout(function () {
    window.hideCard();
  }, DISPLAY_DURATION_MS);
};

window.hideCard = function () {
  cancelDismiss();
  animateOut(); // animations.js
};

var VALID_THEMES = ['default', 'rpg', 'cyber', 'minimal', 'tokyo', 'sakura', 'myastya-samourai'];

// Thèmes embarquant un module d'effets (canvas pétales + étincelles + son).
// Chargé/déchargé dynamiquement via themes/<theme>/effects.js.
var EFFECTS_THEMES = ['myastya-samourai'];

function currentThemeFromLink() {
  var link = document.getElementById('theme-css');
  var m = link && /themes\/([^/]+)\/theme\.css/.exec(link.getAttribute('href') || '');
  return m ? m[1] : 'default';
}

function loadThemeEffects(theme) {
  // Décharge proprement les effets du thème précédent (canvas, audio, listeners)
  if (window.__themeFx && typeof window.__themeFx.destroy === 'function') {
    try { window.__themeFx.destroy(); } catch (e) { /* noop */ }
  }
  window.__themeFx = null;

  var old = document.getElementById('theme-js');
  if (old && old.parentNode) old.parentNode.removeChild(old);

  if (EFFECTS_THEMES.indexOf(theme) === -1) return;

  var s = document.createElement('script');
  s.id  = 'theme-js';
  s.src = 'themes/' + theme + '/effects.js';
  s.onerror = function () { console.warn('[Card] effects.js introuvable pour', theme); };
  document.body.appendChild(s);
}

window.setTheme = function (theme) {
  if (VALID_THEMES.indexOf(theme) === -1) {
    console.warn('[Card] Thème inconnu :', theme, '— valides :', VALID_THEMES.join(', '));
    return;
  }
  var link = document.getElementById('theme-css');
  if (!link) {
    console.warn('[Card] Élément #theme-css introuvable');
    return;
  }
  link.href = 'themes/' + theme + '/theme.css';
  document.documentElement.setAttribute('data-theme', theme);
  loadThemeEffects(theme);
  console.log('[Card] Thème :', theme);
};

// ---- Vérifications au démarrage ----

(function init() {
  // IDs obligatoires — présence vérifiée au démarrage
  var required = [
    'card', 'avatar', 'username', 'card-title',
    'level', 'rank',
    'messages', 'watchtime',
    'xp-fill', 'xp-current', 'xp-next'
  ];

  var missing = required.filter(function (id) { return !document.getElementById(id); });

  if (missing.length > 0) {
    console.error('[Card] Éléments DOM manquants :', missing.join(', '));
  } else {
    console.log('[Card] DOM OK');
  }

  // Thème depuis le paramètre URL (?theme=rpg), sinon celui déjà dans le <link>.
  // On passe toujours par setTheme pour charger les effets éventuels du thème actif.
  var params   = new URLSearchParams(window.location.search);
  var urlTheme = params.get('theme');
  window.setTheme(urlTheme || currentThemeFromLink());

  console.log('[Card] Prêt — en attente de Streamer.bot');
  console.log('[Card]    Thème : window.setTheme("rpg") | ?theme=cyber');
}());
