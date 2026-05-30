'use strict';

// ============================================================
// socket.js — WebSocket Streamer.bot → leaderboard
//
// Pour diagnostiquer : dans OBS, clic droit sur la source
// browser → Interact → F12 → onglet Console
//
// PORT : doit correspondre au port WebSocket de Streamer.bot
//        (Settings → WebSocket → Port)
// ============================================================

var WS_URL = 'ws://127.0.0.1:8080/';

// Extrait les players depuis un message Streamer.bot
// Gère tous les formats connus (direct, enveloppé, double-encodé)
function extractPlayers(raw) {
  var msg;

  try {
    msg = JSON.parse(raw);
  } catch (e) {
    console.error('[Leaderboard] ❌ Message non-JSON :', raw);
    return null;
  }

  // Log brut complet pour debug
  console.log('[Leaderboard] 📩 Message brut :', JSON.stringify(msg));

  // Format A : payload direct envoyé par CPH.WebsocketBroadcastJson
  // { "event": "updateLeaderboard", "players": [...] }
  if (msg.event && typeof msg.event === 'string') {
    console.log('[Leaderboard] Format A détecté (payload direct)');
    return msg.event.toLowerCase() === 'updateleaderboard' ? msg.players : null;
  }

  // Format B : enveloppe Streamer.bot standard
  // { event: { source: "General", type: "Custom" }, data: { data: "json-string" } }
  if (msg.event && typeof msg.event === 'object' && msg.data) {
    var inner = msg.data;

    // data.data est une chaîne JSON
    if (inner.data && typeof inner.data === 'string') {
      console.log('[Leaderboard] Format B détecté (enveloppe SB, data.data string)');
      try {
        var parsed = JSON.parse(inner.data);
        if ((parsed.event || '').toLowerCase() === 'updateleaderboard') {
          return parsed.players;
        }
      } catch (e) {
        console.error('[Leaderboard] ❌ Impossible de parser data.data :', inner.data);
      }
    }

    // data.data est déjà un objet
    if (inner.data && typeof inner.data === 'object' && inner.data.event) {
      console.log('[Leaderboard] Format C détecté (enveloppe SB, data.data objet)');
      if ((inner.data.event || '').toLowerCase() === 'updateleaderboard') {
        return inner.data.players;
      }
    }

    // data est directement l'objet payload
    if (inner.event && typeof inner.event === 'string') {
      console.log('[Leaderboard] Format D détecté (enveloppe SB, data = payload)');
      if (inner.event.toLowerCase() === 'updateleaderboard') {
        return inner.players;
      }
    }
  }

  // Message ignoré (subscribe confirm, heartbeat, autre event)
  console.log('[Leaderboard] ℹ️ Message ignoré (event non reconnu)');
  return null;
}

// Extrait le nom de thème depuis un message setTheme (mêmes formats que extractPlayers)
function extractTheme(raw) {
  var msg;
  try { msg = JSON.parse(raw); } catch (e) { return null; }

  // Format A : { event: "setTheme", theme: "cyber" }
  if (msg.event && typeof msg.event === 'string') {
    return msg.event.toLowerCase() === 'settheme' ? (msg.theme || null) : null;
  }

  // Format B/C/D : enveloppes Streamer.bot
  if (msg.event && typeof msg.event === 'object' && msg.data) {
    var inner = msg.data;

    if (inner.data && typeof inner.data === 'string') {
      try {
        var parsed = JSON.parse(inner.data);
        if ((parsed.event || '').toLowerCase() === 'settheme') return parsed.theme || null;
      } catch (e) {}
    }

    if (inner.data && typeof inner.data === 'object') {
      if ((inner.data.event || '').toLowerCase() === 'settheme') return inner.data.theme || null;
    }

    if (inner.event && typeof inner.event === 'string') {
      if (inner.event.toLowerCase() === 'settheme') return inner.theme || null;
    }
  }

  return null;
}

function connectLeaderboardWS() {
  console.log('[Leaderboard] 🔌 Connexion WebSocket →', WS_URL);
  var ws = new WebSocket(WS_URL);

  ws.addEventListener('open', function () {
    console.log('[Leaderboard] ✅ WebSocket connecté');
    ws.send(JSON.stringify({
      request: 'Subscribe',
      id:      'leaderboard-overlay',
      events:  { General: ['Custom'] }
    }));
    console.log('[Leaderboard] 📬 Abonnement General.Custom envoyé');
  });

  ws.addEventListener('message', function (e) {
    // Leaderboard update
    var players = extractPlayers(e.data);
    if (players) {
      if (!Array.isArray(players) || players.length === 0) {
        console.warn('[Leaderboard] ⚠️ players reçu mais vide ou invalide :', players);
        return;
      }
      console.log('[Leaderboard] 🎬 Déclenchement leaderboard —', players.length, 'joueurs');
      if (typeof window.updateLeaderboard === 'function') {
        window.updateLeaderboard(players);
      } else {
        console.error('[Leaderboard] ❌ window.updateLeaderboard non défini');
      }
      return;
    }

    // Changement de thème
    var theme = extractTheme(e.data);
    if (theme) {
      console.log('[Leaderboard] 🎨 setTheme reçu :', theme);
      if (typeof window.setTheme === 'function') {
        window.setTheme(theme);
      }
    }
  });

  ws.addEventListener('close', function (e) {
    console.warn('[Leaderboard] ⚠️ WebSocket fermé (code ' + e.code + ') — reconnexion dans 2s');
    setTimeout(connectLeaderboardWS, 2000);
  });

  ws.addEventListener('error', function () {
    console.error('[Leaderboard] ❌ Erreur WebSocket — Streamer.bot tourne-t-il sur le port 8080 ?');
  });
}

connectLeaderboardWS();
