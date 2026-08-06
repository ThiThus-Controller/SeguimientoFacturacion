using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Infrastructure.Configuration;

namespace SeguimientoFacturacion.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configura la persistencia de las aplicaciones
/// de pagos sobre facturas.
/// </summary>
internal sealed class AplicacionPagoConfiguration :
    IEntityTypeConfiguration<AplicacionPago>
{
    private const int UsuarioAuditoriaLongitudMaxima = 100;

    /// <inheritdoc />
    public void Configure(
        EntityTypeBuilder<AplicacionPago> builder)
    {
        builder.ToTable(
            "AplicacionesPago",
            EsquemasBaseDatos.Cartera,
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_AplicacionesPago_Valores",
                    "[ValorRecibido] > 0 AND " +
                    "[ValorAplicado] >= 0 AND " +
                    "[ValorAnticipo] >= 0 AND " +
                    "[ValorAplicado] + [ValorAnticipo] = [ValorRecibido]");
            });

        builder.HasKey(aplicacion => aplicacion.Id);

        builder.Property(aplicacion => aplicacion.Id)
            .ValueGeneratedNever();

        builder.Property(aplicacion => aplicacion.PagoId)
            .IsRequired();

        builder.Property(aplicacion => aplicacion.FacturaId)
            .HasMaxLength(
                AplicacionPago.FacturaIdLongitudMaxima)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(aplicacion => aplicacion.ValorRecibido)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(aplicacion => aplicacion.ValorAplicado)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(aplicacion => aplicacion.ValorAnticipo)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(aplicacion => aplicacion.FechaCreacionUtc)
            .HasPrecision(0)
            .IsRequired();

        builder.Property(aplicacion => aplicacion.CreadoPor)
            .HasMaxLength(UsuarioAuditoriaLongitudMaxima)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(
                aplicacion =>
                    aplicacion.FechaModificacionUtc)
            .HasPrecision(0);

        builder.Property(aplicacion => aplicacion.ModificadoPor)
            .HasMaxLength(UsuarioAuditoriaLongitudMaxima)
            .IsUnicode(false);

        builder.HasOne(aplicacion => aplicacion.Factura)
            .WithMany()
            .HasForeignKey(aplicacion => aplicacion.FacturaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(
                aplicacion => new
                {
                    aplicacion.PagoId,
                    aplicacion.FacturaId
                })
            .IsUnique()
            .HasDatabaseName(
                "UX_AplicacionesPago_Pago_Factura");

        builder.HasIndex(aplicacion => aplicacion.FacturaId)
            .HasDatabaseName(
                "IX_AplicacionesPago_FacturaId");
    }
}
