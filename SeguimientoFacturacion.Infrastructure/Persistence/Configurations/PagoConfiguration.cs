using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Infrastructure.Configuration;

namespace SeguimientoFacturacion.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configura la persistencia de los pagos
/// recibidos de las aseguradoras.
/// </summary>
internal sealed class PagoConfiguration :
    IEntityTypeConfiguration<Pago>
{
    private const int UsuarioAuditoriaLongitudMaxima = 100;

    /// <inheritdoc />
    public void Configure(
        EntityTypeBuilder<Pago> builder)
    {
        builder.ToTable(
            "Pagos",
            EsquemasBaseDatos.Cartera,
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_Pagos_ValorPagado",
                    "[ValorPagado] > 0");

                tableBuilder.HasCheckConstraint(
                    "CK_Pagos_ValoresNoNegativos",
                    "[Retencion] >= 0 AND " +
                    "[ReteIca] >= 0");
            });

        builder.HasKey(pago => pago.Id);

        builder.Property(pago => pago.Id)
            .ValueGeneratedNever();

        builder.Property(pago => pago.AseguradoraId)
            .IsRequired();

        builder.Property(pago => pago.FechaPago)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(pago => pago.Recibo)
            .HasMaxLength(Pago.ReciboLongitudMaxima)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(pago => pago.ValorPagado)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(pago => pago.Retencion)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(pago => pago.ReteIca)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(pago => pago.Notas)
            .HasMaxLength(Pago.NotasLongitudMaxima)
            .IsUnicode();

        builder.Property(pago => pago.FechaCreacionUtc)
            .HasPrecision(0)
            .IsRequired();

        builder.Property(pago => pago.CreadoPor)
            .HasMaxLength(UsuarioAuditoriaLongitudMaxima)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(pago => pago.FechaModificacionUtc)
            .HasPrecision(0);

        builder.Property(pago => pago.ModificadoPor)
            .HasMaxLength(UsuarioAuditoriaLongitudMaxima)
            .IsUnicode(false);

        builder.Ignore(pago => pago.TotalRecibidoDistribuido);
        builder.Ignore(pago => pago.TotalAplicado);
        builder.Ignore(pago => pago.TotalAnticipo);

        builder.HasOne(pago => pago.Aseguradora)
            .WithMany()
            .HasForeignKey(pago => pago.AseguradoraId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(pago => pago.Aplicaciones)
            .WithOne(aplicacion => aplicacion.Pago)
            .HasForeignKey(aplicacion => aplicacion.PagoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(pago => pago.Aplicaciones)
            .HasField("_aplicaciones")
            .UsePropertyAccessMode(
                PropertyAccessMode.Field);

        builder.HasIndex(
                pago => new
                {
                    pago.AseguradoraId,
                    pago.Recibo
                })
            .IsUnique()
            .HasDatabaseName(
                "UX_Pagos_Aseguradora_Recibo");

        builder.HasIndex(
                pago => new
                {
                    pago.AseguradoraId,
                    pago.FechaPago
                })
            .HasDatabaseName(
                "IX_Pagos_Aseguradora_FechaPago");
    }
}
