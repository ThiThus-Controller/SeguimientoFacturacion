using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Infrastructure.Configuration;

namespace SeguimientoFacturacion.Infrastructure
    .Persistence.Configurations;

/// <summary>
/// Configura la persistencia temporal de los pagos
/// durante el proceso de importación.
/// </summary>
internal sealed class
    PagoImportacionTemporalConfiguration :
        IEntityTypeConfiguration<
            PagoImportacionTemporal>
{
    /// <inheritdoc />
    public void Configure(
        EntityTypeBuilder<
            PagoImportacionTemporal> builder)
    {
        builder.ToTable(
            "PagosTemporales",
            EsquemasBaseDatos.Importacion,
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_PagosTemporales_Aseguradora",
                    "[AseguradoraId] > 0");

                tableBuilder.HasCheckConstraint(
                    "CK_PagosTemporales_ValorPagado",
                    "[ValorPagado] > 0");

                tableBuilder.HasCheckConstraint(
                    "CK_PagosTemporales_Valores",
                    "[Retencion] >= 0 AND " +
                    "[ReteIca] >= 0");
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
                    registro.LoteImportacionId)
            .IsRequired();

        builder.Property(
                registro =>
                    registro.AseguradoraId)
            .IsRequired();

        builder.Property(
                registro =>
                    registro.FechaPago)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(
                registro =>
                    registro.Recibo)
            .HasMaxLength(
                PagoImportacionTemporal
                    .ReciboLongitudMaxima)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(
                registro =>
                    registro.ValorPagado)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(
                registro =>
                    registro.Retencion)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(
                registro =>
                    registro.ReteIca)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(
                registro =>
                    registro.Notas)
            .HasMaxLength(
                PagoImportacionTemporal
                    .NotasLongitudMaxima)
            .IsUnicode();

        builder.Ignore(
            registro =>
                registro.TotalAplicado);

        builder.Ignore(registro => registro.TotalRecibidoDistribuido);

        builder.Ignore(registro => registro.TotalAnticipo);

        builder.Ignore(
            registro =>
                registro.EstaDistribuido);

        builder.HasOne(
                registro =>
                    registro.LoteImportacion)
            .WithMany()
            .HasForeignKey(
                registro =>
                    registro.LoteImportacionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(
                registro =>
                    registro.Aplicaciones)
            .WithOne(
                aplicacion =>
                    aplicacion
                        .PagoImportacionTemporal)
            .HasForeignKey(
                aplicacion =>
                    aplicacion
                        .PagoImportacionTemporalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(
                registro =>
                    registro.Aplicaciones)
            .HasField("_aplicaciones")
            .UsePropertyAccessMode(
                PropertyAccessMode.Field);

        builder.HasIndex(
                registro => new
                {
                    registro.LoteImportacionId,
                    registro.AseguradoraId,
                    registro.Recibo
                })
            .IsUnique()
            .HasDatabaseName(
                "UX_PagosTemporales_" +
                "Lote_Aseguradora_Recibo");

        builder.HasIndex(
                registro => new
                {
                    registro.LoteImportacionId,
                    registro.FechaPago
                })
            .HasDatabaseName(
                "IX_PagosTemporales_Lote_Fecha");
    }
}
