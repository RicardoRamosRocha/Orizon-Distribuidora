(() => {
  const form = document.querySelector('[data-import-upload-form]'); if (!form) return; const dropzone = form.querySelector('[data-import-dropzone]'); const input = form.querySelector('[data-import-file]'); const selected = form.querySelector('[data-import-selected]'); const submit = form.querySelector('[data-upload-submit]');
  const update = () => { const file = input.files?.[0]; if (!file) return; form.querySelector('[data-import-file-name]').textContent = file.name; form.querySelector('[data-import-file-size]').textContent = file.size >= 1048576 ? `${(file.size / 1048576).toFixed(2)} MB` : `${(file.size / 1024).toFixed(2)} KB`; selected.hidden = false; submit.disabled = false; };
  form.querySelector('[data-import-select]').addEventListener('click', event => { event.preventDefault(); input.click(); }); input.addEventListener('change', update);
  ['dragenter','dragover'].forEach(name => dropzone.addEventListener(name, event => { event.preventDefault(); dropzone.classList.add('is-dragover'); })); ['dragleave','drop'].forEach(name => dropzone.addEventListener(name, event => { event.preventDefault(); dropzone.classList.remove('is-dragover'); }));
  dropzone.addEventListener('drop', event => { const file = event.dataTransfer.files?.[0]; if (!file) return; const transfer = new DataTransfer(); transfer.items.add(file); input.files = transfer.files; update(); });
  form.addEventListener('submit', () => { submit.disabled = true; form.querySelector('[data-loading]').hidden = false; });
})();
