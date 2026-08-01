(() => {
  const root = document.querySelector('[data-import-mapping]');
  if (!root) return;

  const data = JSON.parse(root.querySelector('[data-import-data]').textContent);
  const rows = [...root.querySelectorAll('.map-row')];
  const validationForm = root.querySelector('[data-validation-form]');
  const mappingInputs = root.querySelector('[data-mapping-inputs]');
  const token = root.querySelector('input[name="__RequestVerificationToken"]').value;
  const notice = root.querySelector('[data-import-notice]');
  const loading = root.querySelector('[data-loading]');

  const selectedField = row => {
    const value = row.querySelector('select')?.value || '';
    return value === '__pending' ? '' : value;
  };

  const mapping = () => Object.fromEntries(
    rows.map(row => [selectedField(row), row.dataset.header]).filter(([field]) => field)
  );

  const syncMappingInputs = () => {
    mappingInputs.replaceChildren();
    for (const [field, header] of Object.entries(mapping())) {
      const input = document.createElement('input');
      input.type = 'hidden';
      input.name = `Mapeamentos[${field}]`;
      input.value = header;
      mappingInputs.append(input);
    }
  };

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

  const setText = (selector, value) => {
    const element = root.querySelector(selector);
    if (element) element.textContent = value;
  };

  const refresh = () => {
    const fieldUse = new Map();
    for (const row of rows) {
      const field = selectedField(row);
      if (field) fieldUse.set(field, (fieldUse.get(field) || 0) + 1);
    }

    let reviews = 0;
    let conflicts = 0;
    let ignored = 0;
    let recognized = 0;
    let learned = 0;
    const confidences = [];

    for (const row of rows) {
      const select = row.querySelector('select');
      const rawValue = select?.value || '';
      const field = selectedField(row);
      const duplicate = Boolean(field && fieldUse.get(field) > 1);
      const untouchedReview = row.dataset.needsReview === 'true' && row.dataset.userReviewed !== 'true';
      const pending = rawValue === '__pending';
      const automaticConflict = untouchedReview && Boolean(row.dataset.autoConflict);
      const needsReview = pending || untouchedReview || automaticConflict;
      const status = row.querySelector('[data-map-status]');
      const observation = row.querySelector('[data-map-observation]');
      const strategy = row.querySelector('[data-recognition-strategy]');
      const confidenceText = row.querySelector('[data-confidence-text]');
      const confidenceBar = row.querySelector('[data-confidence-bar]');
      const confirmButton = row.querySelector('[data-confirm-row]');

      let confidence = 0;
      let strategyName = '—';
      let isLearned = false;
      if (field) {
        recognized++;
        if (field === row.dataset.originalField) {
          confidence = Number(row.dataset.confidence || 0);
          strategyName = row.dataset.strategy || 'Similarity';
          isLearned = row.dataset.learned === 'true';
        } else {
          strategyName = 'Similarity';
        }
        confidences.push(confidence);
        if (isLearned) learned++;
      } else if (!pending) ignored++;

      row.classList.toggle('needs-review', needsReview || duplicate);
      row.classList.toggle('error', duplicate);
      if (duplicate) {
        conflicts++;
        status.textContent = 'Conflito';
        status.className = 'import-status conflict';
        observation.textContent = 'Este campo já está associado a outra coluna.';
      } else if (needsReview) {
        reviews++;
        status.textContent = 'Revisar';
        status.className = 'import-status pending';
        observation.textContent = field ? 'Confirme a sugestão do ODRE.' : 'Selecione um campo ou marque como não importar.';
      } else if (field) {
        status.textContent = row.dataset.userReviewed === 'true' ? 'Revisado' : 'Reconhecida';
        status.className = 'import-status mapped';
        observation.textContent = 'Pronta para validação.';
      } else {
        status.textContent = 'Não importar';
        status.className = 'import-status unavailable';
        observation.textContent = 'Coluna revisada e ignorada.';
      }

      strategy.textContent = strategyName;
      strategy.className = `recognition-strategy ${strategyName.toLowerCase()}`;
      confidenceText.textContent = field ? `${Math.round(confidence)}%` : '—';
      confidenceBar.style.width = `${Math.max(0, Math.min(100, confidence))}%`;
      if (confirmButton) confirmButton.hidden = !(needsReview && field && !duplicate);
    }

    const missingRequired = data.required.filter(item => !fieldUse.has(item.chave));
    const requiredMapped = data.required.length - missingRequired.length;
    const progress = data.required.length ? Math.round(requiredMapped / data.required.length * 100) : 100;
    const average = confidences.length ? Math.round(confidences.reduce((sum, value) => sum + value, 0) / confidences.length) : 0;

    setText('[data-recognition-total]', rows.length);
    setText('[data-recognized-count]', recognized);
    setText('[data-learned-count]', learned);
    setText('[data-review-count]', reviews + conflicts);
    setText('[data-overall-confidence]', `${average}%`);
    setText('[data-progress-text]', `${progress}%`);
    setText('[data-required-mapped]', requiredMapped);
    setText('[data-missing-count]', reviews);
    setText('[data-conflict-count]', conflicts);
    setText('[data-unused-count]', ignored);
    setText('[data-side-confidence]', `${average}%`);
    root.querySelector('[data-progress-bar]').style.width = `${progress}%`;
    root.querySelector('[data-required-message]').textContent = missingRequired.length
      ? `Ainda faltam: ${missingRequired.map(item => item.nome).join(', ')}.`
      : 'Todos os campos obrigatórios estão mapeados.';
    root.querySelector('[data-required-summary]').classList.toggle('complete', missingRequired.length === 0);
    root.querySelector('[data-continue]').disabled = missingRequired.length > 0 || reviews > 0 || conflicts > 0;
    syncMappingInputs();
    return { missingRequired, reviews, conflicts };
  };

  for (const row of rows) {
    row.querySelector('select')?.addEventListener('change', () => {
      row.dataset.userReviewed = 'true';
      row.dataset.needsReview = 'false';
      row.dataset.autoConflict = '';
      refresh();
    });
    row.querySelector('[data-confirm-row]')?.addEventListener('click', () => {
      row.dataset.userReviewed = 'true';
      row.dataset.needsReview = 'false';
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
    locationSelect.options[0].textContent = available ? 'Selecione o local' : 'Nenhum local ativo neste depósito';
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
    if (!name) return showError('Informe um nome para o modelo.');
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
        method: 'POST', headers: { RequestVerificationToken: token }
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
    if (state.missingRequired.length || state.reviews || state.conflicts) {
      event.preventDefault();
      showError('Conclua as revisões e mapeie Código, Descrição, Unidade e Preço de venda para continuar.');
      const firstProblem = rows.find(row => row.classList.contains('needs-review') || row.classList.contains('error'));
      firstProblem?.scrollIntoView({ behavior: 'smooth', block: 'center' });
      firstProblem?.querySelector('select')?.focus({ preventScroll: true });
      return;
    }
    syncMappingInputs();
    loading.hidden = false;
    root.querySelector('[data-continue]').disabled = true;
  });
})();
