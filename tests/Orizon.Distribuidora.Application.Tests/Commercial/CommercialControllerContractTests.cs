using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using Orizon.Distribuidora.Web.Areas.Admin.Controllers;
using Orizon.Distribuidora.Web.Areas.Admin.Models.Commercial;

namespace Orizon.Distribuidora.Application.Tests.Commercial;

public sealed class CommercialControllerContractTests
{
    private static ActionContext TestActionContext() => new(
        new DefaultHttpContext(), new RouteData(), new ActionDescriptor(), new ModelStateDictionary());

    [Theory]
    [InlineData(typeof(QuotesController), nameof(QuotesController.Create))]
    [InlineData(typeof(QuotesController), nameof(QuotesController.Edit))]
    [InlineData(typeof(QuotesController), nameof(QuotesController.Send))]
    [InlineData(typeof(QuotesController), nameof(QuotesController.Approve))]
    [InlineData(typeof(QuotesController), nameof(QuotesController.Reject))]
    [InlineData(typeof(QuotesController), nameof(QuotesController.Cancel))]
    [InlineData(typeof(QuotesController), nameof(QuotesController.Convert))]
    [InlineData(typeof(SalesController), nameof(SalesController.Confirm))]
    [InlineData(typeof(SalesController), nameof(SalesController.Cancel))]
    public void Critical_posts_require_antiforgery(Type controller, string action) =>
        Assert.Contains(controller.GetMethods().Where(x => x.Name == action)
            .SelectMany(x => x.GetCustomAttributes(true)), x => x is ValidateAntiForgeryTokenAttribute);

    [Theory]
    [InlineData(typeof(QuotesController))]
    [InlineData(typeof(SalesController))]
    public void Commercial_controllers_require_authorization(Type controller) =>
        Assert.NotNull(controller.GetCustomAttributes(typeof(AuthorizeAttribute), true).SingleOrDefault());

    [Fact]
    public void Quote_form_does_not_expose_company_id() =>
        Assert.Null(typeof(QuoteFormViewModel).GetProperty("CompanyId"));

    [Theory]
    [InlineData(typeof(QuotesController), "Admin/Orcamentos")]
    [InlineData(typeof(SalesController), "Admin/Vendas")]
    public void Commercial_modules_have_independent_routes(Type controller, string expectedRoute)
    {
        var route = Assert.Single(controller.GetCustomAttributes(typeof(RouteAttribute), true)
            .Cast<RouteAttribute>());
        Assert.Equal(expectedRoute, route.Template);
    }

    [Fact]
    public void Sidebar_exposes_quotes_and_sales_as_independent_first_level_links()
    {
        var sidebar = ReadRepositoryFile("src/Orizon.Distribuidora.Web/Views/Shared/_Sidebar.cshtml");

        Assert.Contains("href=\"/Admin/Orcamentos\"", sidebar);
        Assert.Contains("href=\"/Admin/Vendas\"", sidebar);
        Assert.DoesNotContain("sidebar-sales", sidebar);
        Assert.DoesNotContain("data-sidebar-group=\"sales\"", sidebar);
    }

    [Theory]
    [InlineData("src/Orizon.Distribuidora.Web/Areas/Admin/Views/Quotes/Print.cshtml")]
    [InlineData("src/Orizon.Distribuidora.Web/Areas/Admin/Views/Sales/Receipt.cshtml")]
    public void Commercial_documents_identify_themselves_as_non_fiscal(string path)
    {
        var view = ReadRepositoryFile(path);
        Assert.Contains("DOCUMENTO NÃO FISCAL", view);
        Assert.Contains("CustomerName", view);
        Assert.Contains("document-items", view);
        Assert.Contains("financial-summary", view);
    }

    [Theory]
    [InlineData("src/Orizon.Distribuidora.Web/Areas/Admin/Views/Quotes/Print.cshtml")]
    [InlineData("src/Orizon.Distribuidora.Web/Areas/Admin/Views/Sales/Receipt.cshtml")]
    public void Commercial_documents_render_notes_only_when_present(string path)
    {
        var view = ReadRepositoryFile(path);
        Assert.Contains("!string.IsNullOrWhiteSpace", view);
        Assert.Contains("Observações", view);
    }

    [Theory]
    [InlineData("1", 1)]
    [InlineData("2", 2)]
    [InlineData("1,5", 1.5)]
    [InlineData("1.5", 1.5)]
    [InlineData("1.234,56", 1234.56)]
    public async Task Commercial_decimal_binder_accepts_pt_br_and_invariant_values(string raw, decimal expected)
    {
        var valueProvider = new QueryStringValueProvider(
            BindingSource.Query,
            new QueryCollection(new Dictionary<string, StringValues> { ["Quantity"] = raw }),
            System.Globalization.CultureInfo.InvariantCulture);
        var metadataProvider = new EmptyModelMetadataProvider();
        var context = DefaultModelBindingContext.CreateBindingContext(
            TestActionContext(), valueProvider, metadataProvider.GetMetadataForType(typeof(decimal)),
            bindingInfo: null, modelName: "Quantity");

        await new CommercialDecimalModelBinder().BindModelAsync(context);

        Assert.True(context.Result.IsModelSet);
        Assert.Equal(expected, context.Result.Model);
        Assert.Empty(context.ModelState["Quantity"]!.Errors);
    }

    [Fact]
    public async Task Commercial_decimal_binder_rejects_invalid_quantity()
    {
        var valueProvider = new QueryStringValueProvider(
            BindingSource.Query,
            new QueryCollection(new Dictionary<string, StringValues> { ["Quantity"] = "abc" }),
            System.Globalization.CultureInfo.InvariantCulture);
        var metadataProvider = new EmptyModelMetadataProvider();
        var context = DefaultModelBindingContext.CreateBindingContext(
            TestActionContext(), valueProvider, metadataProvider.GetMetadataForType(typeof(decimal)),
            bindingInfo: null, modelName: "Quantity");

        await new CommercialDecimalModelBinder().BindModelAsync(context);

        Assert.False(context.Result.IsModelSet);
        Assert.False(context.ModelState.IsValid);
    }

    private static string ReadRepositoryFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Orizon.Distribuidora.sln")))
            directory = directory.Parent;
        Assert.NotNull(directory);
        return File.ReadAllText(Path.Combine(directory.FullName, relativePath));
    }
}
