# Permissoes e seguranca

Os cadastros ficam sob a area administrativa e o controller compartilhado exige usuario autenticado no role `Administrator`.

O fluxo de autenticacao administrativo usa ASP.NET Core Identity com controller MVC local:

- `GET /Account/Login`
- `POST /Account/Login`
- `POST /Account/Logout`
- `GET /Account/AccessDenied`

O logout e feito somente por POST com antiforgery. A aplicacao nao possui cadastro publico, recuperacao de senha ou confirmacao de e-mail nesta fase.

O `CompanyId` nao e aceito em formularios, query string ou rotas. A empresa atual e resolvida no servidor por `ApplicationUser.CompanyId`.

Um usuario autenticado sem empresa associada e bloqueado pelo `CurrentCompanyAccessor`. O fallback para a primeira empresa existe somente para cenarios nao autenticados em ambiente Development, para facilitar validacoes locais explicitas.

Todas as consultas de listagem, criacao, edicao, ativacao, desativacao e exclusao logica filtram por empresa.

Relacionamentos sensiveis tambem sao validados por empresa:

- subcategoria deve usar categoria da empresa atual;
- localizacao interna deve usar deposito da empresa atual;
- duplicidades sao verificadas por empresa;
- indices unicos filtrados ignoram registros com soft delete.

As constantes de permissao granular foram declaradas para os cadastros basicos, mas a Sprint 0 ainda nao possui infraestrutura completa de policies/claims por permissao. Nesta sprint, o controle efetivo ficou em autenticacao + role administrativa, sem criar um sistema paralelo incompleto.

O menu `Cadastros` foi integrado por partial local no projeto Web, sem alterar a biblioteca compartilhada Orizon UI.

O administrador de desenvolvimento e criado pelo seed a partir das chaves de configuracao:

- `Seed:Administrator:Email`
- `Seed:Administrator:FullName`
- `Seed:Administrator:Password`

Nao registre senha real em documentacao versionada. Use `appsettings.Development.json`, variaveis de ambiente ou User Secrets conforme o ambiente.
