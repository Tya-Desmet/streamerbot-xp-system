'use strict';

var _params             = new URLSearchParams(window.location.search);
var DISPLAY_DURATION_MS = parseInt(_params.get('display') || '5000', 10);
var _dismissTimer       = null;

// ---- Animations entrée / sortie ----

function animateIn() {
  var container = document.getElementById('checkin-container');
  container.classList.remove('visible');
  container.classList.remove('hidden');

  // Double rAF — garantit le reset de la transition avant d'ajouter .visible
  requestAnimationFrame(function () {
    requestAnimationFrame(function () {
      container.classList.add('visible');
    });
  });
}

function animateOut() {
  var container = document.getElementById('checkin-container');
  container.classList.remove('visible');
  setTimeout(function () {
    container.classList.add('hidden');
    container.classList.remove('card-complete');
  }, 500);
}

// ---- API publique ----

window.showCheckIn = function (data) {
  if (!data || !data.username) {
    console.warn('[CheckIn] showCheckIn — données invalides :', data);
    return;
  }
  console.log(
    '[CheckIn] Affichage :', data.username,
    '| XP +' + (data.xpGained || 0),
    '| Cases', (data.count || 0) + '/' + (data.cardSize || 10),
    data.event === 'checkIn_cardComplete' ? '| CARTE COMPLÈTE' : ''
  );

  if (_dismissTimer !== null) {
    clearTimeout(_dismissTimer);
    _dismissTimer = null;
  }

  populate(data); // renderer.js
  animateIn();

  var duration = data.durationMs || DISPLAY_DURATION_MS;
  _dismissTimer = setTimeout(function () {
    window.hideCheckIn();
  }, duration);
};

window.hideCheckIn = function () {
  if (_dismissTimer !== null) {
    clearTimeout(_dismissTimer);
    _dismissTimer = null;
  }
  animateOut();
};

var VALID_THEMES = ['default', 'rpg', 'cyber', 'minimal', 'tokyo', 'sakura', 'myastya-samourai'];

// Thèmes embarquant un module d'effets (canvas pétales + étincelles + son).
var EFFECTS_THEMES = ['myastya-samourai'];

function currentThemeFromLink() {
  var link = document.getElementById('theme-css');
  var m = link && /themes\/([^/]+)\/theme\.css/.exec(link.getAttribute('href') || '');
  return m ? m[1] : 'default';
}

function loadThemeEffects(theme) {
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
  s.onerror = function () { console.warn('[CheckIn] effects.js introuvable pour', theme); };
  document.body.appendChild(s);
}

window.setTheme = function (theme) {
  if (VALID_THEMES.indexOf(theme) === -1) {
    console.warn('[CheckIn] Thème inconnu :', theme, '— valides :', VALID_THEMES.join(', '));
    return;
  }
  var link = document.getElementById('theme-css');
  if (!link) {
    console.warn('[CheckIn] Élément #theme-css introuvable');
    return;
  }
  link.href = 'themes/' + theme + '/theme.css';
  document.documentElement.setAttribute('data-theme', theme);
  loadThemeEffects(theme);
  console.log('[CheckIn] Thème :', theme);
};

// ---- Init ----

(function init() {
  var required = [
    'checkin-container', 'checkin-icon',     'checkin-header',
    'checkin-username',  'checkin-xp',        'checkin-boxes',
    'checkin-progress-label',                 'checkin-card-complete'
  ];

  var missing = required.filter(function (id) { return !document.getElementById(id); });
  if (missing.length > 0) {
    console.error('[CheckIn] Éléments DOM manquants :', missing.join(', '));
  } else {
    console.log('[CheckIn] DOM OK');
  }

  var urlTheme = _params.get('theme');
  window.setTheme(urlTheme || currentThemeFromLink());

  console.log('[CheckIn] Prêt — en attente de Streamer.bot');
  console.log('[CheckIn]    Thème : window.setTheme("rpg") | ?theme=cyber');
  console.log('[CheckIn]    Durée : ?display=5000 (ms)');
}());
