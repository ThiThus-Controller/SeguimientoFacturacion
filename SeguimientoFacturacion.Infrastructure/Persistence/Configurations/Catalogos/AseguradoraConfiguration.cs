using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SeguimientoFacturacion.Domain.Common;
using SeguimientoFacturacion.Domain.Entities.Catalogos;
using SeguimientoFacturacion.Infrastructure.Configuration;

namespace SeguimientoFacturacion.Infrastructure.Persistence.Configurations.Catalogos;

/// <summary>
/// Configura la persistencia de las aseguradoras.
/// </summary>
internal sealed class AseguradoraConfiguration :
    IEntityTypeConfiguration<Aseguradora>
{
    /// <inheritdoc />
    public void Configure(
        EntityTypeBuilder<Aseguradora> builder)
    {
        builder.ToTable(
            "Aseguradoras",
            EsquemasBaseDatos.Facturacion);

        builder.HasKey(aseguradora => aseguradora.Id);

        builder.Property(aseguradora => aseguradora.Id)
            .ValueGeneratedNever();

        builder.Property(aseguradora => aseguradora.Descripcion)
            .HasMaxLength(
                CatalogoAdministrableBase.DescripcionLongitudMaxima)
            .IsUnicode()
            .IsRequired();

        builder.Property(aseguradora => aseguradora.Activo)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(aseguradora => aseguradora.FechaCreacionUtc)
            .HasColumnType("datetimeoffset")
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .IsRequired();

        builder.Property(aseguradora => aseguradora.CreadoPor)
            .HasMaxLength(100)
            .IsUnicode()
            .HasDefaultValue("migracion-sistema")
            .IsRequired();

        builder.Property(aseguradora => aseguradora.FechaModificacionUtc)
            .HasColumnType("datetimeoffset");

        builder.Property(aseguradora => aseguradora.ModificadoPor)
            .HasMaxLength(100)
            .IsUnicode();

        builder.HasIndex(aseguradora => aseguradora.Descripcion)
            .HasDatabaseName("IX_Aseguradoras_Descripcion");
    }
}
