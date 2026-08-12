using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Infrastructure.Configuration;

namespace SeguimientoFacturacion.Infrastructure
    .Persistence.Configurations;

/// <summary>
/// Configura la persistencia de las glosas
/// asociadas a facturas.
/// </summary>
internal sealed class GlosaConfiguration :
    IEntityTypeConfiguration<Glosa>
{
    private const int UsuarioAuditoriaLongitudMaxima =
        100;

    /// <inheritdoc />
    public void Configure(
        EntityTypeBuilder<Glosa> builder)
    {
        builder.ToTable(
            "Glosas",
            EsquemasBaseDatos.Facturacion,
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_Glosas_ValorGlosa",
                    "[ValorGlosa] > 0");

                tableBuilder.HasCheckConstraint(
                    "CK_Glosas_ValorAceptado",
                    "[ValorAceptado] >= 0 AND " +
                    "[ValorAceptado] <= [ValorGlosa]");

                tableBuilder.HasCheckConstraint(
                    "CK_Glosas_FechaRespuesta",
                    "[FechaRespuesta] IS NULL OR " +
                    "[FechaRespuesta] >= [FechaGlosa]");

                tableBuilder.HasCheckConstraint(
                    "CK_Glosas_Estado",
                    "[Estado] BETWEEN 1 AND 6");

                tableBuilder.HasCheckConstraint(
                    "CK_Glosas_ObservacionResolucion",
                    "[Estado] IN (1, 2) OR " +
                    "NULLIF(LTRIM(RTRIM([Observacion])), '') " +
                    "IS NOT NULL");

                tableBuilder.HasCheckConstraint(
                    "CK_Glosas_Anulacion",
                    "[Estado] <> 6 OR [ValorAceptado] = 0");
            });

        builder.HasKey(
            glosa => glosa.Id);

        builder.Property(
                glosa => glosa.Id)
            .ValueGeneratedNever();

        builder.Property(
                glosa => glosa.FacturaId)
            .HasMaxLength(
                Glosa.FacturaIdLongitudMaxima)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(
                glosa => glosa.FechaGlosa)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(
                glosa => glosa.ValorGlosa)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(
                glosa => glosa.FechaRespuesta)
            .HasColumnType("date");

        builder.Property(
                glosa => glosa.Estado)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(
                glosa => glosa.ValorAceptado)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(
                glosa => glosa.Observacion)
            .HasMaxLength(
                Glosa.ObservacionLongitudMaxima)
            .IsUnicode();

        builder.Property(
                glosa => glosa.VersionFila)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.Property(
                glosa => glosa.FechaCreacionUtc)
            .HasPrecision(0)
            .IsRequired();

        builder.Property(
                glosa => glosa.CreadoPor)
            .HasMaxLength(
                UsuarioAuditoriaLongitudMaxima)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(
                glosa => glosa.FechaModificacionUtc)
            .HasPrecision(0);

        builder.Property(
                glosa => glosa.ModificadoPor)
            .HasMaxLength(
                UsuarioAuditoriaLongitudMaxima)
            .IsUnicode(false);

        builder.Ignore(
            glosa => glosa.ValorPendiente);

        builder.HasOne(
                glosa => glosa.Factura)
            .WithMany()
            .HasForeignKey(
                glosa => glosa.FacturaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(
                glosa => new
                {
                    glosa.FacturaId,
                    glosa.FechaGlosa,
                    glosa.ValorGlosa
                })
            .IsUnique()
            .HasDatabaseName(
                "UX_Glosas_Factura_Fecha_Valor");

        builder.HasIndex(
                glosa => new
                {
                    glosa.FacturaId,
                    glosa.Estado,
                    glosa.FechaGlosa
                })
            .HasDatabaseName(
                "IX_Glosas_Factura_Estado_Fecha");

        builder.HasIndex(
                glosa => glosa.FechaGlosa)
            .HasDatabaseName(
                "IX_Glosas_FechaGlosa");
    }
}
