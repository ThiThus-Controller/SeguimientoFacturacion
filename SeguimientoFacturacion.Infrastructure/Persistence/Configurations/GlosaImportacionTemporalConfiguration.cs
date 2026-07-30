using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Infrastructure.Configuration;

namespace SeguimientoFacturacion.Infrastructure
    .Persistence.Configurations;

/// <summary>
/// Configura la persistencia temporal de las glosas
/// durante el proceso de importación.
/// </summary>
internal sealed class
    GlosaImportacionTemporalConfiguration :
        IEntityTypeConfiguration<
            GlosaImportacionTemporal>
{
    /// <inheritdoc />
    public void Configure(
        EntityTypeBuilder<
            GlosaImportacionTemporal> builder)
    {
        builder.ToTable(
            "GlosasTemporales",
            EsquemasBaseDatos.Importacion,
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_GlosasTemporales_FilaOrigen",
                    "[FilaOrigen] > 0");

                tableBuilder.HasCheckConstraint(
                    "CK_GlosasTemporales_Aseguradora",
                    "[AseguradoraId] > 0");

                tableBuilder.HasCheckConstraint(
                    "CK_GlosasTemporales_Valor",
                    "[ValorGlosa] > 0");

                tableBuilder.HasCheckConstraint(
                    "CK_GlosasTemporales_FE",
                    "[IdentificadorFe] = " +
                    "[Prefijo] + [NumeroFactura]");

                tableBuilder.HasCheckConstraint(
                    "CK_GlosasTemporales_Fechas",
                    "[FechaRespuesta] IS NULL OR " +
                    "[FechaRespuesta] >= [FechaGlosa]");
            });

        builder.HasKey(
            registro => registro.Id);

        builder.Property(
                registro => registro.Id)
            .ValueGeneratedNever();

        builder.Property(
                registro =>
                    registro.LoteImportacionId)
            .IsRequired();

        builder.Property(
                registro => registro.HojaOrigen)
            .HasMaxLength(
                GlosaImportacionTemporal
                    .HojaOrigenLongitudMaxima)
            .IsUnicode()
            .IsRequired();

        builder.Property(
                registro => registro.FilaOrigen)
            .IsRequired();

        builder.Property(
                registro =>
                    registro.IdentificadorFe)
            .HasMaxLength(
                Factura.IdLongitudMaxima)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(
                registro => registro.Prefijo)
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

        builder.Property(
                registro => registro.AseguradoraId)
            .IsRequired();

        builder.Property(
                registro => registro.FechaGlosa)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(
                registro => registro.ValorGlosa)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(
                registro => registro.FechaRespuesta)
            .HasColumnType("date");

        builder.Ignore(
            registro => registro.TieneRespuesta);

        builder.HasOne(
                registro =>
                    registro.LoteImportacion)
            .WithMany()
            .HasForeignKey(
                registro =>
                    registro.LoteImportacionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(
                registro => new
                {
                    registro.LoteImportacionId,
                    registro.HojaOrigen,
                    registro.FilaOrigen
                })
            .IsUnique()
            .HasDatabaseName(
                "UX_GlosasTemporales_" +
                "Lote_Hoja_Fila");

        builder.HasIndex(
                registro => new
                {
                    registro.LoteImportacionId,
                    registro.IdentificadorFe,
                    registro.FechaGlosa,
                    registro.ValorGlosa
                })
            .IsUnique()
            .HasDatabaseName(
                "UX_GlosasTemporales_" +
                "Lote_Factura_Fecha_Valor");

        builder.HasIndex(
                registro => new
                {
                    registro.LoteImportacionId,
                    registro.IdentificadorFe
                })
            .HasDatabaseName(
                "IX_GlosasTemporales_Lote_FE");
    }
}