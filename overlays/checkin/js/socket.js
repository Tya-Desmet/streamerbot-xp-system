'use strict';

var _params = new URLSearchParams(window.location.search);
var _wsPort = _params.get('wsport') || '8080';
var WS_URL  = 'ws://127.0.0.1:' + _wsPort + '/';

function connectCheckInWS() {
  var ws = new WebSocket(WS_URL);

  ws.addEventListener('open', function () {
    console.log('[CheckIn] ✅ WebSocket connecté à Streamer.bot');
    ws.send(JSON.stringify({
      request: 'Subscribe',
      id:      'checkin-overlay',
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

      if (ev === 'checkin_normal' || ev === 'checkin_cardcomplete') {
        console.log('[CheckIn] 📨 Événement reçu :', msg.event, '— viewer :', msg.username);
        if (typeof window.showCheckIn === 'function') {
          window.showCheckIn(msg);
        }
      }
    } catch (err) {
      console.error('[CheckIn] ❌ Erreur parsing :', err);
    }
  });

  ws.addEventListener('close', function () {
    console.warn('[CheckIn] ⚠️ WebSocket fermé — reconnexion dans 2s');
    setTimeout(connectCheckInWS, 2000);
  });

  ws.addEventListener('error', function () {
    console.error('[CheckIn] ❌ WebSocket erreur — Streamer.bot est-il lancé ?');
  });
}

connectCheckInWS();
