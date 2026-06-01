'use strict';

var _params = new URLSearchParams(window.location.search);
var _wsPort = _params.get('wsport') || '8080';
var WS_URL  = 'ws://127.0.0.1:' + _wsPort + '/';

function connectCardWS() {
  var ws = new WebSocket(WS_URL);

  ws.addEventListener('open', function () {
    console.log('[Card] ✅ WebSocket connecté à Streamer.bot');
    ws.send(JSON.stringify({
      request: 'Subscribe',
      id:      'card-overlay',
      events:  { General: ['Custom'] }
    }));
  });

  ws.addEventListener('message', function (e) {
    try {
      var msg = JSON.parse(e.data);
      if (msg.data)                                 msg = msg.data;
      if (typeof msg === 'string')                  msg = JSON.parse(msg);
      if (msg.data && typeof msg.data === 'string') msg = JSON.parse(msg.data);

      var ev = (msg.event || '').toLowerCase();

      if (ev === 'showcard' && typeof window.showCard === 'function') {
        console.log('[Card] 📨 showCard reçu :', msg.card);
        window.showCard(msg.card);
      } else if (ev === 'hidecard' && typeof window.hideCard === 'function') {
        console.log('[Card] 📨 hideCard reçu');
        window.hideCard();
      } else if (ev === 'settheme' && typeof window.setTheme === 'function') {
        console.log('[Card] 🎨 setTheme reçu :', msg.theme);
        window.setTheme(msg.theme);
      }
    } catch (err) {
      console.error('[Card] ❌ Erreur parsing :', err);
    }
  });

  ws.addEventListener('close', function () {
    console.warn('[Card] ⚠️ WebSocket fermé — reconnexion dans 2s');
    setTimeout(connectCardWS, 2000);
  });

  ws.addEventListener('error', function () {
    console.error('[Card] ❌ WebSocket erreur — Streamer.bot est-il lancé ?');
  });
}

connectCardWS();
