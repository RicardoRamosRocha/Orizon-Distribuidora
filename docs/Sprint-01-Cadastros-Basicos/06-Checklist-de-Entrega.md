# Checklist de entrega

- [x] Entidades de dominio criadas.
- [x] Configuracoes EF Core criadas.
- [x] Migration `AddBasicRegistrations` criada e regenerada com FKs para empresa.
- [x] Migration aplicada no PostgreSQL local.
- [x] Dez tabelas de cadastros criadas no banco.
- [x] CRUD administrativo por modulo criado.
- [x] Busca, filtros e paginacao no servidor implementados.
- [x] Ativacao, desativacao e exclusao logica implementadas.
- [x] Menu Cadastros integrado localmente no Web.
- [x] Seeds idempotentes de unidades e status adicionados.
- [x] Seeds conferidos no banco: 8 unidades e 4 status de sistema.
- [x] Testes de dominio adicionados e executados.
- [x] Build executado sem warnings.
- [x] Aplicacao iniciou em Development.
- [x] Documentacao da sprint atualizada.
- [ ] Login autenticado validado manualmente.
- [ ] CRUDs validados pelo navegador com usuario logado.

Pendencia real:

A Sprint atual nao implementa `AccountController`/Login. As rotas protegidas redirecionam corretamente para `/Account/Login`, mas a URL retorna 404 porque o fluxo de login nao existe neste branch. A pendencia foi mantida documentada sem implementar Login, conforme restricao de escopo.

Comandos de operacao:

```bash
docker compose up -d
dotnet restore
dotnet build --no-restore
dotnet test --no-build
dotnet ef database update --project src/Orizon.Distribuidora.Infrastructure/Orizon.Distribuidora.Infrastructure.csproj --startup-project src/Orizon.Distribuidora.Web/Orizon.Distribuidora.Web.csproj
dotnet run --project src/Orizon.Distribuidora.Web/Orizon.Distribuidora.Web.csproj
```
