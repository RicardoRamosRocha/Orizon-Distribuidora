# Validação

Migration criada:

```text
20260717141214_AddProducts
```

Tabelas:

- `Products`;
- `ProductChangeHistories`.

Validação obrigatória:

```bash
dotnet restore
dotnet build --no-restore -m:1
dotnet test --no-build -m:1
git diff --check
```

Validações manuais realizadas nesta implementação:

- PostgreSQL Docker saudável;
- migrations anteriores aplicadas;
- migration `AddProducts` aplicada;
- aplicação sobe em Development;
- seeder de produtos executa;
- `/Account/Login` responde 200;
- `/Admin/Products` redireciona para login quando anônimo;
- login seedado acessa `/Admin/Products` com HTTP 200;
- consulta da grade gerou SQL com `LIMIT/OFFSET` e projeção.

Validação visual:

Não foi feita validação visual completa em navegador nos viewports 1440, 1024, 768 e 375 nesta execução. A validação realizada foi por build, testes, HTTP e inspeção de HTML/SQL.

Próximos passos:

- expor controles inline diretamente nas células da grade;
- validar visualmente com navegador real;
- ampliar testes de controller com banco em memória ou container;
- medir consulta com volume maior controlado;
- integrar módulo futuro de estoque.
