using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Infrastructure.Configuration;

namespace SeguimientoFacturacion.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configura las filas temporales de facturación.
/// </summary>
internal sealed class
    FacturaImportacionTemporalConfiguration :
        IEntityTypeConfiguration<
            FacturaImportacionTemporal>
{
    /// <inheritdoc />
    public void Configure(
        EntityTypeBuilder<FacturaImportacionTemporal>
            builder)
    {
        builder.ToTable(
            "FacturasTemporales",
            EsquemasBaseDatos.Importacion,
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_FacturasTemporales_FilaOrigen",
                    "[FilaOrigen] > 0");

                tableBuilder.HasCheckConstraint(
                    "CK_FacturasTemporales_Valor",
                    "[Valor] > 0");

                tableBuilder.HasCheckConstraint(
                    "CK_FacturasTemporales_Catalogos",
                    "[AseguradoraId] > 0 AND " +
                    "[TipoDocumentoId] > 0 AND " +
                    "[AtencionId] > 0 AND " +
                    "[CostoId] > 0 AND " +
                    "[EstadoId] > 0 AND " +
                    "[FacturadorId] > 0");

                tableBuilder.HasCheckConstraint(
                    "CK_FacturasTemporales_Fechas",
                    "([FechaRadicacion] IS NULL OR " +
                    "[FechaRadicacion] >= [FechaFactura]) AND " +
                    "([FechaAdmision] IS NULL OR " +
                    "[FechaAdmision] <= [FechaFactura])");
            });

        builder.HasKey(registro => registro.Id);

        builder.Property(registro => registro.Id)
            .ValueGeneratedNever();

        builder.Property(
                registro => registro.LoteImportacionId)
            .IsRequired();

        builder.Property(registro => registro.HojaOrigen)
            .HasMaxLength(
                FacturaImportacionTemporal
                    .HojaOrigenLongitudMaxima)
            .IsUnicode()
            .IsRequired();

        builder.Property(registro => registro.FilaOrigen)
            .IsRequired();

        builder.Property(
                registro => registro.IdentificadorFe)
            .HasMaxLength(Factura.IdLongitudMaxima)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(registro => registro.Prefijo)
            .HasMaxLength(
                Factura.PrefijoLongitudMaxima)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(registro => registro.Numero)
            .HasMaxLength(
                Factura.NumeroLongitudMaxima)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(registro => registro.FechaFactura)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(registro => registro.Valor)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(
                registro => registro.FechaRadicacion)
            .HasColumnType("date");

        builder.Property(
                registro => registro.NumeroDocumento)
            .HasMaxLength(
                Paciente.NumeroDocumentoLongitudMaxima)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(
                registro => registro.NombreCompleto)
            .HasMaxLength(
                Paciente.NombreCompletoLongitudMaxima)
            .IsUnicode()
            .IsRequired();

        builder.Property(
                registro => registro.NumeroAdmision)
            .HasMaxLength(
                Factura.NumeroAdmisionLongitudMaxima)
            .IsUnicode(false);

        builder.Property(registro => registro.FechaAdmision)
            .HasColumnType("date");

        builder.HasOne(
                registro => registro.LoteImportacion)
            .WithMany()
            .HasForeignKey(
                registro => registro.LoteImportacionId)
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
                "UX_FacturasTemporales_" +
                "Lote_Hoja_Fila");

        builder.HasIndex(
                registro => new
                {
                    registro.LoteImportacionId,
                    registro.IdentificadorFe
                })
            .HasDatabaseName(
                "IX_FacturasTemporales_Lote_FE");

        builder.HasIndex(
                registro => new
                {
                    registro.LoteImportacionId,
                    registro.Prefijo,
                    registro.Numero
                })
            .HasDatabaseName(
                "IX_FacturasTemporales_" +
                "Lote_Prefijo_Numero");
    }
}