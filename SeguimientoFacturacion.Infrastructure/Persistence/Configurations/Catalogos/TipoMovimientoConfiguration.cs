using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SeguimientoFacturacion.Domain.Entities.Catalogos;
using SeguimientoFacturacion.Domain.Enums;
using SeguimientoFacturacion.Infrastructure.Configuration;

namespace SeguimientoFacturacion.Infrastructure.Persistence.Configurations.Catalogos;

/// <summary>
/// Configura la persistencia de los tipos de movimiento.
/// </summary>
internal sealed class TipoMovimientoConfiguration :
    IEntityTypeConfiguration<TipoMovimiento>
{
    /// <inheritdoc />
    public void Configure(
        EntityTypeBuilder<TipoMovimiento> builder)
    {
        builder.ToTable(
            "TiposMovimiento",
            EsquemasBaseDatos.Facturacion);

        builder.HasKey(tipoMovimiento => tipoMovimiento.Id);

        builder.Property(tipoMovimiento => tipoMovimiento.Id)
            .HasConversion<int>()
            .ValueGeneratedNever();

        builder.Property(tipoMovimiento => tipoMovimiento.Descripcion)
            .HasMaxLength(TipoMovimiento.DescripcionLongitudMaxima)
            .IsUnicode()
            .IsRequired();

        builder.HasData(
            new
            {
                Id = TipoMovimientoCodigo.NotaCredito,
                Descripcion = "NOTA CREDITO"
            },
            new
            {
                Id = TipoMovimientoCodigo.Abono,
                Descripcion = "ABONOS"
            },
            new
            {
                Id = TipoMovimientoCodigo.GlosaODevolucion,
                Descripcion = "GLOSA Y/O DEVOLUCION"
            },
            new
            {
                Id = TipoMovimientoCodigo.Conciliacion,
                Descripcion = "CONCILIACION"
            });
    }
}
