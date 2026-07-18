# Sprint 4.2.1 — Organização da Administração

## Objetivo

Reorganizar a experiência administrativa antes da Sprint 5, preservando regras de negócio, banco de dados, serviços, APIs e a identidade visual das Sprints 4.1 e 4.2.

## Entrega

- Navegação principal organizada em Dashboard, Produtos, Estoque, Compras, Vendas, Financeiro, Cadastros, Configurações e Ajuda.
- Cadastros reunidos em um grupo expansível com Categorias, Subcategorias, Marcas, Grupos de Produtos, Unidades, Fornecedores, Parceiros, Depósitos, Localizações e Status.
- Configurações reunidas em um grupo expansível e em uma página inicial com cards para Empresa, Usuários, Perfis, Aparência, Preferências e Auditoria.
- Aparência preservada em `/Admin/Appearance`.
- Rotas administrativas preparadas em `/Admin/Company`, `/Admin/Users`, `/Admin/Roles`, `/Admin/Audit` e `/Admin/Preferences`, sem implementar regras dos módulos futuros.
- Breadcrumb automático com níveis de Dashboard, grupo e página atual.
- Header com empresa, usuário autenticado, avatar, pesquisa global preparada, notificações, alternância de tema e menu do usuário.
- Sidebar com estado ativo, recolhimento, tooltips, rolagem própria e transições suaves.
- Drawer responsivo com overlay e fechamento após navegação no mobile.

## Persistência local

As preferências de navegação usam somente `localStorage`, sem persistência no servidor:

| Chave | Conteúdo |
| --- | --- |
| `orizon.admin.sidebar.collapsed` | Estado recolhido/expandido da sidebar |
| `orizon.admin.sidebar.groups` | Grupos atualmente expandidos |
| `orizon.admin.lastPage` | Última página administrativa visitada |

O link da marca usa a última página administrativa válida como destino. Falhas ou bloqueios de armazenamento são tratados sem impedir a navegação.

## Acessibilidade e teclado

- Regiões de navegação possuem rótulos ARIA.
- Botões expansíveis atualizam `aria-expanded` e apontam para seus submenus com `aria-controls`.
- Links ativos usam indicação visual; breadcrumb usa `aria-current="page"`.
- Itens futuros são identificados por `aria-disabled` e permanecem acessíveis por foco para leitura do tooltip.
- `Escape` fecha drawer e menu do usuário.
- O atalho `/` está reservado para a pesquisa global quando ela for ativada.
- Animações são desabilitadas quando `prefers-reduced-motion` está ativo.

## Responsividade

- Desktop (a partir de 1024 px): sidebar fixa, expansível e recolhível.
- Tablet e mobile: sidebar em drawer com overlay.
- Abaixo de 900 px: pesquisa global é ocultada para preservar espaço.
- Abaixo de 480 px: ações secundárias do header são simplificadas.

## Limites respeitados

Não houve alteração em entidades, `DbContext`, migrations, serviços, endpoints de API ou regras de negócio. As novas actions MVC servem exclusivamente views administrativas de organização e placeholders explícitos para módulos futuros.

## Validação

Executar na raiz do repositório:

```powershell
dotnet build Orizon.Distribuidora.sln --no-restore
dotnet test Orizon.Distribuidora.sln --no-build
```

Critérios: build e testes aprovados, sem warnings ou erros.
