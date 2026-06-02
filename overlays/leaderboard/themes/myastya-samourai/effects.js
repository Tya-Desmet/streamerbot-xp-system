/* ============================================================
   Leaderboard — Effets MYASTYA-SAMOURAI v2
   Déclenchement UNIQUEMENT sur les animations existantes.
   - panelReveal     → burst d'étincelles depuis le haut du panel
   - slotRevealFirst → grand pétale prize + burst sur le n°1
   - slotReveal      → petit burst sur les n°2 / n°3
   - rowReveal       → micro-burst sur les lignes 4-10
   Aucun effet permanent en fond de stream.
   ============================================================ */
(function () {
  'use strict';

  var LOG       = '[LB FX]';
  var THEME_DIR = 'themes/myastya-samourai/';

  if (window.__themeFx && typeof window.__themeFx.destroy === 'function') {
    try { window.__themeFx.destroy(); } catch (e) { /* noop */ }
  }

  /* ========================================================
     1) SON
     ======================================================== */
  var SFX = (function () {
    var SOUND_KEYS = ['appear', 'petal', 'spark', 'fill', 'tick', 'levelup', 'milestone', 'complete', 'reveal'];
    var muted  = /[?&]mute(?:[=&]|$)/i.test(location.search);
    var ctx = null, master = null, buffers = {}, fileVol = {};

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
      var gn = c.createGain(); gn.gain.value = (gain || 0.15) * (vol == null ? 1 : vol);
      src.connect(bp); bp.connect(gn); gn.connect(master); src.start();
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
        var gn = c.createGain(); gn.gain.value = (fileVol[key] != null ? fileVol[key] : 1) * vol;
        src.connect(gn); gn.connect(master); src.start();
      } else { synth(key, vol); }
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
              .then(function (buf) { buffers[key] = buf; })
              .catch(function () {});
          });
        }).catch(function () {});
    }
    loadFiles();
    return {
      play: play,
      mute:      function () { muted = true;  if (master) master.gain.value = 0; },
      unmute:    function () { muted = false; if (master) master.gain.value = 0.6; resume(); },
      enable:    function () { this.unmute(); },
      disable:   function () { this.mute(); },
      isMuted:   function () { return muted; },
      setVolume: function (v) { if (master) master.gain.value = Math.max(0, Math.min(1, v)); },
      _close:    function () { try { if (ctx) ctx.close(); } catch (e) {} ctx = null; }
    };
  })();
  window.sfx = SFX;

  /* ========================================================
     2) CANVAS — étincelles en burst uniquement
     ======================================================== */
  var canvas = document.createElement('canvas');
  canvas.className = 'mys-fx-canvas';
  canvas.style.opacity = '0'; // caché par défaut, visible uniquement pendant les bursts
  document.body.appendChild(canvas);
  var cx = canvas.getContext('2d');
  var sparks = [], rafId = null;

  function resize() { canvas.width = window.innerWidth; canvas.height = window.innerHeight; }
  resize();
  window.addEventListener('resize', resize);

  function Spark(x, y, small) {
    var a = Math.random() * Math.PI * 2, spd = small ? (0.8 + Math.random() * 1.8) : (1.6 + Math.random() * 3.6);
    this.x = x + (Math.random() - 0.5) * (small ? 6 : 12);
    this.y = y + (Math.random() - 0.5) * (small ? 6 : 12);
    this.vx = Math.cos(a) * spd; this.vy = Math.sin(a) * spd - (small ? 0.2 : 0.5);
    this.life = 0; this.maxLife = small ? (10 + Math.random() * 10) : (18 + Math.random() * 22);
    this.size = small ? (1 + Math.random() * 2) : (2 + Math.random() * 4);
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
    c.beginPath(); c.moveTo(0,-s); c.lineTo(0,s); c.moveTo(-s,0); c.lineTo(s,0); c.moveTo(-d,-d); c.lineTo(d,d); c.moveTo(d,-d); c.lineTo(-d,d); c.stroke();
    c.fillStyle = '#fff'; c.shadowBlur = 10; c.beginPath(); c.arc(0,0,s*0.22,0,Math.PI*2); c.fill();
    c.restore();
  };

  function burst(x, y, n, small) { for (var i = 0; i < n; i++) sparks.push(new Spark(x, y, small)); if (!rafId) startBurst(); }
  function burstAtEl(el, n, small) {
    if (!el) return;
    var r = el.getBoundingClientRect(); if (!r.width && !r.height) return;
    burst(r.left + r.width / 2, r.top + r.height / 2, n || 16, small);
  }
  function startBurst() {
    canvas.style.opacity = '1';
    rafId = requestAnimationFrame(loop);
  }
  function loop() {
    cx.clearRect(0, 0, canvas.width, canvas.height);
    var i;
    for (i = sparks.length - 1; i >= 0; i--) { if (!sparks[i].update()) sparks.splice(i, 1); else sparks[i].draw(cx); }
    if (sparks.length === 0) { rafId = null; canvas.style.opacity = '0'; return; }
    rafId = requestAnimationFrame(loop);
  }

  /* ========================================================
     3) PÉTALE PRIZE — grand pétale SVG sur le n°1
     ======================================================== */
  var uid = 0;
  var domEls = [];

  function petalSVG(size) {
    var id = 'mys-lb' + (uid++);
    var paths = [0,72,144,216,288].map(function (d) {
      return '<path transform="rotate('+d+')" d="M0,-17 C9,-14 11,-4 7,4 Q0,11 -7,4 C-11,-4 -9,-14 0,-17Z" fill="url(#pp'+id+')"/>';
    }).join('');
    return '<svg viewBox="-32 -32 64 64" width="'+size+'" height="'+size+'" xmlns="http://www.w3.org/2000/svg">' +
      '<defs><radialGradient id="pp'+id+'" cx="50%" cy="22%" r="68%"><stop offset="0%" stop-color="#FFCCE0"/><stop offset="100%" stop-color="#C83A64"/></radialGradient></defs>' +
      paths +
      '<circle cx="0" cy="0" r="6" fill="#FFD8EC"/><circle cx="0" cy="0" r="3.5" fill="#FFB8D4"/>' +
      '</svg>';
  }

  function spawnPrizePetal(el) {
    if (!el) return;
    var r = el.getBoundingClientRect();
    var size = 90;
    var div = document.createElement('div');
    div.className = 'mys-prize-petal';
    div.innerHTML = petalSVG(size);
    div.style.left = (r.left + r.width / 2 - size / 2) + 'px';
    div.style.top  = (r.top  + r.height / 2 - size / 2) + 'px';
    document.body.appendChild(div);
    domEls.push(div);
    function rm() { if (div.parentNode) div.parentNode.removeChild(div); var i = domEls.indexOf(div); if (i >= 0) domEls.splice(i, 1); }
    div.addEventListener('animationend', rm, { once: true });
    setTimeout(rm, 2000);
  }

  /* ========================================================
     4) FLEURS DE CERISIER DÉCORATIVE (coins du panel)
     ======================================================== */
  var flowerEls = [];
  var flowerRetry;

  function makeFlowerSvg(fuid) {
    var paths = [0,72,144,216,288].map(function (d) {
      return '<path transform="rotate('+d+')" d="M0,-17 C9,-14 11,-4 7,4 Q0,11 -7,4 C-11,-4 -9,-14 0,-17Z" fill="url(#fp'+fuid+')"/>';
    }).join('');
    return '<svg viewBox="-32 -32 64 64" xmlns="http://www.w3.org/2000/svg">' +
      '<defs><radialGradient id="fp'+fuid+'" cx="50%" cy="22%" r="68%"><stop offset="0%" stop-color="#FFCCE0"/><stop offset="100%" stop-color="#C83A64"/></radialGradient>' +
      '<radialGradient id="fc'+fuid+'" cx="50%" cy="40%" r="60%"><stop offset="0%" stop-color="#FFFFFF"/><stop offset="100%" stop-color="#FFB8D4"/></radialGradient></defs>' +
      paths +
      '<circle cx="0" cy="0" r="8" fill="#FFD8EC"/><circle cx="0" cy="0" r="4.5" fill="url(#fc'+fuid+')"/>' +
      '<line x1="0" y1="-4.5" x2="0" y2="-11" stroke="#F080A8" stroke-width="1" stroke-linecap="round"/>' +
      '<line x1="-3" y1="-3.5" x2="-6" y2="-9" stroke="#F080A8" stroke-width="1" stroke-linecap="round"/>' +
      '<line x1="3" y1="-3.5" x2="6" y2="-9" stroke="#F080A8" stroke-width="1" stroke-linecap="round"/>' +
      '<circle cx="0" cy="-11" r="2" fill="#E86090"/><circle cx="-6" cy="-9" r="2" fill="#E86090"/><circle cx="6" cy="-9" r="2" fill="#E86090"/>' +
      '</svg>';
  }

  var FLOWER_CFG = [
    { left: '-22px', top: '-24px' },
    { right: '-22px', top: '-24px' }
  ];
  function buildFlowers() {
    var panel = document.querySelector('.panel');
    if (!panel || flowerEls.length) return;
    FLOWER_CFG.forEach(function (cfg, i) {
      var f = document.createElement('div');
      f.className = 'mys-flower';
      f.innerHTML = makeFlowerSvg('lb' + i);
      f.style.position = 'absolute';
      f.style.width = '64px'; f.style.height = '64px';
      if (cfg.left)  f.style.left  = cfg.left;
      if (cfg.right) f.style.right = cfg.right;
      f.style.top = cfg.top;
      panel.appendChild(f);
      flowerEls.push(f);
    });
  }
  buildFlowers();
  // Le panel peut apparaître après le chargement du script → retry
  flowerRetry = setInterval(function () {
    if (flowerEls.length) { clearInterval(flowerRetry); return; }
    buildFlowers();
  }, 500);

  /* ========================================================
     5) HOOKS — animations existantes du leaderboard
     ======================================================== */
  function onAnim(e) {
    var panel = document.getElementById('panel');
    switch (e.animationName) {
      case 'panelReveal':
        SFX.play('reveal');
        if (panel) {
          var r = panel.getBoundingClientRect();
          // burst depuis le haut-centre du panel
          burst(r.left + r.width / 2, r.top + 20, 26);
        }
        break;

      case 'slotRevealFirst':
        SFX.play('milestone');
        spawnPrizePetal(e.target);
        burstAtEl(e.target, 28);
        break;

      case 'slotReveal':
        SFX.play('tick', { volume: 0.65 });
        burstAtEl(e.target, 10);
        break;

      case 'rowReveal':
        SFX.play('tick', { volume: 0.30 });
        burstAtEl(e.target, 5, true); // petits sparks
        break;
    }
  }

  document.addEventListener('animationstart', onAnim, true);

  function resumeOnGesture() { SFX.play('tick', { volume: 0 }); }
  document.addEventListener('pointerdown', resumeOnGesture, { once: true });
  document.addEventListener('keydown',     resumeOnGesture, { once: true });

  /* ========================================================
     6) DESTROY
     ======================================================== */
  function destroy() {
    if (rafId) { cancelAnimationFrame(rafId); rafId = null; }
    if (flowerRetry) clearInterval(flowerRetry);
    window.removeEventListener('resize', resize);
    document.removeEventListener('animationstart', onAnim, true);
    document.removeEventListener('pointerdown', resumeOnGesture);
    document.removeEventListener('keydown',     resumeOnGesture);
    if (canvas.parentNode) canvas.parentNode.removeChild(canvas);
    flowerEls.forEach(function (f) { if (f.parentNode) f.parentNode.removeChild(f); });
    domEls.forEach(function (e) { if (e.parentNode) e.parentNode.removeChild(e); });
    flowerEls = []; sparks = []; domEls = [];
    if (SFX && SFX._close) SFX._close();
    window.sfx = null;
  }
  window.__themeFx = { destroy: destroy };

  console.log(LOG, 'v2 — prize petal + burst on-demand + son. Aucun effet permanent.', SFX.isMuted() ? '(muet)' : '');
})();
