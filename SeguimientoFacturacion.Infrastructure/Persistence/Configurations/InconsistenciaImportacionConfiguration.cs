using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Infrastructure.Configuration;

namespace SeguimientoFacturacion.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configura la persistencia de las inconsistencias
/// detectadas en una importación.
/// </summary>
internal sealed class InconsistenciaImportacionConfiguration :
    IEntityTypeConfiguration<InconsistenciaImportacion>
{
    /// <inheritdoc />
    public void Configure(
        EntityTypeBuilder<InconsistenciaImportacion> builder)
    {
        builder.ToTable(
            "InconsistenciasImportacion",
            EsquemasBaseDatos.Importacion,
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_InconsistenciasImportacion_NumeroFila",
                    "[NumeroFila] IS NULL OR [NumeroFila] > 0");
            });

        builder.HasKey(inconsistencia => inconsistencia.Id);

        builder.Property(inconsistencia => inconsistencia.Id)
            .ValueGeneratedNever();

        builder.Property(
                inconsistencia =>
                    inconsistencia.LoteImportacionId)
            .IsRequired();

        builder.Property(inconsistencia => inconsistencia.Severidad)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(inconsistencia => inconsistencia.NumeroFila);

        builder.Property(inconsistencia => inconsistencia.Columna)
            .HasMaxLength(
                InconsistenciaImportacion.ColumnaLongitudMaxima)
            .IsUnicode();

        builder.Property(inconsistencia => inconsistencia.Codigo)
            .HasMaxLength(
                InconsistenciaImportacion.CodigoLongitudMaxima)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(inconsistencia => inconsistencia.Mensaje)
            .HasMaxLength(
                InconsistenciaImportacion.MensajeLongitudMaxima)
            .IsUnicode()
            .IsRequired();

        builder.Property(
                inconsistencia =>
                    inconsistencia.ValorPresentado)
            .HasMaxLength(
                InconsistenciaImportacion
                    .ValorPresentadoLongitudMaxima)
            .IsUnicode();

        builder.Property(
                inconsistencia =>
                    inconsistencia.EsDatoSensible)
            .IsRequired();

        builder.HasOne(
                inconsistencia =>
                    inconsistencia.LoteImportacion)
            .WithMany()
            .HasForeignKey(
                inconsistencia =>
                    inconsistencia.LoteImportacionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(
                inconsistencia => new
                {
                    inconsistencia.LoteImportacionId,
                    inconsistencia.Severidad,
                    inconsistencia.NumeroFila
                })
            .HasDatabaseName(
                "IX_InconsistenciasImportacion_" +
                "Lote_Severidad_Fila");

        builder.HasIndex(
                inconsistencia => new
                {
                    inconsistencia.LoteImportacionId,
                    inconsistencia.Codigo
                })
            .HasDatabaseName(
                "IX_InconsistenciasImportacion_Lote_Codigo");
    }
}