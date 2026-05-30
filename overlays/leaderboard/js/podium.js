'use strict';

var T_PODIUM3 =  200;
var T_PODIUM2 =  580;
var T_PODIUM1 = 1100;
var T_CROWN   = 1700;

function _pName(p)  { return p ? (p.name || p.username || '—') : '—'; }
function _pLevel(p) {
  if (!p) return '—';
  if (p.level !== undefined) return 'Niv. ' + p.level;
  if (p.xp    !== undefined) return 'Niv. ' + (Math.floor(p.xp / 100) + 1);
  return '—';
}
function _initial(name) { return (name && name !== '—') ? name[0].toUpperCase() : '?'; }

function _buildSlot(player, cls, rank) {
  var name = _pName(player);
  var el   = document.createElement('div');
  el.className = 'slot ' + cls;
  el.innerHTML =
    (cls === 'slot-1' ? '<span class="crown">👑</span>' : '') +
    '<div class="av-wrap">' +
      '<div class="av">' + _initial(name) + '</div>' +
      '<div class="rank-badge">' + rank + '</div>' +
    '</div>' +
    '<div class="pname">' + name + '</div>' +
    '<div class="plevel">' + _pLevel(player) + '</div>' +
    '<div class="pillar"></div>';
  return el;
}

function renderPodium(container, players, onDone) {
  container.innerHTML = '';

  var s1 = _buildSlot(players[0] || null, 'slot-1', 1);
  var s2 = _buildSlot(players[1] || null, 'slot-2', 2);
  var s3 = _buildSlot(players[2] || null, 'slot-3', 3);
  container.appendChild(s1);
  container.appendChild(s2);
  container.appendChild(s3);

  var crown  = s1.querySelector('.crown');
  var timers = [];

  timers.push(setTimeout(function () { revealEl(s3); },                   T_PODIUM3));
  timers.push(setTimeout(function () { revealEl(s2); },                   T_PODIUM2));
  timers.push(setTimeout(function () { revealEl(s1, 'anim-in-first'); },  T_PODIUM1));
  timers.push(setTimeout(function () {
    if (crown) crown.classList.add('anim-crown');
    if (onDone) onDone();
  }, T_CROWN));

  return timers;
}

function resetPodium(container) {
  container.innerHTML = '';
}
