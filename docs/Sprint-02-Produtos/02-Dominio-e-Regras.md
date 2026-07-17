# Domínio e Regras

`Product` herda de `CompanyOwnedAuditableEntity`, reaproveitando `Id`, `CompanyId`, auditoria e soft delete.

Tipos:

- `Own`: pode controlar estoque, mas não é obrigado.
- `ThirdParty`: não controla estoque e exige parceiro comercial.
- `MadeToOrder`: pode ou não controlar estoque.
- `Service`: não controla estoque e não mantém depósito, localização ou estoque mínimo.

Regras implementadas:

- código interno obrigatório e único por empresa;
- SKU único por empresa quando informado;
- código de barras único por empresa quando informado;
- unidade obrigatória;
- custo, venda, estoque mínimo e comissão não podem ser negativos;
- comissão percentual não pode passar de 100%;
- NCM normalizado para números e validado com 8 dígitos;
- CEST normalizado para números e validado com 7 dígitos;
- subcategoria deve pertencer à categoria;
- localização deve pertencer ao depósito;
- entidades relacionadas são validadas por `CompanyId`;
- produtos sem controle de estoque limpam depósito, localização e estoque mínimo.

Margem:

```text
((Preço de venda - Preço de custo) / Preço de venda) * 100
```

Quando o preço de venda é zero, a margem retornada é zero.

Estoque:

`Product` não possui `CurrentStock`, `StockQuantity` ou `Balance`. A grade mostra textos honestos como "Aguardando estoque", "Sem controle", "Não controlado" e "Não aplicável".
