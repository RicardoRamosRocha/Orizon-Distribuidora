# Grade de Produtos

Rota principal:

```text
/Admin/Products
```

A consulta usa:

- `AsNoTracking`;
- filtros no banco;
- ordenação no banco por whitelist;
- paginação com `Skip` e `Take`;
- projeção direta para `ProductRowViewModel`;
- joins somente para nomes de categoria, marca e unidade.

Filtros:

- busca;
- tipo;
- ativo/inativo;
- categoria;
- marca;
- fornecedor;
- parceiro;
- controle de estoque;
- validade de preço.

Busca:

Pesquisa server-side em código interno, SKU, código de barras, referência, descrição principal, descrição resumida e descrição detalhada.

Paginação:

- padrão: 50 itens;
- opções: 25, 50 e 100;
- máximo aceito: 100;
- seleção múltipla atua somente sobre a página atual.

Ações:

- editar;
- histórico;
- ativar/inativar por linha;
- ativação em massa;
- inativação em massa.

Imagem:

A aplicação salva apenas caminho relativo em `ImagePath`, aceita JPG, PNG e WEBP até 2 MB, gera nome físico com GUID e impede uso do nome original como caminho.
