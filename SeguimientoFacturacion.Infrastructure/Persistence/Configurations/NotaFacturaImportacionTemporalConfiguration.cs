using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Infrastructure.Configuration;

namespace SeguimientoFacturacion.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configura la persistencia temporal de notas crédito
/// y débito durante su proceso de importación.
/// </summary>
internal sealed class
    NotaFacturaImportacionTemporalConfiguration :
        IEntityTypeConfiguration<
            NotaFacturaImportacionTemporal>
{
    /// <inheritdoc />
    public void Configure(
        EntityTypeBuilder<
            NotaFacturaImportacionTemporal> builder)
    {
        builder.ToTable(
            "NotasFacturaTemporales",
            EsquemasBaseDatos.Importacion,
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_NotasFacturaTemporales_FilaOrigen",
                    "[FilaOrigen] > 0");

                tableBuilder.HasCheckConstraint(
                    "CK_NotasFacturaTemporales_Aseguradora",
                    "[AseguradoraId] > 0");

                tableBuilder.HasCheckConstraint(
                    "CK_NotasFacturaTemporales_Tipo",
                    "[Tipo] IN (1, 2)");

                tableBuilder.HasCheckConstraint(
                    "CK_NotasFacturaTemporales_Valor",
                    "[ValorNota] > 0");

                tableBuilder.HasCheckConstraint(
                    "CK_NotasFacturaTemporales_FE",
                    "[IdentificadorFe] = " +
                    "[Prefijo] + [NumeroFactura]");
            });

        builder.HasKey(registro => registro.Id);

        builder.Property(registro => registro.Id)
            .ValueGeneratedNever();

        builder.Property(
                registro => registro.LoteImportacionId)
            .IsRequired();

        builder.Property(registro => registro.HojaOrigen)
            .HasMaxLength(
                NotaFacturaImportacionTemporal
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

        builder.Property(
                registro => registro.NumeroFactura)
            .HasMaxLength(
                Factura.NumeroLongitudMaxima)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(registro => registro.AseguradoraId)
            .IsRequired();

        builder.Property(registro => registro.Tipo)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(registro => registro.FechaNota)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(registro => registro.NumeroNota)
            .HasMaxLength(
                NotaFactura.NumeroLongitudMaxima)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(registro => registro.ValorNota)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Ignore(registro => registro.ImpactoSaldo);

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
                "UX_NotasFacturaTemporales_" +
                "Lote_Hoja_Fila");

        builder.HasIndex(
                registro => new
                {
                    registro.LoteImportacionId,
                    registro.IdentificadorFe,
                    registro.Tipo,
                    registro.NumeroNota
                })
            .IsUnique()
            .HasDatabaseName(
                "UX_NotasFacturaTemporales_" +
                "Lote_Factura_Tipo_Numero");

        builder.HasIndex(
                registro => new
                {
                    registro.LoteImportacionId,
                    registro.IdentificadorFe
                })
            .HasDatabaseName(
                "IX_NotasFacturaTemporales_Lote_FE");
    }
}