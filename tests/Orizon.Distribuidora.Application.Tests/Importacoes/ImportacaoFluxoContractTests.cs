using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orizon.Distribuidora.Application.Importacoes;
using Orizon.Distribuidora.Domain.Entities;
using Orizon.Distribuidora.Domain.Enums;
using Orizon.Distribuidora.Infrastructure.Services;
using Orizon.Distribuidora.Web.Areas.Admin.Controllers;

namespace Orizon.Distribuidora.Application.Tests.Importacoes;

public sealed class ImportacaoFluxoContractTests
{
    [Fact]
    public void Controller_ShouldRequireAdministratorRole()
    {
        var authorize = typeof(ImportacaoController).GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(authorize);
        Assert.Contains("Administrator", authorize.Roles, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Upload", "Upload")]
    [InlineData("Executar", "Executar")]
    [InlineData("Cancelar", "Cancelar")]
    [InlineData("SalvarModelo", "SalvarModelo")]
    [InlineData("ExcluirModelo", "ExcluirModelo/{id:guid}")]
    [InlineData("ValidarMapeamento", "ValidarMapeamento")]
    [InlineData("Validacao", "Validacao")]
    [InlineData("Duplicar", "Duplicar/{id:guid}")]
    [InlineData("Excluir", "Excluir/{id:guid}")]
    [InlineData("ExecutarRollback", "ExecutarRollback/{id:guid}")]
    public void MutationEndpoints_ShouldBePostAndUseAntiforgery(string methodName, string route)
    {
        var method = typeof(ImportacaoController).GetMethods()
            .Single(candidate => candidate.Name == methodName &&
                candidate.GetCustomAttribute<HttpPostAttribute>()?.Template == route);

        Assert.NotNull(method.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
    }

    [Fact]
    public void Apply_ShouldPersistEverySupportedProductDomainField()
    {
        var companyId = Guid.NewGuid();
        var originalUnit = Guid.NewGuid();
        var targetUnit = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var subcategoryId = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var product = new Product(companyId, "P-1", "Produto anterior", originalUnit, ProductType.Own);
        var data = new Dictionary<string, JsonElement>
        {
            ["descricao"] = JsonSerializer.SerializeToElement("Produto importado"),
            ["descricaoComplementar"] = JsonSerializer.SerializeToElement("Detalhes"),
            ["codigoBarras"] = JsonSerializer.SerializeToElement("7891234567890"),
            ["precoCompra"] = JsonSerializer.SerializeToElement(12.34m),
            ["precoVenda"] = JsonSerializer.SerializeToElement(25.67m),
            ["unidadeId"] = JsonSerializer.SerializeToElement(targetUnit),
            ["categoriaId"] = JsonSerializer.SerializeToElement(categoryId),
            ["subcategoriaId"] = JsonSerializer.SerializeToElement(subcategoryId),
            ["marcaId"] = JsonSerializer.SerializeToElement(brandId),
            ["grupoId"] = JsonSerializer.SerializeToElement(groupId),
            ["fornecedorId"] = JsonSerializer.SerializeToElement(supplierId),
            ["ncm"] = JsonSerializer.SerializeToElement("12345678"),
            ["observacoes"] = JsonSerializer.SerializeToElement("Importado"),
            ["status"] = JsonSerializer.SerializeToElement(false),
            ["controlaEstoque"] = JsonSerializer.SerializeToElement(true),
            ["estoqueInicial"] = JsonSerializer.SerializeToElement(10m)
        };
        var options = new OpcoesValidacaoImportacao(DepositoId: warehouseId, LocalInternoId: locationId);
        var apply = typeof(ExecutorImportacaoProdutosService).GetMethod("Apply", BindingFlags.NonPublic | BindingFlags.Static)!;

        apply.Invoke(null, [product, data, options, false]);

        Assert.Equal("Produto importado", product.Name);
        Assert.Equal("Detalhes", product.Description);
        Assert.Equal("7891234567890", product.Barcode);
        Assert.Equal(12.34m, product.CostPrice);
        Assert.Equal(25.67m, product.SalePrice);
        Assert.Equal(targetUnit, product.UnitOfMeasureId);
        Assert.Equal(categoryId, product.CategoryId);
        Assert.Equal(subcategoryId, product.SubcategoryId);
        Assert.Equal(brandId, product.BrandId);
        Assert.Equal(groupId, product.ProductGroupId);
        Assert.Equal(supplierId, product.MainSupplierId);
        Assert.Equal("12345678", product.Ncm);
        Assert.Equal("Importado", product.Notes);
        Assert.False(product.IsActive);
        Assert.True(product.ControlsStock);
        Assert.Equal(warehouseId, product.DefaultWarehouseId);
        Assert.Equal(locationId, product.DefaultWarehouseLocationId);
    }

    [Fact]
    public void Mapping_view_should_post_named_dictionary_instead_of_javascript_only_json()
    {
        var repositoryRoot = FindRepositoryRoot();
        var view = File.ReadAllText(Path.Combine(repositoryRoot,
            "src", "Orizon.Distribuidora.Web", "Areas", "Admin", "Views", "Importacao", "Mapeamento.cshtml"));
        var script = File.ReadAllText(Path.Combine(repositoryRoot,
            "src", "Orizon.Distribuidora.Web", "wwwroot", "js", "importacao-mapeamento.js"));

        Assert.Contains("data-mapping-inputs", view);
        Assert.Contains("id=\"validationForm\"", view);
        Assert.Contains("input.name = `Mapeamentos[${field}]`", script);
        Assert.DoesNotContain("MapeamentoJson", view);
    }

    [Fact]
    public void Validation_view_should_block_execution_and_offer_correction_when_no_rows_are_valid()
    {
        var repositoryRoot = FindRepositoryRoot();
        var view = File.ReadAllText(Path.Combine(repositoryRoot,
            "src", "Orizon.Distribuidora.Web", "Areas", "Admin", "Views", "Importacao", "Validacao.cshtml"));

        Assert.Contains("Nenhuma linha pode ser importada", view);
        Assert.Contains("Corrigir mapeamento", view);
        Assert.Contains("Baixar relatório de erros", view);
        Assert.Contains("Model.Resultado.QuantidadeValida == 0", view);
        Assert.Contains("Coluna original", view);
        Assert.Contains("Como resolver", view);
        Assert.Contains("_ProblemasValidacao", view);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Orizon.Distribuidora.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Raiz do repositório não encontrada.");
    }
}
