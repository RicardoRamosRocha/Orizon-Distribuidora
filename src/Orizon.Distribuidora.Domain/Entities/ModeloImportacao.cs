using Orizon.Distribuidora.Domain.Common;
using Orizon.Distribuidora.Domain.Enums;

namespace Orizon.Distribuidora.Domain.Entities;

public sealed class ModeloImportacao : CompanyOwnedAuditableEntity
{
    private ModeloImportacao()
    {
    }

    public ModeloImportacao(
        Guid companyId,
        string nome,
        TipoArquivoImportacao tipoArquivo,
        string mapeamentoColunasJson)
        : base(companyId)
    {
        SetNome(nome);
        TipoArquivo = tipoArquivo;
        SetMapeamentoColunasJson(mapeamentoColunasJson);
        Ativo = true;
    }

    public string Nome { get; private set; } = string.Empty;

    public TipoArquivoImportacao TipoArquivo { get; private set; }

    public string MapeamentoColunasJson { get; private set; } = "{}";

    public bool Ativo { get; private set; } = true;

    public void Atualizar(string nome, string mapeamentoColunasJson, bool ativo)
    {
        SetNome(nome);
        SetMapeamentoColunasJson(mapeamentoColunasJson);
        Ativo = ativo;
    }

    private void SetNome(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new ArgumentException("O nome do modelo de importação é obrigatório.", nameof(nome));
        }

        Nome = nome.Trim();
    }

    private void SetMapeamentoColunasJson(string mapeamentoColunasJson)
    {
        MapeamentoColunasJson = string.IsNullOrWhiteSpace(mapeamentoColunasJson)
            ? "{}"
            : mapeamentoColunasJson.Trim();
    }
}
