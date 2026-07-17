# Validacao e testes

Validacoes implementadas:

- Data Annotations nas ViewModels.
- Validacoes de dominio nas entidades.
- Validacoes de relacionamento e duplicidade no controller.
- Indices unicos compostos por empresa no banco.
- Foreign keys para `Companies` com `Restrict`.
- Foreign keys de subcategoria/categoria e localizacao/deposito com `Restrict`.
- Query filters de soft delete.
- Login MVC com antiforgery.
- Redirecionamento seguro de `ReturnUrl` apenas para URLs locais.
- Cookie Identity com `LoginPath`, `LogoutPath`, `AccessDeniedPath`, `HttpOnly` e `SameSite=Lax`.

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
- ViewModel de login exige e-mail e senha.
- `ReturnUrl` externo e rejeitado.
- rotas de login/logout usam atributos esperados.
- controllers administrativos exigem role `Administrator`.

Validacao local executada:

```bash
dotnet restore
dotnet build --no-restore
dotnet test --no-build
dotnet ef database update --project src/Orizon.Distribuidora.Infrastructure/Orizon.Distribuidora.Infrastructure.csproj --startup-project src/Orizon.Distribuidora.Web/Orizon.Distribuidora.Web.csproj
dotnet run --project src/Orizon.Distribuidora.Web/Orizon.Distribuidora.Web.csproj --no-build --urls http://localhost:5191
```

Resultado local registrado nesta revisao:

- build verde, sem warnings;
- testes verdes, 32 testes aprovados;
- migration `AddBasicRegistrations` aplicada no PostgreSQL local;
- aplicacao iniciou em Development;
- seeds idempotentes executados;
- `GET /Account/Login` retornou 200;
- login invalido retornou mensagem generica;
- login valido do administrador inicial redirecionou para o `ReturnUrl`;
- Dashboard e cadastros responderam 200 autenticados;
- logout por POST encerrou a sessao;
- registros temporarios de validacao foram excluidos logicamente.

CRUDs validados autenticados:

- categorias: criar, pesquisar, editar, desativar, ativar e excluir logicamente;
- marcas: criar, validar duplicidade e excluir logicamente;
- unidades: criar unidade sem fracao e confirmar exibicao como inteira;
- depositos: criar dois padroes e confirmar apenas um padrao por empresa;
- localizacoes internas: criar vinculada ao deposito e filtrar por deposito;
- fornecedores: validar documento/e-mail invalidos, criar fornecedor simples e excluir logicamente;
- parceiros comerciais: validar comissao invalida, criar comissao valida e excluir logicamente.
