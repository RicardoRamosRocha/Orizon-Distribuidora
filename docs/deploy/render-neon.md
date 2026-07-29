# Homologação no Render + Neon

Este documento descreve o primeiro deploy de homologação. Não armazene senhas ou connection strings no repositório.

## 1. Neon

1. Crie um projeto e um banco exclusivos de homologação.
2. Copie a connection string do endpoint com pooling.
3. Mantenha `sslmode=require` na conexão.
4. Aplique as migrations de forma controlada antes do primeiro acesso. Não configure migrations automáticas em cada inicialização do serviço.
5. Use dados demonstrativos; não copie dados reais do cliente.

## 2. Render

Crie um Web Service conectado a este repositório:

- Runtime: Docker
- Branch: `main`
- Dockerfile: `./Dockerfile`
- Health Check Path: `/health`
- Auto-Deploy: somente após o CI da `main` ser aprovado

Variáveis obrigatórias:

| Variável | Valor |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__DefaultConnection` | connection string secreta do Neon |
| `Seed__Administrator__Email` | e-mail exclusivo de homologação |
| `Seed__Administrator__FullName` | nome do administrador de homologação |
| `Seed__Administrator__Password` | senha longa e exclusiva, marcada como secreta |

O Render fornece `PORT` automaticamente. O container escuta em `0.0.0.0:$PORT`.

## 3. Cuidados com o seed

O `IdentitySeeder` atual sincroniza a senha do administrador configurado sempre que a aplicação inicia. Portanto:

- mantenha a variável de senha protegida;
- alterar a variável altera a senha na próxima inicialização;
- não remova as variáveis até definir uma estratégia definitiva de provisionamento;
- não reutilize senha pessoal ou de produção.

## 4. Primeira publicação

1. Confirme que o CI da `main` está aprovado.
2. Crie o banco Neon de homologação.
3. Aplique todas as migrations existentes uma única vez, usando a mesma connection string.
4. Configure as variáveis secretas no Render.
5. Publique o Web Service.
6. Aguarde `/health` responder com HTTP 200.
7. Entre com o administrador de homologação.
8. Crie um usuário separado para o cliente.
9. Valide login, Produtos, Estoque, Preços, Orçamentos, Vendas, PDFs e logout.
10. Troque a senha inicial após entregar o acesso.

## 5. Limitações do ambiente gratuito

- O Render pode suspender o serviço após inatividade, tornando o primeiro acesso mais lento.
- O filesystem do container é efêmero. Arquivos permanentes não devem depender do disco local.
- O Neon pode reduzir o compute a zero após inatividade.
- Este ambiente é de homologação, não de produção.

## 6. Rollback

Em caso de falha:

1. suspenda o Auto-Deploy;
2. restaure o último deploy saudável no Render;
3. não reverta migrations destrutivamente sem backup e revisão;
4. preserve logs e registre o erro antes de uma nova tentativa.
