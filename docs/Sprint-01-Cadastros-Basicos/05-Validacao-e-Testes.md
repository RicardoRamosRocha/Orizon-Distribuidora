# Validacao e testes

Validacoes implementadas:

- Data Annotations nas ViewModels.
- Validacoes de dominio nas entidades.
- Validacoes de relacionamento e duplicidade no controller.
- Indices unicos compostos por empresa no banco.
- Foreign keys para `Companies` com `Restrict`.
- Foreign keys de subcategoria/categoria e localizacao/deposito com `Restrict`.
- Query filters de soft delete.

Testes automatizados executados:

- categoria valida;
- obrigatoriedade de categoria em subcategoria;
- casas decimais invalidas em unidade de medida;
- unidade sem fracao com zero casas decimais;
- percentual de comissao invalido;
- percentual de comissao valido;
- e-mail invalido de parceiro comercial;
- alteracao explicita de deposito padrao;
- obrigatoriedade de deposito em localizacao interna;
- documento invalido de fornecedor;
- CPF valido e invalido;
- CNPJ valido e invalido;
- protecao de status de sistema contra exclusao;
- cor invalida de status;
- ordem negativa de status.

Validacao local executada:

```bash
dotnet restore
dotnet build --no-restore
dotnet test --no-build
dotnet ef database update --project src/Orizon.Distribuidora.Infrastructure/Orizon.Distribuidora.Infrastructure.csproj --startup-project src/Orizon.Distribuidora.Web/Orizon.Distribuidora.Web.csproj
dotnet run --project src/Orizon.Distribuidora.Web/Orizon.Distribuidora.Web.csproj --no-build --urls http://localhost:5190
```

Resultado local registrado nesta revisao:

- build verde, sem warnings;
- testes verdes, 23 testes aprovados;
- migration `AddBasicRegistrations` aplicada no PostgreSQL local;
- aplicacao iniciou em Development;
- seeds idempotentes executados;
- rotas protegidas de cadastros redirecionaram para `/Account/Login`.

Limitacao real: a tela/controller de login nao existe neste branch. Portanto, a navegacao autenticada e os CRUDs pelo navegador nao puderam ser validados com usuario logado sem implementar Login, que esta fora do escopo desta sprint.
