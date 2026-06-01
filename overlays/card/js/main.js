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

var VALID_THEMES = ['default', 'rpg', 'cyber', 'minimal', 'tokyo', 'sakura'];

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

  // Thème depuis le paramètre URL (?theme=rpg)
  var params   = new URLSearchParams(window.location.search);
  var urlTheme = params.get('theme');
  if (urlTheme) window.setTheme(urlTheme);

  console.log('[Card] Prêt — en attente de Streamer.bot');
  console.log('[Card]    Thème : window.setTheme("rpg") | ?theme=cyber');
}());
