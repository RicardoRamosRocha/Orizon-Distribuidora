using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orizon.Distribuidora.Domain.Entities;

namespace Orizon.Distribuidora.Infrastructure.Data.Configurations;

public sealed class ImportacaoHistoricoConfiguration : IEntityTypeConfiguration<ImportacaoHistorico>
{
    public void Configure(EntityTypeBuilder<ImportacaoHistorico> builder)
    {
        builder.ToTable("ImportacoesHistorico");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.CompanyId).IsRequired();
        builder.Property(entity => entity.ModeloImportacaoId);
        builder.Property(entity => entity.NomeArquivo).HasMaxLength(260).IsRequired();
        builder.Property(entity => entity.TipoArquivo).HasConversion<int>().IsRequired();
        builder.Property(entity => entity.TamanhoArquivoBytes).IsRequired();
        builder.Property(entity => entity.Status).HasConversion<int>().IsRequired();
        builder.Property(entity => entity.TotalLinhas).IsRequired();
        builder.Property(entity => entity.LinhasValidas).IsRequired();
        builder.Property(entity => entity.LinhasComErro).IsRequired();
        builder.Property(entity => entity.LinhasImportadas).IsRequired();
        builder.Property(entity => entity.Observacoes).HasMaxLength(2000);
        builder.Property(entity => entity.CreatedAt).IsRequired();
        builder.Property(entity => entity.IsDeleted).HasDefaultValue(false).IsRequired();

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(entity => entity.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(entity => entity.ModeloImportacao)
            .WithMany()
            .HasForeignKey(entity => entity.ModeloImportacaoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(entity => entity.Itens)
            .WithOne(entity => entity.ImportacaoHistorico)
            .HasForeignKey(entity => entity.ImportacaoHistoricoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(entity => entity.Erros)
            .WithOne(entity => entity.ImportacaoHistorico)
            .HasForeignKey(entity => entity.ImportacaoHistoricoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(entity => new { entity.CompanyId, entity.CreatedAt });
        builder.HasIndex(entity => new { entity.CompanyId, entity.Status });
        builder.HasIndex(entity => new { entity.CompanyId, entity.NomeArquivo });

        builder.HasQueryFilter(entity => !entity.IsDeleted);
    }
}

public sealed class ImportacaoItemConfiguration : IEntityTypeConfiguration<ImportacaoItem>
{
    public void Configure(EntityTypeBuilder<ImportacaoItem> builder)
    {
        builder.ToTable("ImportacaoItens");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.CompanyId).IsRequired();
        builder.Property(entity => entity.ImportacaoHistoricoId).IsRequired();
        builder.Property(entity => entity.NumeroLinha).IsRequired();
        builder.Property(entity => entity.Status).HasConversion<int>().IsRequired();
        builder.Property(entity => entity.DadosOriginaisJson).HasColumnType("jsonb").IsRequired();
        builder.Property(entity => entity.DadosNormalizadosJson).HasColumnType("jsonb");
        builder.Property(entity => entity.CreatedAt).IsRequired();
        builder.Property(entity => entity.IsDeleted).HasDefaultValue(false).IsRequired();

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(entity => entity.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(entity => entity.Produto)
            .WithMany()
            .HasForeignKey(entity => entity.ProdutoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(entity => entity.Erros)
            .WithOne(entity => entity.ImportacaoItem)
            .HasForeignKey(entity => entity.ImportacaoItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(entity => new { entity.CompanyId, entity.ImportacaoHistoricoId, entity.NumeroLinha })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");
        builder.HasIndex(entity => new { entity.CompanyId, entity.Status });

        builder.HasQueryFilter(entity => !entity.IsDeleted);
    }
}

public sealed class ImportacaoErroConfiguration : IEntityTypeConfiguration<ImportacaoErro>
{
    public void Configure(EntityTypeBuilder<ImportacaoErro> builder)
    {
        builder.ToTable("ImportacaoErros");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.CompanyId).IsRequired();
        builder.Property(entity => entity.ImportacaoHistoricoId).IsRequired();
        builder.Property(entity => entity.Coluna).HasMaxLength(120);
        builder.Property(entity => entity.ValorOriginal).HasMaxLength(1000);
        builder.Property(entity => entity.Mensagem).HasMaxLength(1000).IsRequired();
        builder.Property(entity => entity.CreatedAt).IsRequired();
        builder.Property(entity => entity.IsDeleted).HasDefaultValue(false).IsRequired();

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(entity => entity.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(entity => new { entity.CompanyId, entity.ImportacaoHistoricoId });
        builder.HasIndex(entity => new { entity.CompanyId, entity.NumeroLinha });

        builder.HasQueryFilter(entity => !entity.IsDeleted);
    }
}

public sealed class ModeloImportacaoConfiguration : IEntityTypeConfiguration<ModeloImportacao>
{
    public void Configure(EntityTypeBuilder<ModeloImportacao> builder)
    {
        builder.ToTable("ModelosImportacao");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.CompanyId).IsRequired();
        builder.Property(entity => entity.Nome).HasMaxLength(160).IsRequired();
        builder.Property(entity => entity.TipoArquivo).HasConversion<int>().IsRequired();
        builder.Property(entity => entity.MapeamentoColunasJson).HasColumnType("jsonb").IsRequired();
        builder.Property(entity => entity.Ativo).HasDefaultValue(true).IsRequired();
        builder.Property(entity => entity.UsuarioId);
        builder.Property(entity => entity.AssinaturaColunas).HasMaxLength(2000).HasDefaultValue("").IsRequired();
        builder.Property(entity => entity.Padrao).HasDefaultValue(false).IsRequired();
        builder.Property(entity => entity.CreatedAt).IsRequired();
        builder.Property(entity => entity.IsDeleted).HasDefaultValue(false).IsRequired();

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(entity => entity.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(entity => new { entity.CompanyId, entity.Nome })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");
        builder.HasIndex(entity => new { entity.CompanyId, entity.Ativo });

        builder.HasQueryFilter(entity => !entity.IsDeleted);
    }
}
