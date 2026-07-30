(() => {
  document.querySelectorAll('[data-open-dialog]').forEach(button => button.addEventListener('click', () => document.getElementById(button.dataset.openDialog)?.showModal()));
  document.querySelectorAll('[data-close-dialog]').forEach(button => button.addEventListener('click', () => button.closest('dialog')?.close()));
  const validation = document.querySelector('[data-import-validation]'); if (!validation) return;
  const executionDialog = validation.querySelector('[data-execution-dialog]'); validation.querySelector('[data-open-execution]')?.addEventListener('click', () => executionDialog.showModal());
  validation.querySelector('[data-execution-form]')?.addEventListener('submit', event => { const button = event.currentTarget.querySelector('button[type="submit"]'); button.disabled = true; validation.querySelector('[data-loading]').hidden = false; });
})();
