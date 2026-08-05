using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SeguimientoFacturacion.Domain.Entities.Catalogos;
using SeguimientoFacturacion.Infrastructure.Configuration;

namespace SeguimientoFacturacion.Infrastructure.Persistence.Configurations.Catalogos;

/// <summary>
/// Configura la persistencia de los facturadores.
/// </summary>
internal sealed class FacturadorConfiguration :
    IEntityTypeConfiguration<Facturador>
{
    /// <inheritdoc />
    public void Configure(
        EntityTypeBuilder<Facturador> builder)
    {
        builder.ToTable(
            "Facturadores",
            EsquemasBaseDatos.Facturacion);

        builder.HasKey(facturador => facturador.Id);

        builder.Property(facturador => facturador.Id)
            .ValueGeneratedNever();

        builder.Property(facturador => facturador.Nombre)
            .HasMaxLength(Facturador.NombreLongitudMaxima)
            .IsUnicode()
            .IsRequired();

        builder.Property(facturador => facturador.Activo)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(facturador => facturador.FechaCreacionUtc)
            .HasColumnType("datetimeoffset")
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .IsRequired();

        builder.Property(facturador => facturador.CreadoPor)
            .HasMaxLength(100)
            .IsUnicode()
            .HasDefaultValue("migracion-sistema")
            .IsRequired();

        builder.Property(facturador => facturador.FechaModificacionUtc)
            .HasColumnType("datetimeoffset");

        builder.Property(facturador => facturador.ModificadoPor)
            .HasMaxLength(100)
            .IsUnicode();

        builder.HasIndex(facturador => facturador.Nombre)
            .HasDatabaseName("IX_Facturadores_Nombre");
    }
}
