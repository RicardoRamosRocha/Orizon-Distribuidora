namespace Orizon.Distribuidora.Web.Areas.Admin.Models.BasicRegistrations;

public sealed record BasicRegistrationModule(
    string Key,
    string Title,
    string SingularTitle,
    string Icon,
    bool HasCode,
    bool HasDescription,
    bool HasCategory,
    bool HasWarehouse,
    bool HasUnitFields,
    bool HasSupplierFields,
    bool HasPartnerFields,
    bool HasWarehouseFields,
    bool HasLocationFields,
    bool HasStatusFields,
    bool HasTypeFilter,
    bool HasSystemFilter)
{
    public static readonly IReadOnlyList<BasicRegistrationModule> All =
    [
        new("categorias", "Categorias", "Categoria", "folder", true, true, false, false, false, false, false, false, false, false, false, false),
        new("subcategorias", "Subcategorias", "Subcategoria", "folders", true, true, true, false, false, false, false, false, false, false, false, false),
        new("marcas", "Marcas", "Marca", "tag", true, true, false, false, false, false, false, false, false, false, false, false),
        new("unidades-medida", "Unidades de medida", "Unidade de medida", "ruler", false, true, false, false, true, false, false, false, false, false, false, false),
        new("grupos-produtos", "Grupos de produtos", "Grupo de produtos", "layers", true, true, false, false, false, false, false, false, false, false, false, false),
        new("fornecedores", "Fornecedores", "Fornecedor", "truck", false, false, false, false, false, true, false, false, false, false, true, false),
        new("parceiros-comerciais", "Parceiros comerciais", "Parceiro comercial", "handshake", false, false, false, false, false, false, true, false, false, false, true, false),
        new("depositos", "Depósitos", "Depósito", "warehouse", true, true, false, false, false, false, false, true, false, false, false, false),
        new("localizacoes-internas", "Localizações internas", "Localização interna", "map-pin", true, true, false, true, false, false, false, false, true, false, false, false),
        new("status-cadastro", "Status de cadastro", "Status de cadastro", "badge-check", true, true, false, false, false, false, false, false, false, true, false, true)
    ];

    public static BasicRegistrationModule Resolve(string key)
    {
        return All.FirstOrDefault(module =>
                module.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("Cadastro não encontrado.");
    }
}
