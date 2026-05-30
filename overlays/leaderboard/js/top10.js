'use strict';

var TOP10_GAP = 90;

function _tName(p)  { return p ? (p.name || p.username || '—') : '—'; }
function _tLevel(p) {
  if (!p) return '—';
  if (p.level !== undefined) return 'Niv. ' + p.level;
  if (p.xp    !== undefined) return 'Niv. ' + (Math.floor(p.xp / 100) + 1);
  return '—';
}

function _buildRow(rank, player) {
  var empty = !player || (!player.name && !player.username);
  var row   = document.createElement('div');
  row.className = empty ? 'row row-empty' : 'row';
  row.innerHTML =
    '<span class="row-rank">#' + rank + '</span>' +
    '<span class="row-name">'  + _tName(empty ? null : player)  + '</span>' +
    '<span class="row-level">' + _tLevel(empty ? null : player) + '</span>';
  return row;
}

function buildRows(container, players) {
  container.innerHTML = '';
  var rows = [];
  for (var i = 3; i < 10; i++) {
    var row = _buildRow(i + 1, players[i] || null);
    container.appendChild(row);
    rows.push(row);
  }
  return rows;
}

function revealRows(rows, onDone) {
  var timers = rows.map(function (row, i) {
    return setTimeout(function () { revealEl(row); }, i * TOP10_GAP);
  });
  if (onDone) {
    timers.push(setTimeout(onDone, (rows.length - 1) * TOP10_GAP + 380));
  }
  return timers;
}

function resetRows(container) {
  container.innerHTML = '';
}
