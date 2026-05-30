'use strict';

// ============================================================
// animations.js — Primitives réutilisables
// Toutes les animations utilisent uniquement transform + opacity (GPU)
// ============================================================

// Rend un élément invisible sans animation
function hideEl(el) {
  if (el) el.classList.add('anim-hidden');
}

// Révèle un élément via une classe d'animation CSS
// animClass : nom de la classe CSS qui porte le @keyframes
// callback  : déclenché à la fin de l'animation (optionnel)
// Double requestAnimationFrame : attend que le navigateur ait peint au moins
// une frame sans la classe d'animation avant de la déclencher.
// Plus propre que void el.offsetWidth (pas de reflow forcé synchrone).
function revealEl(el, animClass, callback) {
  if (!el) return;
  el.classList.remove('anim-hidden', animClass);

  requestAnimationFrame(function () {
    requestAnimationFrame(function () {
      el.classList.add(animClass);
      if (callback) {
        el.addEventListener('animationend', function onEnd() {
          el.removeEventListener('animationend', onEnd);
          callback();
        });
      }
    });
  });
}

// Applique une animation de sortie, puis cache et nettoie
function dismissEl(el, animClass, callback) {
  if (!el) return;
  el.classList.remove(animClass);
  void el.offsetWidth;
  el.classList.add(animClass);

  el.addEventListener('animationend', function onEnd() {
    el.removeEventListener('animationend', onEnd);
    el.classList.remove(animClass);
    el.classList.add('anim-hidden');
    if (callback) callback();
  });
}

// Exécute des actions à des instants précis
// steps : Array<{ delay: Number, fn: Function }>
function sequence(steps) {
  steps.forEach(function (step) {
    setTimeout(step.fn, step.delay);
  });
}
