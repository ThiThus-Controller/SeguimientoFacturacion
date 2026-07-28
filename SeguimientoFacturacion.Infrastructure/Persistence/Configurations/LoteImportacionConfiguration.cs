using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Infrastructure.Configuration;

namespace SeguimientoFacturacion.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configura la persistencia de los lotes
/// de importación masiva.
/// </summary>
internal sealed class LoteImportacionConfiguration :
    IEntityTypeConfiguration<LoteImportacion>
{
    private const int UsuarioAuditoriaLongitudMaxima = 100;

    /// <inheritdoc />
    public void Configure(
        EntityTypeBuilder<LoteImportacion> builder)
    {
        builder.ToTable(
            "LotesImportacion",
            EsquemasBaseDatos.Importacion,
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_LotesImportacion_Totales",
                    "[TotalFilas] >= 0 AND " +
                    "[TotalFilasValidas] >= 0 AND " +
                    "[TotalFilasConError] >= 0 AND " +
                    "[TotalAdvertencias] >= 0 AND " +
                    "[TotalFilasValidas] + " +
                    "[TotalFilasConError] = [TotalFilas]");
            });

        builder.HasKey(lote => lote.Id);

        builder.Property(lote => lote.Id)
            .ValueGeneratedNever();

        builder.Property(lote => lote.Tipo)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(lote => lote.NombreArchivo)
            .HasMaxLength(
                LoteImportacion.NombreArchivoLongitudMaxima)
            .IsUnicode()
            .IsRequired();

        builder.Property(lote => lote.HashArchivo)
            .HasColumnType("char(64)")
            .HasMaxLength(
                LoteImportacion.HashArchivoLongitud)
            .IsUnicode(false)
            .IsFixedLength()
            .IsRequired();

        builder.Property(lote => lote.Estado)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(lote => lote.TotalFilas)
            .IsRequired();

        builder.Property(lote => lote.TotalFilasValidas)
            .IsRequired();

        builder.Property(lote => lote.TotalFilasConError)
            .IsRequired();

        builder.Property(lote => lote.TotalAdvertencias)
            .IsRequired();

        builder.Property(lote => lote.FechaAnalisisUtc)
            .HasPrecision(0);

        builder.Property(lote => lote.FechaConfirmacionUtc)
            .HasPrecision(0);

        builder.Property(lote => lote.ConfirmadoPor)
            .HasMaxLength(
                LoteImportacion.UsuarioLongitudMaxima)
            .IsUnicode(false);

        builder.Property(
                lote => lote.FechaInicioProcesamientoUtc)
            .HasPrecision(0);

        builder.Property(lote => lote.FechaFinalizacionUtc)
            .HasPrecision(0);

        builder.Property(lote => lote.DetalleResultado)
            .HasMaxLength(
                LoteImportacion.DetalleResultadoLongitudMaxima)
            .IsUnicode();

        builder.Property(lote => lote.FechaCreacionUtc)
            .HasPrecision(0)
            .IsRequired();

        builder.Property(lote => lote.CreadoPor)
            .HasMaxLength(UsuarioAuditoriaLongitudMaxima)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(lote => lote.FechaModificacionUtc)
            .HasPrecision(0);

        builder.Property(lote => lote.ModificadoPor)
            .HasMaxLength(UsuarioAuditoriaLongitudMaxima)
            .IsUnicode(false);

        builder.Ignore(lote => lote.PuedeConfirmarse);

        builder.HasIndex(
                lote => new
                {
                    lote.Tipo,
                    lote.HashArchivo
                })
            .HasDatabaseName(
                "IX_LotesImportacion_Tipo_HashArchivo");

        builder.HasIndex(
                lote => new
                {
                    lote.Estado,
                    lote.FechaCreacionUtc
                })
            .HasDatabaseName(
                "IX_LotesImportacion_Estado_FechaCreacionUtc");
    }
}