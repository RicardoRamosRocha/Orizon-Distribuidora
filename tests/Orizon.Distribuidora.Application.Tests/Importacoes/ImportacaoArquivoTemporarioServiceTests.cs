using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Orizon.Distribuidora.Web.Options;
using Orizon.Distribuidora.Web.Services;

namespace Orizon.Distribuidora.Application.Tests.Importacoes;

public sealed class ImportacaoArquivoTemporarioServiceTests
{
    [Fact]
    public async Task Token_fica_vinculado_ao_historico_empresa_e_usuario()
    {
        var service = CreateService(); var import = Guid.NewGuid(); var company = Guid.NewGuid(); var user = Guid.NewGuid();
        using var stream = new MemoryStream([0x50,0x4B,0x03,0x04]); var file = new FormFile(stream,0,stream.Length,"arquivo","produtos.xlsx");
        var saved = await service.SalvarAsync(file, import, company, user, default);
        try
        {
            Assert.Null(await service.ObterAsync(saved.Token, Guid.NewGuid(), company, user, default));
            Assert.Null(await service.ObterAsync(saved.Token, import, Guid.NewGuid(), user, default));
            Assert.Null(await service.ObterAsync(saved.Token, import, company, Guid.NewGuid(), default));
            Assert.NotNull(await service.ObterAsync(saved.Token, import, company, user, default));
        }
        finally { service.Excluir(saved.Token); }
    }

    [Fact]
    public async Task Token_expirado_e_rejeitado_e_limpo()
    {
        var service = CreateService(); var token = Guid.NewGuid().ToString("N"); var import = Guid.NewGuid(); var company = Guid.NewGuid(); var user = Guid.NewGuid();
        var directory = Path.Combine(Path.GetTempPath(),"orizon-importacoes"); Directory.CreateDirectory(directory); var dataPath = Path.Combine(directory,$"{token}.xlsx"); var metadataPath = Path.Combine(directory,$"{token}.json");
        await File.WriteAllBytesAsync(dataPath,[0x50,0x4B,0x03,0x04]); await File.WriteAllTextAsync(metadataPath,JsonSerializer.Serialize(new ImportacaoArquivoTemporario(token,import,company,user,dataPath,DateTimeOffset.UtcNow.AddMinutes(-1))));
        Assert.Null(await service.ObterAsync(token,import,company,user,default)); Assert.False(File.Exists(dataPath)); Assert.False(File.Exists(metadataPath));
    }

    [Fact]
    public async Task Mapeamento_fica_disponivel_para_corrigir_apos_a_validacao()
    {
        var service = CreateService();
        var import = Guid.NewGuid();
        var company = Guid.NewGuid();
        var user = Guid.NewGuid();
        using var stream = new MemoryStream([0x50, 0x4B, 0x03, 0x04]);
        var file = new FormFile(stream, 0, stream.Length, "arquivo", "produtos.xlsx");
        var saved = await service.SalvarAsync(file, import, company, user, default);
        var mappings = new Dictionary<string, string>
        {
            ["codigo"] = " Código ",
            ["descricao"] = "Descrição",
            ["unidade"] = "Unidade",
            ["precoVenda"] = "Preço de venda"
        };

        try
        {
            await service.SalvarMapeamentoAsync(saved.Token, import, company, user, mappings, "Produtos", default);
            var recovered = await service.ObterAsync(saved.Token, import, company, user, default);

            Assert.NotNull(recovered);
            Assert.Equal(" Código ", recovered.Mapeamentos!["codigo"]);
            Assert.Equal("Produtos", recovered.AbaSelecionada);
            Assert.True(File.Exists(recovered.Caminho));
        }
        finally
        {
            service.Excluir(saved.Token);
        }
    }

    private static ImportacaoArquivoTemporarioService CreateService()=>new(Options.Create(new ImportacaoOptions{ExpiracaoArquivoTemporarioMinutos=30}));
}
