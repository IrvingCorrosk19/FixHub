/* FixHub — UX global (FASE 4 + 5) */
(function () {
  'use strict';

  /* ── 1. Toast system ──────────────────────────────────────────────────────── */
  function showToast(message, type) {
    var container = document.getElementById('fixhubToastContainer');
    if (!container) return;

    var icon = type === 'success' ? '✅' : '❌';
    var toast = document.createElement('div');
    toast.className = 'fixhub-toast fixhub-toast-' + type;
    toast.innerHTML =
      '<span class="fixhub-toast-icon">' + icon + '</span>' +
      '<span class="fixhub-toast-msg">' + message + '</span>' +
      '<button class="fixhub-toast-close" aria-label="Cerrar">&times;</button>';

    container.appendChild(toast);

    // Forzar reflow antes de añadir clase visible (activa la transición CSS)
    void toast.offsetHeight;
    toast.classList.add('fixhub-toast-visible');

    // Auto-dismiss a los 4 segundos
    var timer = setTimeout(function () { dismissToast(toast); }, 4000);

    toast.querySelector('.fixhub-toast-close').addEventListener('click', function () {
      clearTimeout(timer);
      dismissToast(toast);
    });
  }

  function dismissToast(toast) {
    toast.classList.remove('fixhub-toast-visible');
    toast.addEventListener('transitionend', function () {
      if (toast.parentNode) toast.parentNode.removeChild(toast);
    }, { once: true });
  }

  /* ── 2. Loading states en formularios ────────────────────────────────────── */
  function initLoadingStates() {
    document.querySelectorAll('form').forEach(function (form) {
      form.addEventListener('submit', function () {
        var btn = form.querySelector('button[type="submit"]:not([data-bs-toggle])');
        if (btn && !btn.classList.contains('btn-loading')) {
          btn.classList.add('btn-loading');
          btn.disabled = true;
        }
      });
    });
  }

  /* ── 3. Smooth scroll para anclas internas ──────────────────────────────── */
  function initSmoothScroll() {
    document.querySelectorAll('a[href^="#"]').forEach(function (anchor) {
      anchor.addEventListener('click', function (e) {
        var target = document.querySelector(this.getAttribute('href'));
        if (target) {
          e.preventDefault();
          target.scrollIntoView({ behavior: 'smooth', block: 'start' });
        }
      });
    });
  }

  /* ── 4. Hover micro-interacción en job cards ─────────────────────────────── */
  function initCardHovers() {
    document.querySelectorAll('.fixhub-job-card, .fixhub-proposal-card-v2, .fixhub-kpi-card').forEach(function (card) {
      card.addEventListener('mouseenter', function () {
        this.style.willChange = 'transform, box-shadow';
      });
      card.addEventListener('mouseleave', function () {
        this.style.willChange = 'auto';
      });
    });
  }

  /* ── 5. Init ─────────────────────────────────────────────────────────────── */
  document.addEventListener('DOMContentLoaded', function () {

    // Leer TempData y disparar toasts
    var successEl = document.getElementById('toastDataSuccess');
    var errorEl   = document.getElementById('toastDataError');
    if (successEl) showToast(successEl.getAttribute('data-message'), 'success');
    if (errorEl)   showToast(errorEl.getAttribute('data-message'),   'error');

    initLoadingStates();
    initSmoothScroll();
    initCardHovers();
  });

})();
