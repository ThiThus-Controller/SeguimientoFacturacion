using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Infrastructure.Configuration;

namespace SeguimientoFacturacion.Infrastructure
    .Persistence.Configurations;

/// <summary>
/// Configura la persistencia de las aplicaciones
/// temporales de pagos.
/// </summary>
internal sealed class
    AplicacionPagoImportacionTemporalConfiguration :
        IEntityTypeConfiguration<
            AplicacionPagoImportacionTemporal>
{
    /// <inheritdoc />
    public void Configure(
        EntityTypeBuilder<
            AplicacionPagoImportacionTemporal> builder)
    {
        builder.ToTable(
            "AplicacionesPagoTemporales",
            EsquemasBaseDatos.Importacion,
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_AplicacionesPagoTemporales_Fila",
                    "[FilaOrigen] > 0");

                tableBuilder.HasCheckConstraint(
                    "CK_AplicacionesPagoTemporales_FE",
                    "[IdentificadorFe] = " +
                    "[Prefijo] + [NumeroFactura]");

                tableBuilder.HasCheckConstraint(
                    "CK_AplicacionesPagoTemporales_Valores",
                    "[ValorRecibido] > 0 AND " +
                    "[ValorAplicado] >= 0 AND " +
                    "[ValorAnticipo] >= 0 AND " +
                    "[ValorAplicado] + [ValorAnticipo] = [ValorRecibido]");
            });

        builder.HasKey(
            registro =>
                registro.Id);

        builder.Property(
                registro =>
                    registro.Id)
            .ValueGeneratedNever();

        builder.Property(
                registro =>
                    registro.PagoImportacionTemporalId)
            .IsRequired();

        builder.Property(
                registro =>
                    registro.HojaOrigen)
            .HasMaxLength(
                AplicacionPagoImportacionTemporal
                    .HojaOrigenLongitudMaxima)
            .IsUnicode()
            .IsRequired();

        builder.Property(
                registro =>
                    registro.FilaOrigen)
            .IsRequired();

        builder.Property(
                registro =>
                    registro.IdentificadorFe)
            .HasMaxLength(
                Factura.IdLongitudMaxima)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(
                registro =>
                    registro.Prefijo)
            .HasMaxLength(
                Factura.PrefijoLongitudMaxima)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(
                registro =>
                    registro.NumeroFactura)
            .HasMaxLength(
                Factura.NumeroLongitudMaxima)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(registro => registro.ValorRecibido)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(
                registro =>
                    registro.ValorAplicado)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(
                registro => registro.ValorAnticipo)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.HasOne(
                registro =>
                    registro.PagoImportacionTemporal)
            .WithMany(
                pago =>
                    pago.Aplicaciones)
            .HasForeignKey(
                registro =>
                    registro.PagoImportacionTemporalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(
                registro => new
                {
                    registro.PagoImportacionTemporalId,
                    registro.HojaOrigen,
                    registro.FilaOrigen
                })
            .IsUnique()
            .HasDatabaseName(
                "UX_AplicacionesPagoTemporales_" +
                "Pago_Hoja_Fila");

        builder.HasIndex(
                registro => new
                {
                    registro.PagoImportacionTemporalId,
                    registro.IdentificadorFe
                })
            .IsUnique()
            .HasDatabaseName(
                "UX_AplicacionesPagoTemporales_" +
                "Pago_FE");

        builder.HasIndex(
                registro =>
                    registro.IdentificadorFe)
            .HasDatabaseName(
                "IX_AplicacionesPagoTemporales_FE");
    }
}
