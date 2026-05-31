'use strict';

function resetBar() {
  var fill = document.getElementById('xp-fill');
  if (!fill) return;
  fill.style.transition = 'none';
  fill.style.transform  = 'scaleX(0)';
}

function animateIn() {
  var card = document.getElementById('card');
  if (!card) return;

  card.classList.remove('hidden', 'leaving', 'entering');
  void card.offsetWidth;
  card.classList.add('entering');

  card.addEventListener('animationend', function onIn() {
    card.classList.remove('entering');
    card.removeEventListener('animationend', onIn);
  });
}

function animateOut() {
  var card = document.getElementById('card');
  if (!card) return;

  card.classList.remove('entering');
  card.classList.add('leaving');

  card.addEventListener('animationend', function onOut() {
    card.classList.remove('leaving');
    card.classList.add('hidden');
    resetBar();
    card.removeEventListener('animationend', onOut);
  });
}
