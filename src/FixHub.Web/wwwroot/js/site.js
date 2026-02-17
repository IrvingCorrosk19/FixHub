// FixHub — UX: loading state en botones submit (FASE E)
(function () {
  document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('form').forEach(function (form) {
      form.addEventListener('submit', function () {
        var btn = form.querySelector('button[type="submit"]');
        if (btn && !btn.classList.contains('btn-loading')) {
          btn.classList.add('btn-loading');
          btn.disabled = true;
        }
      });
    });
  });
})();
