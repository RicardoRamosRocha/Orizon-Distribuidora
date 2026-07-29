using System.Text.RegularExpressions;

namespace Orizon.Distribuidora.Application.Tests.Products;

public sealed class ProductGridPresentationTests
{
    private readonly string view = ReadRepositoryFile(
        "src/Orizon.Distribuidora.Web/Areas/Admin/Views/Products/Index.cshtml");
    private readonly string styles = ReadRepositoryFile(
        "src/Orizon.Distribuidora.Web/wwwroot/css/products-premium-grid.css");
    private readonly string script = ReadRepositoryFile(
        "src/Orizon.Distribuidora.Web/wwwroot/js/products-premium-grid.js");

    [Fact]
    public void Selection_and_code_are_the_compact_non_resizable_fixed_block()
    {
        Assert.Contains("--products-select-width: 42px", styles);
        Assert.Contains("Key = \"code\", Label = \"Código\", Sort = \"code\", Width = 92", view);
        Assert.Contains("Sticky = true, Resizable = false", view);
        Assert.Contains("left: var(--products-select-width)", styles);
        Assert.Contains(".products-sticky-col[data-column=\"code\"]", styles);
        Assert.DoesNotContain("data-resizer=\"code\"", view);
        Assert.DoesNotContain("data-column=\"name\" class=\"products-name products-sticky-col\"", view);
    }

    [Fact]
    public void Description_is_the_first_scrollable_resizable_data_column()
    {
        var codeIndex = view.IndexOf("Key = \"code\"", StringComparison.Ordinal);
        var nameIndex = view.IndexOf("Key = \"name\"", StringComparison.Ordinal);

        Assert.True(codeIndex >= 0 && nameIndex > codeIndex);
        Assert.Contains(
            "Key = \"name\", Label = \"Produto\", Sort = \"name\", Width = 280, MinWidth = 220, MaxWidth = 480, Sticky = false, Resizable = true",
            view);
        Assert.Contains("title=\"@item.Name\"", view);
        Assert.DoesNotContain(".products-sticky-col[data-column=\"name\"]", styles);
        Assert.DoesNotContain("data-column=\"name\" class=\"products-name products-sticky-col\"", script);
    }

    [Fact]
    public void Resize_is_bounded_and_excludes_functional_columns()
    {
        Assert.Contains("columnLimits(column)", script);
        Assert.Contains("Math.min(limits.max, Math.max(limits.min", script);
        Assert.Contains("pointercancel", script);
        Assert.Contains("requestAnimationFrame", script);
        Assert.Contains("event.stopPropagation()", script);

        foreach (var key in new[] { "code", "unit", "actions" })
        {
            var definition = Regex.Match(view, $@"Key = ""{key}""[^\r\n]+");
            Assert.True(definition.Success, $"Definição da coluna {key} não encontrada.");
            Assert.Contains("Resizable = false", definition.Value);
        }
    }

    [Fact]
    public void Persisted_layout_is_versioned_validated_and_never_globally_cleared()
    {
        Assert.Contains("const schemaVersion = 5", script);
        Assert.Contains("normalizeState(JSON.parse", script);
        Assert.Contains("hideableColumns.has(key)", script);
        Assert.Contains("Number.isFinite(parsed)", script);
        Assert.Contains("pinned: [\"code\"]", script);
        Assert.DoesNotContain("localStorage.clear", script);
        Assert.DoesNotContain("removeItem(", script);
    }

    [Fact]
    public void Sorting_selection_inline_edit_and_internal_scrolling_are_preserved()
    {
        Assert.Contains("Sort = \"code\"", view);
        Assert.Contains("aria-sort=", view);
        Assert.Contains("data-select-all", view);
        Assert.Contains("data-select-filtered", view);
        Assert.Contains("data-bulk-operation", view);
        Assert.Contains("data-edit=\"price\"", view);
        Assert.Contains("data-edit=\"cost\"", view);
        Assert.Contains("beginEdit(cell)", script);
        Assert.Contains("event.shiftKey && lastSelected", script);
        Assert.Contains("overflow: auto", styles);
        Assert.Contains("max-width: 100%", styles);
    }

    [Fact]
    public void Sticky_cells_share_row_states_and_theme_tokens()
    {
        Assert.Contains("tbody tr:hover :is(.products-sticky-select, .products-sticky-col)", styles);
        Assert.Contains("tbody tr.is-selected :is(.products-sticky-select, .products-sticky-col)", styles);
        Assert.Contains("var(--orizon-surface-1-background)", styles);
        Assert.Contains("var(--orizon-surface-1-selected)", styles);
        Assert.Contains("@media (prefers-reduced-motion: reduce)", styles);
    }

    private static string ReadRepositoryFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null
            && !File.Exists(Path.Combine(directory.FullName, "Orizon.Distribuidora.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return File.ReadAllText(Path.Combine(directory.FullName, relativePath));
    }
}
