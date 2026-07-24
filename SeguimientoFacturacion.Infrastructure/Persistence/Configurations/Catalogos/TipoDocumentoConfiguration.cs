using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SeguimientoFacturacion.Domain.Entities.Catalogos;

namespace SeguimientoFacturacion.Infrastructure.Persistence.Configurations.Catalogos;

/// <summary>
/// Configura la persistencia de los tipos de documento.
/// </summary>
internal sealed class TipoDocumentoConfiguration :
    CatalogoConfigurationBase<TipoDocumento>
{
    protected override string NombreTabla => "TiposDocumento";

    /// <inheritdoc />
    public override void Configure(
        EntityTypeBuilder<TipoDocumento> builder)
    {
        base.Configure(builder);

        builder.Property(tipoDocumento => tipoDocumento.Sigla)
            .HasMaxLength(TipoDocumento.SiglaLongitudMaxima)
            .IsUnicode()
            .IsRequired();

        builder.HasIndex(tipoDocumento => tipoDocumento.Sigla)
            .IsUnique()
            .HasDatabaseName("UX_TiposDocumento_Sigla");
    }
}