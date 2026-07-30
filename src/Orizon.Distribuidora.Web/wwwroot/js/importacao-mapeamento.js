(() => {
  const root = document.querySelector('[data-import-mapping]');
  if (!root) return;

  const data = JSON.parse(root.querySelector('[data-import-data]').textContent);
  const rows = [...root.querySelectorAll('.map-row')];
  const validationForm = root.querySelector('[data-validation-form]');
  const token = root.querySelector('input[name="__RequestVerificationToken"]').value;
  const notice = root.querySelector('[data-import-notice]');
  const loading = root.querySelector('[data-loading]');

  const mapping = () => Object.fromEntries(
    rows
      .filter(row => row.dataset.available === 'true')
      .map(row => [row.dataset.field, row.querySelector('select').value])
      .filter(([, value]) => value)
  );

  const payload = () => ({
    nome: '',
    padrao: false,
    cabecalhos: data.headers,
    amostra: data.sample,
    mapeamentos: mapping()
  });

  const showError = message => {
    notice.textContent = message;
    notice.hidden = false;
    notice.scrollIntoView({ behavior: 'smooth', block: 'center' });
  };

  const refresh = () => {
    const used = new Map();
    for (const row of rows) {
      const value = row.querySelector('select')?.value;
      if (value) used.set(value, (used.get(value) || 0) + 1);
    }

    let requiredMapped = 0;
    let optionalMapped = 0;
    let missing = 0;
    let conflicts = 0;

    for (const row of rows) {
      const select = row.querySelector('select');
      const value = select?.value || '';
      const status = row.querySelector('[data-map-status]');
      const observation = row.querySelector('[data-map-observation]');
      const preview = row.querySelector('[data-preview-value]');

      if (row.dataset.available !== 'true') {
        status.textContent = 'Indisponível';
        status.className = 'import-status unavailable';
        preview.textContent = '—';
        continue;
      }

      const duplicate = value && used.get(value) > 1;
      const automaticConflict = !value && Boolean(row.dataset.autoConflict);
      const requiredMissing = row.dataset.required === 'true' && !value && !automaticConflict;
      const conflict = Boolean(duplicate || automaticConflict);

      row.classList.toggle('error', conflict || requiredMissing);
      if (value) {
        if (row.dataset.required === 'true') requiredMapped++;
        else optionalMapped++;
      }
      if (requiredMissing) missing++;
      if (conflict) conflicts++;

      if (duplicate) {
        status.textContent = 'Conflito';
        status.className = 'import-status conflict';
        observation.textContent = 'Escolha outra coluna; esta já está associada a outro campo.';
      } else if (automaticConflict) {
        status.textContent = 'Conflito';
        status.className = 'import-status conflict';
        observation.textContent = `Correspondência ambígua entre: ${row.dataset.autoConflict.split('|').join(', ')}. Escolha manualmente.`;
      } else if (value) {
        status.textContent = 'Mapeado';
        status.className = 'import-status mapped';
        observation.textContent = 'Coluna confirmada e será enviada para validação.';
      } else {
        status.textContent = requiredMissing ? 'Pendente' : 'Não importar';
        status.className = `import-status ${requiredMissing ? 'pending' : 'unavailable'}`;
        observation.textContent = requiredMissing
          ? 'Selecione a coluna da planilha correspondente.'
          : 'Campo opcional não será importado.';
      }

      const sample = data.sample.find(item => String(item[value] ?? '').trim()) || data.sample[0] || {};
      preview.textContent = value ? (sample[value] ?? '—') : '—';
    }

    for (const chip of root.querySelectorAll('[data-column-chip]')) {
      const count = used.get(chip.dataset.columnChip) || 0;
      chip.classList.toggle('used', count === 1);
      chip.classList.toggle('conflict', count > 1);
    }

    const required = rows.filter(row => row.dataset.available === 'true' && row.dataset.required === 'true').length;
    const percent = required ? Math.round(requiredMapped / required * 100) : 100;
    root.querySelector('[data-progress-text]').textContent = `${percent}%`;
    root.querySelector('[data-progress-bar]').style.width = `${percent}%`;
    root.querySelector('[data-required-mapped]').textContent = requiredMapped;
    root.querySelector('[data-optional-mapped]').textContent = optionalMapped;
    root.querySelector('[data-unavailable-count]').textContent =
      rows.filter(row => row.dataset.available !== 'true').length;
    root.querySelector('[data-unused-count]').textContent = data.headers.length - used.size;
    root.querySelector('[data-missing-count]').textContent = missing;
    root.querySelector('[data-conflict-count]').textContent = conflicts;
    root.querySelector('[data-continue]').disabled = missing > 0 || conflicts > 0;

    return { missing, conflicts };
  };

  for (const row of rows) {
    row.querySelector('select')?.addEventListener('change', () => {
      row.dataset.autoConflict = '';
      refresh();
    });
  }
  refresh();

  const warehouse = root.querySelector('#warehouseSelect');
  const locationSelect = root.querySelector('#locationSelect');
  warehouse.addEventListener('change', () => {
    let available = 0;
    for (const [index, option] of [...locationSelect.options].entries()) {
      if (!index) continue;
      option.hidden = option.dataset.parent !== warehouse.value;
      if (!option.hidden) available++;
    }
    locationSelect.value = '';
    locationSelect.disabled = !warehouse.value || !available;
    locationSelect.options[0].textContent = available
      ? 'Selecione o local'
      : 'Nenhum local ativo neste depósito';
  });

  root.querySelector('[data-model-select]').addEventListener('change', event => {
    const url = new URL(root.dataset.mapBaseUrl, window.location.href);
    if (event.target.value) url.searchParams.set('modeloId', event.target.value);
    window.location.href = url;
  });

  const saveDialog = root.querySelector('[data-save-dialog]');
  root.querySelector('[data-save-model]').addEventListener('click', () => saveDialog.showModal());
  root.querySelector('[data-confirm-save]').addEventListener('click', async event => {
    event.preventDefault();
    const name = root.querySelector('[data-model-name]').value.trim();
    if (!name) {
      showError('Informe um nome para o modelo.');
      return;
    }
    const body = payload();
    body.nome = name;
    body.padrao = root.querySelector('[data-model-default]').checked;
    event.currentTarget.disabled = true;
    try {
      const response = await fetch(root.dataset.saveModelUrl, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', RequestVerificationToken: token },
        body: JSON.stringify(body)
      });
      if (!response.ok) throw new Error();
      location.reload();
    } catch {
      showError('Não foi possível salvar o modelo. Corrija o mapeamento e tente novamente.');
      event.currentTarget.disabled = false;
      saveDialog.close();
    }
  });

  const deleteDialog = root.querySelector('[data-delete-dialog]');
  root.querySelector('[data-delete-model]').addEventListener('click', () => deleteDialog.showModal());
  root.querySelector('[data-confirm-delete]').addEventListener('click', async event => {
    event.preventDefault();
    const id = root.querySelector('[data-model-select]').value;
    if (!id) return;
    event.currentTarget.disabled = true;
    try {
      const response = await fetch(`${root.dataset.deleteModelUrl}/${id}`, {
        method: 'POST',
        headers: { RequestVerificationToken: token }
      });
      if (!response.ok) throw new Error();
      window.location.href = new URL(root.dataset.mapBaseUrl, window.location.href);
    } catch {
      showError('Não foi possível excluir este modelo. Verifique se ele pertence ao seu usuário.');
      event.currentTarget.disabled = false;
      deleteDialog.close();
    }
  });

  validationForm.addEventListener('submit', event => {
    const state = refresh();
    if (state.missing > 0 || state.conflicts > 0) {
      event.preventDefault();
      showError('Mapeie Código, Descrição, Unidade e Preço de venda para continuar.');
      const firstProblem = rows.find(row => row.classList.contains('error'));
      firstProblem?.scrollIntoView({ behavior: 'smooth', block: 'center' });
      firstProblem?.querySelector('select')?.focus({ preventScroll: true });
      return;
    }
    loading.hidden = false;
    root.querySelector('[data-continue]').disabled = true;
  });
})();
