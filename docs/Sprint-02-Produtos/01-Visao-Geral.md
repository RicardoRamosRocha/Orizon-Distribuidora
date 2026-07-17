# Sprint 02 - Produtos

Objetivo: criar o cadastro central de produtos da Orizon Distribuidora com suporte a produtos próprios, de terceiros, sob encomenda e serviços.

Entregas principais:

- entidade `Product`;
- enum `ProductType`;
- enum `CommissionType`;
- migration `AddProducts`;
- grade administrativa em `/Admin/Products`;
- cadastro e edição;
- filtros, busca, ordenação e paginação server-side;
- ativação e inativação individual e em massa;
- imagem principal;
- histórico funcional em `ProductChangeHistories`;
- seeder idempotente em Development.

Limitações atuais:

- saldo de estoque não é armazenado em `Product`;
- importação de planilhas fica para sprint futura;
- edição inline existe no endpoint seguro, mas a grade ainda não expõe controles inline completos;
- múltiplas tabelas de preço e movimentações de estoque ficam fora do escopo.

Comandos Codespace:

```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=orizon_distribuidora;Username=postgres;Password=postgres"
dotnet restore
dotnet build --no-restore -m:1
dotnet test --no-build -m:1
dotnet ef database update --project src/Orizon.Distribuidora.Infrastructure/Orizon.Distribuidora.Infrastructure.csproj --startup-project src/Orizon.Distribuidora.Web/Orizon.Distribuidora.Web.csproj --no-build
dotnet run --project src/Orizon.Distribuidora.Web/Orizon.Distribuidora.Web.csproj --no-build
```

Comando Windows/PowerShell:

```powershell
$env:ConnectionStrings__DefaultConnection="Host=localhost;Port=5433;Database=orizon_distribuidora;Username=postgres;Password=postgres"
dotnet build -m:1
dotnet test --no-build -m:1
```
