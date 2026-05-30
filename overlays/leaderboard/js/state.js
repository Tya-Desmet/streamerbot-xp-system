'use strict';

var State = (function () {
  var _status = 'idle';
  var _timer  = null;

  return {
    get status() { return _status; },

    set: function (s) {
      _status = s;
    },

    scheduleDismiss: function (delayMs, cb) {
      State.cancelDismiss();
      _timer = setTimeout(cb, delayMs);
    },

    cancelDismiss: function () {
      if (_timer !== null) { clearTimeout(_timer); _timer = null; }
    },

    reset: function () {
      State.cancelDismiss();
      _status = 'idle';
    }
  };
}());
