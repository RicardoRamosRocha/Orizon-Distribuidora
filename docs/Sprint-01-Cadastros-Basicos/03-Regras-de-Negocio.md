# Regras de Negócio

Regras principais implementadas:

- Nome obrigatório nos cadastros principais.
- Sigla obrigatória e única por empresa em unidades de medida.
- Casas decimais de unidade entre 0 e 6.
- Unidade sem fração força zero casas decimais.
- Subcategoria exige categoria.
- Localização interna exige depósito.
- CPF/CNPJ são normalizados e validados quando informados.
- Comissão de parceiro entre 0 e 100.
- Depósito padrão é único por empresa.
- Status de sistema é protegido contra exclusão lógica.
- Duplicidades relevantes são bloqueadas por validação de aplicação e índices únicos filtrados.

