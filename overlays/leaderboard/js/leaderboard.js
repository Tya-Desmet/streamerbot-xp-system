'use strict';

// ============================================================
// Changement de thème dynamique
// Usage : window.setTheme('rpg') | URL : ?theme=cyber
// Depuis Streamer.bot : event setTheme + theme: "minimal"
// ============================================================
var VALID_THEMES = ['default', 'rpg', 'cyber', 'minimal', 'tokyo', 'sakura'];

window.setTheme = function (theme) {
  if (VALID_THEMES.indexOf(theme) === -1) {
    console.warn('[LB] ⚠️ Thème inconnu :', theme, '— valides :', VALID_THEMES.join(', '));
    return;
  }
  var link = document.getElementById('theme-css');
  if (!link) {
    console.warn('[LB] ⚠️ Élément #theme-css introuvable dans le DOM');
    return;
  }
  link.href = 'themes/' + theme + '/theme.css';
  document.documentElement.setAttribute('data-theme', theme);
  console.log('[LB] 🎨 Thème :', theme);
};

// Timings (ms depuis le démarrage)
var T_PANEL     =    0;
var T_HEADER    =  120;
var T_PODIUM    =  280;
var T_LIST      = 2300;
var DISPLAY     = 10000;
var DISMISS_DUR =  460;

var _timers = [];

function _els() {
  return {
    panel:  document.getElementById('panel'),
    header: document.getElementById('header'),
    podium: document.getElementById('podium'),
    list:   document.getElementById('list'),
  };
}

function _resetAll() {
  cancelAll(_timers);
  _timers = [];
  State.cancelDismiss();

  var e = _els();
  e.panel.classList.remove('anim-in', 'anim-out');
  e.header.classList.remove('anim-in');
  e.panel.style.opacity = '0';
  resetPodium(e.podium);
  resetRows(e.list);
  State.reset();
}

function _dismiss() {
  if (State.status === 'hiding') return;
  State.set('hiding');

  var e = _els();
  e.panel.classList.remove('anim-in');

  requestAnimationFrame(function () {
    requestAnimationFrame(function () {
      e.panel.classList.add('anim-out');
      setTimeout(function () {
        e.panel.style.opacity = '0';
        e.panel.classList.remove('anim-out');
        State.set('idle');
      }, DISMISS_DUR);
    });
  });
}

function _run(players) {
  State.set('running');
  _timers = [];

  var e = _els();

  // Lignes pré-construites (opacity:0 en CSS) — le panel atteint
  // sa hauteur finale avant l'animation, sans saut visuel.
  var rows = buildRows(e.list, players);

  _timers.push(setTimeout(function () {
    e.panel.style.opacity = '';
    requestAnimationFrame(function () {
      requestAnimationFrame(function () {
        e.panel.classList.add('anim-in');
      });
    });
  }, T_PANEL));

  _timers.push(setTimeout(function () {
    revealEl(e.header);
  }, T_HEADER));

  _timers.push(setTimeout(function () {
    var t = renderPodium(e.podium, players, function () {});
    t.forEach(function (id) { _timers.push(id); });
  }, T_PODIUM));

  _timers.push(setTimeout(function () {
    var t = revealRows(rows, function () { State.set('visible'); });
    t.forEach(function (id) { _timers.push(id); });
  }, T_PODIUM + T_LIST));

  State.scheduleDismiss(T_PODIUM + T_LIST + (6 * TOP10_GAP) + 380 + DISPLAY, _dismiss);
}

window.updateLeaderboard = function (payload) {
  var players = Array.isArray(payload) ? payload
              : (payload && Array.isArray(payload.players)) ? payload.players
              : null;

  if (!players || players.length === 0) {
    console.warn('[LB] Données invalides :', payload);
    return;
  }

  // Diagnostic : ouvrir la console OBS (F12) pour voir le nombre de joueurs reçus
  console.log('[LB] ' + players.length + ' joueur(s) reçu(s) :', players.map(function (p) { return p.name || p.username; }));

  if (State.status === 'hiding') {
    setTimeout(function () { window.updateLeaderboard(payload); }, DISMISS_DUR + 80);
    return;
  }

  if (State.status === 'running' || State.status === 'visible') {
    _resetAll();
    setTimeout(function () { _run(players); }, 120);
    return;
  }

  _run(players);
};

(function init() {
  var e = _els();
  var missing = Object.keys(e).filter(function (k) { return !e[k]; });

  if (missing.length) {
    console.error('[LB] DOM manquant :', missing.join(', '));
    return;
  }

  e.panel.style.opacity = '0';

  // Thème depuis l'URL (?theme=rpg) — doit être dans VALID_THEMES
  var params = new URLSearchParams(window.location.search);
  var urlTheme = params.get('theme');
  if (urlTheme) window.setTheme(urlTheme);

  if (location.search.indexOf('dev') !== -1) {
    var fake = ['Tyrael','Solara','Kairon','Vesper','Dusk','Nyra','Thorin','Elara','Zephyr','Mira']
      .map(function (name, i) { return { name: name, level: 50 - i * 3 }; });
    setTimeout(function () { window.updateLeaderboard(fake); }, 1000);
  }
}());
