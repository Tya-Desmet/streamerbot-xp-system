/* ============================================================
   Leaderboard — Effets MYASTYA-SAMOURAI
   Pétales de cerisier (canvas) + étincelles + fleurs + son.
   Chargé/déchargé automatiquement par leaderboard.js (loadThemeEffects).
   Son : synthèse Web Audio par défaut, remplaçable par fichiers via sounds.json.
         Actif par défaut — couper avec ?mute ou window.sfx.mute().
   ============================================================ */
(function () {
  'use strict';

  var LOG       = '[LB FX]';
  var THEME_DIR = 'themes/myastya-samourai/';

  if (window.__themeFx && typeof window.__themeFx.destroy === 'function') {
    try { window.__themeFx.destroy(); } catch (e) { /* noop */ }
  }

  /* ========================================================
     1) SON — synthèse Web Audio + override fichiers (sounds.json)
     ======================================================== */
  var SFX = (function () {
    var SOUND_KEYS = ['appear', 'petal', 'spark', 'fill', 'tick', 'levelup', 'milestone', 'complete', 'reveal'];
    var muted   = /[?&]mute(?:[=&]|$)/i.test(location.search);
    var ctx = null, master = null;
    var buffers = {}, fileVol = {};

    function ac() {
      if (ctx) return ctx;
      var AC = window.AudioContext || window.webkitAudioContext;
      if (!AC) return null;
      ctx = new AC();
      master = ctx.createGain();
      master.gain.value = muted ? 0 : 0.6;
      master.connect(ctx.destination);
      return ctx;
    }
    function resume() { if (ctx && ctx.state === 'suspended') ctx.resume(); }

    function tone(o, vol) {
      var c = ac(); if (!c) return;
      var t0 = c.currentTime + (o.delay || 0);
      var osc = c.createOscillator(), g = c.createGain();
      osc.type = o.type || 'sine';
      osc.frequency.setValueAtTime(o.f0, t0);
      if (o.f1 != null) osc.frequency.exponentialRampToValueAtTime(Math.max(1, o.f1), t0 + o.dur);
      g.gain.setValueAtTime(0.0001, t0);
      g.gain.exponentialRampToValueAtTime((o.gain || 0.2) * (vol == null ? 1 : vol), t0 + (o.attack || 0.012));
      g.gain.exponentialRampToValueAtTime(0.0001, t0 + o.dur);
      osc.connect(g); g.connect(master);
      osc.start(t0); osc.stop(t0 + o.dur + 0.03);
    }
    function noiseBurst(dur, gain, freq, vol) {
      var c = ac(); if (!c) return;
      var n = Math.floor(c.sampleRate * dur), buf = c.createBuffer(1, n, c.sampleRate), d = buf.getChannelData(0);
      for (var i = 0; i < n; i++) d[i] = (Math.random() * 2 - 1) * (1 - i / n);
      var src = c.createBufferSource(); src.buffer = buf;
      var bp = c.createBiquadFilter(); bp.type = 'bandpass'; bp.frequency.value = freq || 1200; bp.Q.value = 0.7;
      var g = c.createGain(); g.gain.value = (gain || 0.15) * (vol == null ? 1 : vol);
      src.connect(bp); bp.connect(g); g.connect(master); src.start();
    }

    function synth(key, vol) {
      switch (key) {
        case 'appear':    tone({ f0: 523.25, dur: 0.5, gain: 0.18 }, vol); tone({ f0: 783.99, dur: 0.6, gain: 0.10, delay: 0.04 }, vol); break;
        case 'petal':     tone({ f0: 640 + Math.random() * 240, dur: 0.22, gain: 0.06 }, vol); break;
        case 'spark':     tone({ f0: 1600 + Math.random() * 700, dur: 0.09, gain: 0.07, type: 'triangle' }, vol); break;
        case 'fill':      tone({ f0: 300, f1: 760, dur: 0.32, gain: 0.12 }, vol); break;
        case 'tick':      tone({ f0: 520 + Math.random() * 120, dur: 0.10, gain: 0.06 }, vol); break;
        case 'levelup':   [523.25, 659.25, 783.99, 1046.5].forEach(function (f, i) { tone({ f0: f, dur: 0.4, gain: 0.13, delay: i * 0.07 }, vol); }); break;
        case 'milestone': tone({ f0: 659.25, dur: 0.7, gain: 0.16 }, vol); tone({ f0: 987.77, dur: 0.8, gain: 0.09, delay: 0.05 }, vol); break;
        case 'complete':  [523.25, 659.25, 783.99].forEach(function (f) { tone({ f0: f, dur: 1.0, gain: 0.12 }, vol); }); tone({ f0: 1046.5, dur: 1.1, gain: 0.08, delay: 0.08, type: 'triangle' }, vol); noiseBurst(0.5, 0.05, 2400, vol); break;
        case 'reveal':    noiseBurst(0.4, 0.10, 900, vol); tone({ f0: 220, f1: 440, dur: 0.35, gain: 0.07 }, vol); break;
        default:          tone({ f0: 600, dur: 0.12, gain: 0.06 }, vol);
      }
    }

    function play(key, opts) {
      if (muted) return;
      var c = ac(); if (!c) return;
      resume();
      var vol = (opts && opts.volume != null) ? opts.volume : 1;
      if (buffers[key]) {
        var src = c.createBufferSource(); src.buffer = buffers[key];
        var g = c.createGain(); g.gain.value = (fileVol[key] != null ? fileVol[key] : 1) * vol;
        src.connect(g); g.connect(master); src.start();
      } else {
        synth(key, vol);
      }
    }

    function loadFiles() {
      fetch(THEME_DIR + 'sounds.json', { cache: 'no-cache' })
        .then(function (r) { return r.ok ? r.json() : null; })
        .then(function (map) {
          if (!map) return;
          Object.keys(map).forEach(function (key) {
            if (SOUND_KEYS.indexOf(key) === -1) return;
            var entry = map[key]; if (!entry || !entry.src) return;
            if (entry.volume != null) fileVol[key] = entry.volume;
            fetch(THEME_DIR + entry.src, { cache: 'no-cache' })
              .then(function (r) { if (!r.ok) throw 0; return r.arrayBuffer(); })
              .then(function (ab) { var c = ac(); return c ? c.decodeAudioData(ab) : Promise.reject(); })
              .then(function (buf) { buffers[key] = buf; console.log(LOG, 'son fichier chargé :', key); })
              .catch(function () { console.warn(LOG, 'son fichier illisible (' + key + ') → synthèse'); });
          });
        })
        .catch(function () { /* pas de sounds.json → 100% synthèse */ });
    }
    loadFiles();

    return {
      play: play,
      mute:    function () { muted = true;  if (master) master.gain.value = 0; },
      unmute:  function () { muted = false; if (master) master.gain.value = 0.6; resume(); },
      enable:  function () { this.unmute(); },
      disable: function () { this.mute(); },
      isMuted: function () { return muted; },
      setVolume: function (v) { if (master) master.gain.value = Math.max(0, Math.min(1, v)); },
      _close:  function () { try { if (ctx) ctx.close(); } catch (e) {} ctx = null; }
    };
  })();
  window.sfx = SFX;

  /* ========================================================
     2) CANVAS — pétales de cerisier + étincelles
     ======================================================== */
  var canvas = document.createElement('canvas');
  canvas.className = 'mys-fx-canvas';
  document.body.appendChild(canvas);
  var cx = canvas.getContext('2d');
  var sparks = [], petals = [], rafId = null, lastPetal = 0;

  function resize() { canvas.width = window.innerWidth; canvas.height = window.innerHeight; }
  resize();
  window.addEventListener('resize', resize);

  function Petal() {
    this.x = Math.random() * canvas.width;
    this.y = -12 - Math.random() * 40;
    this.vx = (Math.random() - 0.5) * 0.5;
    this.vy = 0.5 + Math.random() * 0.9;
    this.size = 4 + Math.random() * 5;
    this.rot = Math.random() * Math.PI * 2;
    this.rotSpd = (Math.random() - 0.5) * 0.06;
    this.sway = Math.random() * Math.PI * 2;
    var hue = 330 + Math.random() * 22;
    this.color = 'hsl(' + hue + ',' + (55 + Math.random() * 20) + '%,' + (72 + Math.random() * 15) + '%)';
  }
  Petal.prototype.update = function () {
    this.sway += 0.02;
    this.x += this.vx + Math.sin(this.sway) * 0.5;
    this.y += this.vy;
    this.rot += this.rotSpd;
    return this.y < canvas.height + 20;
  };
  Petal.prototype.draw = function (c) {
    c.save(); c.translate(this.x, this.y); c.rotate(this.rot);
    c.globalAlpha = 0.55; c.fillStyle = this.color;
    c.beginPath(); c.ellipse(0, 0, this.size * 0.55, this.size, 0, 0, Math.PI * 2); c.fill();
    c.globalAlpha = 0.22; c.fillStyle = '#fff';
    c.beginPath(); c.ellipse(-this.size * 0.12, -this.size * 0.3, this.size * 0.18, this.size * 0.34, 0, 0, Math.PI * 2); c.fill();
    c.restore();
  };

  function Spark(x, y) {
    var a = Math.random() * Math.PI * 2, spd = 1.4 + Math.random() * 3.4;
    this.x = x + (Math.random() - 0.5) * 8;
    this.y = y + (Math.random() - 0.5) * 8;
    this.vx = Math.cos(a) * spd; this.vy = Math.sin(a) * spd - 0.4;
    this.life = 0; this.maxLife = 16 + Math.random() * 18; this.size = 2 + Math.random() * 3.5;
    this.rot = Math.random() * Math.PI;
    var pal = ['#FFFFFF', '#FFE0EE', '#FFB0CC', '#FF80AA', '#F060A0'];
    this.color = pal[(Math.random() * pal.length) | 0];
  }
  Spark.prototype.alpha = function () { var t = this.life / this.maxLife; return t < 0.15 ? t / 0.15 : 1 - (t - 0.15) / 0.85; };
  Spark.prototype.update = function () { this.life++; this.x += this.vx; this.y += this.vy; this.vx *= 0.92; this.vy = this.vy * 0.92 + 0.06; this.rot += 0.05; return this.life < this.maxLife; };
  Spark.prototype.draw = function (c) {
    var a = Math.max(0, this.alpha()); if (!a) return;
    c.save(); c.globalAlpha = a; c.translate(this.x, this.y); c.rotate(this.rot);
    c.shadowColor = this.color; c.shadowBlur = 6; c.strokeStyle = this.color; c.lineWidth = 1.4; c.lineCap = 'round';
    var s = this.size, d = s * 0.52;
    c.beginPath();
    c.moveTo(0, -s); c.lineTo(0, s); c.moveTo(-s, 0); c.lineTo(s, 0);
    c.moveTo(-d, -d); c.lineTo(d, d); c.moveTo(d, -d); c.lineTo(-d, d);
    c.stroke();
    c.fillStyle = '#fff'; c.shadowBlur = 10; c.beginPath(); c.arc(0, 0, s * 0.22, 0, Math.PI * 2); c.fill();
    c.restore();
  };

  function burst(x, y, n) { for (var i = 0; i < n; i++) sparks.push(new Spark(x, y)); }
  function burstAtEl(el, n, where) {
    if (!el) return;
    var r = el.getBoundingClientRect();
    if (!r.width && !r.height) return;
    var x = where === 'right' ? r.right : r.left + r.width / 2;
    burst(x, r.top + r.height / 2, n || 18);
  }

  function loop(ts) {
    rafId = requestAnimationFrame(loop);
    cx.clearRect(0, 0, canvas.width, canvas.height);
    if (ts - lastPetal > 520 && petals.length < 46) { petals.push(new Petal()); lastPetal = ts; }
    var i;
    for (i = petals.length - 1; i >= 0; i--) { if (!petals[i].update()) petals.splice(i, 1); else petals[i].draw(cx); }
    for (i = sparks.length - 1; i >= 0; i--) { if (!sparks[i].update()) sparks.splice(i, 1); else sparks[i].draw(cx); }
  }
  rafId = requestAnimationFrame(loop);

  /* ========================================================
     3) FLEURS de cerisier décoratives (SVG)
     ======================================================== */
  var flowerEls = [];
  function makeFlower(idx) {
    var petalsSvg = [0, 72, 144, 216, 288].map(function (deg) {
      return '<path transform="rotate(' + deg + ')" d="M0,-17 C9,-14 11,-4 7,4 Q0,11 -7,4 C-11,-4 -9,-14 0,-17Z" fill="url(#mys-pg' + idx + ')"/>';
    }).join('');
    var svg =
      '<svg viewBox="-32 -32 64 64" xmlns="http://www.w3.org/2000/svg">' +
        '<defs>' +
          '<radialGradient id="mys-pg' + idx + '" cx="50%" cy="22%" r="68%"><stop offset="0%" stop-color="#FFCCE0"/><stop offset="100%" stop-color="#C83A64"/></radialGradient>' +
          '<radialGradient id="mys-cg' + idx + '" cx="50%" cy="40%" r="60%"><stop offset="0%" stop-color="#FFFFFF"/><stop offset="100%" stop-color="#FFB8D4"/></radialGradient>' +
        '</defs>' + petalsSvg +
        '<circle cx="0" cy="0" r="8" fill="#FFD8EC"/><circle cx="0" cy="0" r="4.5" fill="url(#mys-cg' + idx + ')"/>' +
        '<line x1="0" y1="-4.5" x2="0" y2="-11" stroke="#F080A8" stroke-width="1" stroke-linecap="round"/>' +
        '<line x1="-3" y1="-3.5" x2="-6" y2="-9" stroke="#F080A8" stroke-width="1" stroke-linecap="round"/>' +
        '<line x1="3" y1="-3.5" x2="6" y2="-9" stroke="#F080A8" stroke-width="1" stroke-linecap="round"/>' +
        '<circle cx="0" cy="-11" r="2" fill="#E86090"/><circle cx="-6" cy="-9" r="2" fill="#E86090"/><circle cx="6" cy="-9" r="2" fill="#E86090"/>' +
      '</svg>';
    var d = document.createElement('div');
    d.className = 'mys-flower';
    d.innerHTML = svg;
    return d;
  }
  // PER-OVERLAY : deux fleurs aux coins hauts du panneau
  var FLOWERS = [
    { parent: '.panel', style: { position: 'absolute', left: '-22px', top: '-24px', width: '64px', height: '64px', zIndex: '5' } },
    { parent: '.panel', style: { position: 'absolute', right: '-22px', top: '-24px', width: '64px', height: '64px', zIndex: '5' } }
  ];
  function buildFlowers() {
    FLOWERS.forEach(function (cfg, i) {
      var parent = document.querySelector(cfg.parent);
      if (!parent) return;
      var f = makeFlower(i);
      Object.keys(cfg.style).forEach(function (k) { f.style[k] = cfg.style[k]; });
      parent.appendChild(f);
      flowerEls.push(f);
    });
  }
  buildFlowers();
  // Le panneau peut être (re)créé après coup → ré-ancre les fleurs si besoin
  var flowerRetry = setInterval(function () {
    if (!document.querySelector('.panel')) return;
    if (flowerEls.length) { clearInterval(flowerRetry); return; }
    buildFlowers();
  }, 600);

  /* ========================================================
     4) HOOKS — branchés sur les animations de révélation existantes
     ======================================================== */
  function onAnim(e) {
    switch (e.animationName) {
      case 'panelReveal':     SFX.play('reveal'); break;
      case 'slotRevealFirst': SFX.play('milestone'); burstAtEl(e.target, 26, 'center'); break;
      case 'slotReveal':      SFX.play('tick', { volume: 0.7 }); burstAtEl(e.target, 8, 'center'); break;
      case 'rowReveal':       SFX.play('tick', { volume: 0.4 }); break;
    }
  }

  document.addEventListener('animationstart', onAnim, true);
  function resumeOnGesture() { if (window.sfx) window.sfx.play('tick', { volume: 0 }); }
  document.addEventListener('pointerdown', resumeOnGesture, { once: true });
  document.addEventListener('keydown', resumeOnGesture, { once: true });

  /* ========================================================
     5) DESTROY — nettoyage complet au changement de thème
     ======================================================== */
  function destroy() {
    if (rafId) cancelAnimationFrame(rafId);
    if (flowerRetry) clearInterval(flowerRetry);
    window.removeEventListener('resize', resize);
    document.removeEventListener('animationstart', onAnim, true);
    document.removeEventListener('pointerdown', resumeOnGesture);
    document.removeEventListener('keydown', resumeOnGesture);
    if (canvas.parentNode) canvas.parentNode.removeChild(canvas);
    flowerEls.forEach(function (f) { if (f.parentNode) f.parentNode.removeChild(f); });
    flowerEls = []; sparks = []; petals = [];
    if (SFX && SFX._close) SFX._close();
    window.sfx = null;
  }
  window.__themeFx = { destroy: destroy };

  console.log(LOG, 'Effets myastya-samourai actifs — pétales + étincelles + son', SFX.isMuted() ? '(muet)' : '');
})();
