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
        string mapeamentoColunasJson,
        Guid? usuarioId = null,
        string assinaturaColunas = "",
        bool padrao = false)
        : base(companyId)
    {
        SetNome(nome);
        TipoArquivo = tipoArquivo;
        SetMapeamentoColunasJson(mapeamentoColunasJson);
        UsuarioId = usuarioId;
        AssinaturaColunas = assinaturaColunas;
        Padrao = padrao;
        Ativo = true;
    }

    public string Nome { get; private set; } = string.Empty;

    public TipoArquivoImportacao TipoArquivo { get; private set; }

    public string MapeamentoColunasJson { get; private set; } = "{}";

    public bool Ativo { get; private set; } = true;
    public Guid? UsuarioId { get; private set; }
    public string AssinaturaColunas { get; private set; } = string.Empty;
    public bool Padrao { get; private set; }

    public void Atualizar(string nome, string mapeamentoColunasJson, bool ativo)
    {
        SetNome(nome);
        SetMapeamentoColunasJson(mapeamentoColunasJson);
        Ativo = ativo;
    }

    public void DefinirComoPadrao(bool padrao) => Padrao = padrao;

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
