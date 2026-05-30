'use strict';

function revealEl(el, cls) {
  if (!el) return;
  cls = cls || 'anim-in';
  el.classList.remove(cls);
  requestAnimationFrame(function () {
    requestAnimationFrame(function () {
      el.classList.add(cls);
    });
  });
}

function cancelAll(timers) {
  timers.forEach(clearTimeout);
}
