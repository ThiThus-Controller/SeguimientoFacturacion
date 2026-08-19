using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Infrastructure.Configuration;

namespace SeguimientoFacturacion.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configura la persistencia de las notas crédito
/// y débito asociadas a facturas.
/// </summary>
internal sealed class NotaFacturaConfiguration :
    IEntityTypeConfiguration<NotaFactura>
{
    private const int UsuarioAuditoriaLongitudMaxima = 100;

    /// <inheritdoc />
    public void Configure(
        EntityTypeBuilder<NotaFactura> builder)
    {
        builder.ToTable(
            "NotasFactura",
            EsquemasBaseDatos.Facturacion,
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_NotasFactura_Valor",
                    "[Valor] > 0");

                tableBuilder.HasCheckConstraint(
                    "CK_NotasFactura_Anulacion",
                    "([Anulada] = 0 AND " +
                    "[MotivoAnulacion] IS NULL) OR " +
                    "([Anulada] = 1 AND " +
                    "NULLIF(LTRIM(RTRIM(" +
                    "[MotivoAnulacion])), '') IS NOT NULL)");

                tableBuilder.HasCheckConstraint(
                    "CK_NotasFactura_Glosa",
                    "([Tipo] = 1 AND [GlosaId] IS NOT NULL) OR " +
                    "([Tipo] = 2 AND [GlosaId] IS NULL)");
            });

        builder.HasKey(nota => nota.Id);

        builder.Property(nota => nota.Id)
            .ValueGeneratedNever();

        builder.Property(nota => nota.FacturaId)
            .HasMaxLength(
                NotaFactura.FacturaIdLongitudMaxima)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(nota => nota.Tipo)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(nota => nota.Fecha)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(nota => nota.Numero)
            .HasMaxLength(
                NotaFactura.NumeroLongitudMaxima)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(nota => nota.Valor)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(nota => nota.GlosaId);

        builder.Property(nota => nota.Anulada)
            .IsRequired()
            .IsConcurrencyToken();

        builder.Property(nota => nota.MotivoAnulacion)
            .HasMaxLength(
                NotaFactura.MotivoAnulacionLongitudMaxima)
            .IsUnicode();

        builder.Property(nota => nota.FechaCreacionUtc)
            .HasPrecision(0)
            .IsRequired();

        builder.Property(nota => nota.CreadoPor)
            .HasMaxLength(UsuarioAuditoriaLongitudMaxima)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(nota => nota.FechaModificacionUtc)
            .HasPrecision(0);

        builder.Property(nota => nota.ModificadoPor)
            .HasMaxLength(UsuarioAuditoriaLongitudMaxima)
            .IsUnicode(false);

        builder.Ignore(nota => nota.ImpactoSaldo);

        builder.HasOne(nota => nota.Factura)
            .WithMany()
            .HasForeignKey(nota => nota.FacturaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(nota => nota.Glosa)
            .WithMany()
            .HasForeignKey(nota => nota.GlosaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(
                nota => new
                {
                    nota.FacturaId,
                    nota.Tipo,
                    nota.Numero
                })
            .IsUnique()
            .HasDatabaseName(
                "UX_NotasFactura_Factura_Tipo_Numero");

        builder.HasIndex(nota => nota.Fecha)
            .HasDatabaseName(
                "IX_NotasFactura_Fecha");

        builder.HasIndex(nota => nota.GlosaId)
            .HasDatabaseName(
                "IX_NotasFactura_GlosaId");
    }
}
