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
- [x] Login autenticado validado.
- [x] Logout por POST validado.
- [x] Dashboard acessivel com administrador.
- [x] Cadastros acessiveis com administrador.
- [x] CRUDs principais validados com usuario logado.

Pendencia real:

Nao ha cadastro publico, recuperacao de senha ou confirmacao de e-mail nesta fase. Essas funcionalidades permanecem fora do escopo.

Comandos de operacao:

```bash
docker compose up -d
dotnet restore
dotnet build --no-restore
dotnet test --no-build
dotnet ef database update --project src/Orizon.Distribuidora.Infrastructure/Orizon.Distribuidora.Infrastructure.csproj --startup-project src/Orizon.Distribuidora.Web/Orizon.Distribuidora.Web.csproj
dotnet run --project src/Orizon.Distribuidora.Web/Orizon.Distribuidora.Web.csproj
```
